using DynLock.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("DYNLOCK_AUTH_SERVER_BIND_URL") ?? "http://0.0.0.0:5050");

var app = builder.Build();

Directory.CreateDirectory(DynLockRuntimeConfig.ConfigRoot);

if (!DynLockRuntimeConfig.TryLoadAuthServerSettings(out var settings, out var configError))
{
    Console.Error.WriteLine(configError);
    Console.Error.WriteLine("Example " + DynLockRuntimeConfig.AuthServerConfigPath + ":");
    Console.Error.WriteLine("{");
    Console.Error.WriteLine("  \"AuthServerUrl\": \"http://192.168.1.50:5050\",");
    Console.Error.WriteLine("  \"SuperAdminEmail\": \"admin@company.com\"");
    Console.Error.WriteLine("}");
    return;
}

var db = new LeaderDatabase(settings);
db.Initialize(settings.SuperAdminEmail);

if (args.Contains("--import-supabase", StringComparer.OrdinalIgnoreCase))
{
    var importSettings = LegacySupabaseImportSettings.Load(args);
    if (importSettings == null)
    {
        Console.Error.WriteLine("Missing legacy Supabase import settings.");
        Console.Error.WriteLine("Use:");
        Console.Error.WriteLine("  --import-supabase --supabase-url https://your-project.supabase.co --supabase-anon-key <anon-key>");
        Console.Error.WriteLine("or environment variables:");
        Console.Error.WriteLine("  DYNLOCK_LEGACY_SUPABASE_URL");
        Console.Error.WriteLine("  DYNLOCK_LEGACY_SUPABASE_ANON_KEY");
        return;
    }

    int imported = await ImportLegacySupabaseAsync(db, importSettings);
    Console.WriteLine("Imported leaders from Supabase: " + imported);
    Console.WriteLine("Database: " + db.DatabaseLabel);
    return;
}

app.MapGet("/api/health", () => Results.Ok(new
{
    ok = true,
    databaseProvider = db.Provider,
    database = db.DatabaseLabel,
    serverTime = DateTime.UtcNow.ToString("o"),
}));

app.MapGet("/api/auth/check", Results<Ok<LeaderDto>, NotFound> (string email) =>
{
    var leader = db.FindActive(email);
    if (leader == null)
        return TypedResults.NotFound();

    db.UpdateLastLogin(leader.Email);
    leader.LastLogin = DateTime.UtcNow.ToString("o");
    return TypedResults.Ok(leader);
});

app.MapGet("/api/leaders", () => Results.Ok(db.GetAll()));

app.MapPost("/api/leaders", Results<Created<LeaderDto>, Conflict, BadRequest<string>> (CreateLeaderRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
        return TypedResults.BadRequest("Email is required.");

    var created = db.Add(request.Email, request.FullName, request.AddedBy);
    if (created == null)
        return TypedResults.Conflict();

    return TypedResults.Created("/api/leaders/" + Uri.EscapeDataString(created.Email), created);
});

app.MapPatch("/api/leaders/{email}/active", Results<Ok, NotFound> (string email, SetActiveRequest request) =>
{
    return db.SetActive(email, request.IsActive)
        ? TypedResults.Ok()
        : TypedResults.NotFound();
});

app.MapDelete("/api/leaders/{email}", Results<Ok, NotFound> (string email) =>
{
    return db.Delete(Uri.UnescapeDataString(email))
        ? TypedResults.Ok()
        : TypedResults.NotFound();
});

Console.WriteLine("BIMLab Auth Server");
Console.WriteLine("Listening URL : " + settings.AuthServerUrl);
Console.WriteLine("Bind URL      : " + (Environment.GetEnvironmentVariable("DYNLOCK_AUTH_SERVER_BIND_URL") ?? "http://0.0.0.0:5050"));
Console.WriteLine("Database      : " + db.DatabaseLabel);
Console.WriteLine("DB provider   : " + db.Provider);
Console.WriteLine("Super admin   : " + settings.SuperAdminEmail);

app.Run();

static async Task<int> ImportLegacySupabaseAsync(LeaderDatabase db, LegacySupabaseImportSettings settings)
{
    string url = settings.ProjectUrl
        + "/rest/v1/authorized_leaders"
        + "?select=email,full_name,is_active,can_manage,added_by,created_at,last_login"
        + "&order=created_at.asc";

    using var http = new HttpClient();
    using var req = new HttpRequestMessage(HttpMethod.Get, url);
    req.Headers.Add("apikey", settings.AnonKey);
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AnonKey);
    req.Headers.Add("Accept", "application/json");

    using var resp = await http.SendAsync(req);
    resp.EnsureSuccessStatusCode();

    await using var stream = await resp.Content.ReadAsStreamAsync();
    var leaders = await JsonSerializer.DeserializeAsync<List<LegacySupabaseLeader>>(stream)
        ?? new List<LegacySupabaseLeader>();
    return db.UpsertImported(leaders);
}

internal sealed record CreateLeaderRequest(string Email, string? FullName, string? AddedBy);
internal sealed record SetActiveRequest(bool IsActive);

