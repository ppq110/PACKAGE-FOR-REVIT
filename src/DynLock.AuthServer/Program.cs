using DynLock.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;
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

var db = new LeaderDatabase(DynLockRuntimeConfig.AuthDatabasePath);
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
    Console.WriteLine("Database: " + DynLockRuntimeConfig.AuthDatabasePath);
    return;
}

app.MapGet("/api/health", () => Results.Ok(new
{
    ok = true,
    database = DynLockRuntimeConfig.AuthDatabasePath,
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
Console.WriteLine("Database      : " + DynLockRuntimeConfig.AuthDatabasePath);
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
    private readonly string _dbPath;
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();

    public LeaderDatabase(string dbPath)
    {
        _dbPath = dbPath;
    }

    public void Initialize(string superAdminEmail)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

        using var cn = Open();
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText =
                @"CREATE TABLE IF NOT EXISTS authorized_leaders (
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
                  VALUES ($email, $name, 1, 1, NULL, $now, NULL)
                  ON CONFLICT(email) DO UPDATE SET
                    is_active = 1,
                    can_manage = 1;";
            cmd.Parameters.AddWithValue("$email", email);
            cmd.Parameters.AddWithValue("$name", "Super Admin");
            cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
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
              WHERE email = $email AND is_active = 1
              LIMIT 1;";
        cmd.Parameters.AddWithValue("$email", NormalizeEmail(email));
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
              VALUES ($email, $name, 1, 0, $addedBy, $now, NULL);";
        cmd.Parameters.AddWithValue("$email", NormalizeEmail(email));
        cmd.Parameters.AddWithValue("$name", (fullName ?? "").Trim());
        cmd.Parameters.AddWithValue("$addedBy", string.IsNullOrWhiteSpace(addedBy) ? DBNull.Value : NormalizeEmail(addedBy));
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));

        try
        {
            cmd.ExecuteNonQuery();
            return FindActive(email);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return null;
        }
    }

    public bool SetActive(string email, bool isActive)
    {
        using var cn = Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "UPDATE authorized_leaders SET is_active = $active WHERE email = $email;";
        cmd.Parameters.AddWithValue("$active", isActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$email", NormalizeEmail(email));
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(string email)
    {
        using var cn = Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "DELETE FROM authorized_leaders WHERE email = $email;";
        cmd.Parameters.AddWithValue("$email", NormalizeEmail(email));
        return cmd.ExecuteNonQuery() > 0;
    }

    public void UpdateLastLogin(string email)
    {
        using var cn = Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "UPDATE authorized_leaders SET last_login = $now WHERE email = $email;";
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$email", NormalizeEmail(email));
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var cn = new SqliteConnection(ConnectionString);
        cn.Open();
        return cn;
    }

    private static LeaderDto ReadLeader(SqliteDataReader rd)
    {
        return new LeaderDto
        {
            Email = rd.GetString(0),
            FullName = rd.IsDBNull(1) ? "" : rd.GetString(1),
            IsActive = rd.GetInt32(2) != 0,
            CanManage = rd.GetInt32(3) != 0,
            AddedBy = rd.IsDBNull(4) ? "" : rd.GetString(4),
            CreatedAt = rd.IsDBNull(5) ? "" : rd.GetString(5),
            LastLogin = rd.IsDBNull(6) ? "" : rd.GetString(6),
        };
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
                  VALUES ($email, $name, $active, $manage, $addedBy, $createdAt, $lastLogin)
                  ON CONFLICT(email) DO UPDATE SET
                    full_name = excluded.full_name,
                    is_active = excluded.is_active,
                    can_manage = excluded.can_manage,
                    added_by = excluded.added_by,
                    created_at = excluded.created_at,
                    last_login = excluded.last_login;";
            cmd.Parameters.AddWithValue("$email", NormalizeEmail(leader.Email));
            cmd.Parameters.AddWithValue("$name", leader.FullName ?? "");
            cmd.Parameters.AddWithValue("$active", leader.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("$manage", leader.CanManage ? 1 : 0);
            cmd.Parameters.AddWithValue("$addedBy", string.IsNullOrWhiteSpace(leader.AddedBy) ? DBNull.Value : NormalizeEmail(leader.AddedBy));
            cmd.Parameters.AddWithValue("$createdAt", string.IsNullOrWhiteSpace(leader.CreatedAt) ? DateTime.UtcNow.ToString("o") : leader.CreatedAt);
            cmd.Parameters.AddWithValue("$lastLogin", string.IsNullOrWhiteSpace(leader.LastLogin) ? DBNull.Value : leader.LastLogin);
            cmd.ExecuteNonQuery();
            count++;
        }

        tx.Commit();
        return count;
    }
}