internal sealed class LegacySupabaseImportSettings
{
    public string ProjectUrl { get; set; } = "";
    public string AnonKey { get; set; } = "";

    public static LegacySupabaseImportSettings? Load(string[] args)
    {
        string? url = ValueAfter(args, "--supabase-url")
            ?? Environment.GetEnvironmentVariable("DYNLOCK_LEGACY_SUPABASE_URL");
        string? key = ValueAfter(args, "--supabase-anon-key")
            ?? Environment.GetEnvironmentVariable("DYNLOCK_LEGACY_SUPABASE_ANON_KEY");

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
            return null;

        return new LegacySupabaseImportSettings
        {
            ProjectUrl = url.Trim().TrimEnd('/'),
            AnonKey = key.Trim(),
        };
    }

    private static string? ValueAfter(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];

        return null;
    }
}

internal sealed class LegacySupabaseLeader
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("can_manage")]
    public bool CanManage { get; set; }

    [JsonPropertyName("added_by")]
    public string? AddedBy { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("last_login")]
    public string? LastLogin { get; set; }
}

internal sealed class LeaderDto
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; }
    public bool CanManage { get; set; }
    public string AddedBy { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string LastLogin { get; set; } = "";
}

internal sealed class LeaderDatabase
{
    private readonly AuthServerSettings _settings;
    private string ConnectionString => IsPostgres
        ? _settings.DatabaseConnectionString
        : new SqliteConnectionStringBuilder { DataSource = DynLockRuntimeConfig.AuthDatabasePath }.ToString();

    public string Provider => IsPostgres ? "postgres" : "sqlite";
    public string DatabaseLabel => IsPostgres
        ? MaskConnectionString(_settings.DatabaseConnectionString)
        : DynLockRuntimeConfig.AuthDatabasePath;

    private bool IsPostgres => string.Equals(_settings.DatabaseProvider, "postgres", StringComparison.OrdinalIgnoreCase);

    public LeaderDatabase(AuthServerSettings settings)
    {
        _settings = settings;
        if (IsPostgres && string.IsNullOrWhiteSpace(_settings.DatabaseConnectionString))
            throw new InvalidOperationException("Postgres requires DatabaseConnectionString in authserver.json or DYNLOCK_AUTH_DATABASE_CONNECTION_STRING.");
    }

    public void Initialize(string superAdminEmail)
    {
        if (!IsPostgres)
            Directory.CreateDirectory(Path.GetDirectoryName(DynLockRuntimeConfig.AuthDatabasePath)!);

        using var cn = Open();
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = IsPostgres
                ? @"CREATE TABLE IF NOT EXISTS authorized_leaders (
                    email TEXT PRIMARY KEY,
                    full_name TEXT NOT NULL DEFAULT '',
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    can_manage BOOLEAN NOT NULL DEFAULT FALSE,
                    added_by TEXT NULL,
                    created_at TEXT NOT NULL,
                    last_login TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_authorized_leaders_active
                    ON authorized_leaders (email, is_active);"
                : @"CREATE TABLE IF NOT EXISTS authorized_leaders (
                    email TEXT PRIMARY KEY,
                    full_name TEXT NOT NULL DEFAULT '',
                    is_active INTEGER NOT NULL DEFAULT 1,
                    can_manage INTEGER NOT NULL DEFAULT 0,
                    added_by TEXT NULL,
                    created_at TEXT NOT NULL,
                    last_login TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_authorized_leaders_active
                    ON authorized_leaders (email, is_active);";
            cmd.ExecuteNonQuery();
        }

        string email = NormalizeEmail(superAdminEmail);
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText =
                @"INSERT INTO authorized_leaders
                    (email, full_name, is_active, can_manage, added_by, created_at, last_login)
                  VALUES (@email, @name, @active, @manage, NULL, @now, NULL)
                  ON CONFLICT(email) DO UPDATE SET
                    is_active = @active,
                    can_manage = @manage;";
            Add(cmd, "@email", email);
            Add(cmd, "@name", "Super Admin");
            Add(cmd, "@active", true);
            Add(cmd, "@manage", true);
            Add(cmd, "@now", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }
    }

    public LeaderDto? FindActive(string email)
    {
        using var cn = Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText =
            @"SELECT email, full_name, is_active, can_manage, added_by, created_at, last_login
              FROM authorized_leaders
              WHERE email = @email AND is_active = @active
              LIMIT 1;";
        Add(cmd, "@email", NormalizeEmail(email));
        Add(cmd, "@active", true);
        using var rd = cmd.ExecuteReader();
        return rd.Read() ? ReadLeader(rd) : null;
    }

    public List<LeaderDto> GetAll()
    {
        using var cn = Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText =
            @"SELECT email, full_name, is_active, can_manage, added_by, created_at, last_login
              FROM authorized_leaders
              ORDER BY created_at ASC;";
        using var rd = cmd.ExecuteReader();

        var result = new List<LeaderDto>();
        while (rd.Read())
            result.Add(ReadLeader(rd));
        return result;
    }

    public LeaderDto? Add(string email, string? fullName, string? addedBy)
    {
        using var cn = Open();
        using var cmd = cn.CreateCommand();
            cmd.CommandText =
                @"INSERT INTO authorized_leaders
                (email, full_name, is_active, can_manage, added_by, created_at, last_login)
              VALUES (@email, @name, @active, @manage, @addedBy, @now, NULL);";
        Add(cmd, "@email", NormalizeEmail(email));
        Add(cmd, "@name", (fullName ?? "").Trim());
        Add(cmd, "@active", true);
        Add(cmd, "@manage", false);
        Add(cmd, "@addedBy", string.IsNullOrWhiteSpace(addedBy) ? DBNull.Value : NormalizeEmail(addedBy));
        Add(cmd, "@now", DateTime.UtcNow.ToString("o"));

        try
        {
            cmd.ExecuteNonQuery();
            return FindActive(email);
        }
        catch (DbException ex) when (IsUniqueViolation(ex))
        {
            return null;
        }
    }

    public bool SetActive(string email, bool isActive)
    {
        using var cn = Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "UPDATE authorized_leaders SET is_active = @active WHERE email = @email;";
        Add(cmd, "@active", isActive);
        Add(cmd, "@email", NormalizeEmail(email));
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(string email)
    {
        using var cn = Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "DELETE FROM authorized_leaders WHERE email = @email;";
        Add(cmd, "@email", NormalizeEmail(email));
        return cmd.ExecuteNonQuery() > 0;
    }

    public void UpdateLastLogin(string email)
    {
        using var cn = Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "UPDATE authorized_leaders SET last_login = @now WHERE email = @email;";
        Add(cmd, "@now", DateTime.UtcNow.ToString("o"));
        Add(cmd, "@email", NormalizeEmail(email));
        cmd.ExecuteNonQuery();
    }

    private DbConnection Open()
    {
        DbConnection cn = IsPostgres
            ? new NpgsqlConnection(ConnectionString)
            : new SqliteConnection(ConnectionString);
        cn.Open();
        return cn;
    }

    private static void Add(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    private static LeaderDto ReadLeader(DbDataReader rd)
    {
        return new LeaderDto
        {
            Email = rd.GetString(0),
            FullName = rd.IsDBNull(1) ? "" : rd.GetString(1),
            IsActive = GetBool(rd, 2),
            CanManage = GetBool(rd, 3),
            AddedBy = rd.IsDBNull(4) ? "" : rd.GetString(4),
            CreatedAt = rd.IsDBNull(5) ? "" : rd.GetString(5),
            LastLogin = rd.IsDBNull(6) ? "" : rd.GetString(6),
        };
    }

    private static bool GetBool(DbDataReader rd, int index)
    {
        object value = rd.GetValue(index);
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is long l) return l != 0;
        return Convert.ToBoolean(value);
    }

    private static string NormalizeEmail(string email)
    {
        return Uri.UnescapeDataString(email ?? "").Trim().ToLowerInvariant();
    }

    public int UpsertImported(IEnumerable<LegacySupabaseLeader> leaders)
    {
        int count = 0;
        using var cn = Open();
        using var tx = cn.BeginTransaction();

        foreach (var leader in leaders)
        {
            if (string.IsNullOrWhiteSpace(leader.Email))
                continue;

            using var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                @"INSERT INTO authorized_leaders
                    (email, full_name, is_active, can_manage, added_by, created_at, last_login)
                  VALUES (@email, @name, @active, @manage, @addedBy, @createdAt, @lastLogin)
                  ON CONFLICT(email) DO UPDATE SET
                    full_name = excluded.full_name,
                    is_active = excluded.is_active,
                    can_manage = excluded.can_manage,
                    added_by = excluded.added_by,
                    created_at = excluded.created_at,
                    last_login = excluded.last_login;";
            Add(cmd, "@email", NormalizeEmail(leader.Email));
            Add(cmd, "@name", leader.FullName ?? "");
            Add(cmd, "@active", leader.IsActive);
            Add(cmd, "@manage", leader.CanManage);
            Add(cmd, "@addedBy", string.IsNullOrWhiteSpace(leader.AddedBy) ? DBNull.Value : NormalizeEmail(leader.AddedBy));
            Add(cmd, "@createdAt", string.IsNullOrWhiteSpace(leader.CreatedAt) ? DateTime.UtcNow.ToString("o") : leader.CreatedAt);
            Add(cmd, "@lastLogin", string.IsNullOrWhiteSpace(leader.LastLogin) ? DBNull.Value : leader.LastLogin);
            cmd.ExecuteNonQuery();
            count++;
        }

        tx.Commit();
        return count;
    }

    private static bool IsUniqueViolation(DbException ex)
    {
        if (ex is SqliteException sqlite)
            return sqlite.SqliteErrorCode == 19;

        if (ex is PostgresException postgres)
            return postgres.SqlState == PostgresErrorCodes.UniqueViolation;

        return false;
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "";

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrEmpty(builder.Password))
                builder.Password = "***";
            return builder.ToString();
        }
        catch
        {
            return "postgres";
        }
    }
}
