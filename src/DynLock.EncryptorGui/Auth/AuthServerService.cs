using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DynLock.EncryptorGui.Auth
{
    internal static class AuthServerService
    {
        static readonly HttpClient           _http = new HttpClient();
        static readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        static string ApiBase => AuthServerConfig.AuthServerUrl + "/api/";

        static HttpRequestMessage Req(HttpMethod method, string url, string body = null)
        {
            var r = new HttpRequestMessage(method, url);
            r.Headers.Add("Accept", "application/json");
            if (body != null)
                r.Content = new StringContent(body, Encoding.UTF8, "application/json");
            return r;
        }

        static T Get<T>(Dictionary<string, object> row, string key, T def = default)
        {
            if (!row.ContainsKey(key) || row[key] == null) return def;
            try { return (T)row[key]; } catch { return def; }
        }

        public static async Task<bool> LoginAsync(string email)
        {
            email = NormalizeGmail(email);
            if (email == null)
                return false;

            var url = ApiBase
                + "auth/check?email=" + Uri.EscapeDataString(email);

            var resp = await _http.SendAsync(Req(HttpMethod.Get, url));
            if (!resp.IsSuccessStatusCode) return false;

            var row = _json.Deserialize<Dictionary<string, object>>(
                await resp.Content.ReadAsStringAsync());

            if (row == null || !Get<bool>(row, "isActive", false)) return false;

            SessionContext.Email     = Get<string>(row, "email", email);
            SessionContext.FullName  = Get<string>(row, "fullName", "");
            SessionContext.CanManage = Get<bool>(row, "canManage", false);
            return true;
        }

        public static bool IsGmail(string email)
        {
            return NormalizeGmail(email) != null;
        }

        private static string NormalizeGmail(string email)
        {
            email = (email ?? "").Trim().ToLowerInvariant();
            return email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)
                && email.Length > "@gmail.com".Length
                ? email
                : null;
        }

        public static async Task<List<LeaderInfo>> GetAllLeadersAsync()
        {
            var url = ApiBase + "leaders?managerEmail=" + Uri.EscapeDataString(SessionContext.Email ?? "");
            var resp = await _http.SendAsync(Req(HttpMethod.Get, url));
            resp.EnsureSuccessStatusCode();

            var raw = _json.Deserialize<List<Dictionary<string, object>>>(
                await resp.Content.ReadAsStringAsync());
            var result = new List<LeaderInfo>();

            foreach (var row in raw ?? new List<Dictionary<string, object>>())
                result.Add(new LeaderInfo
                {
                    Email     = Get<string>(row, "email"),
                    FullName  = Get<string>(row, "fullName", ""),
                    IsActive  = Get<bool>(row, "isActive", false),
                    CanManage = Get<bool>(row, "canManage", false),
                    AddedBy   = Get<string>(row, "addedBy", ""),
                    CreatedAt = Get<string>(row, "createdAt", ""),
                    LastLogin = Get<string>(row, "lastLogin", ""),
                });

            return result;
        }

        public static async Task<bool> AddLeaderAsync(string email, string fullName)
        {
            var body = _json.Serialize(new Dictionary<string, object>
            {
                ["email"] = email,
                ["fullName"] = fullName,
                ["addedBy"] = SessionContext.Email,
            });

            var resp = await _http.SendAsync(Req(HttpMethod.Post, ApiBase + "leaders", body));
            return resp.IsSuccessStatusCode;
        }

        public static async Task<bool> SetActiveAsync(string email, bool isActive)
        {
            var body = _json.Serialize(new Dictionary<string, object>
            {
                ["isActive"] = isActive,
                ["managerEmail"] = SessionContext.Email,
            });
            var url = ApiBase + "leaders/" + Uri.EscapeDataString(email) + "/active";
            var resp = await _http.SendAsync(Req(new HttpMethod("PATCH"), url, body));
            return resp.IsSuccessStatusCode;
        }

        public static async Task<bool> DeleteLeaderAsync(string email)
        {
            var url = ApiBase + "leaders/"
                + Uri.EscapeDataString(email)
                + "?managerEmail=" + Uri.EscapeDataString(SessionContext.Email ?? "");
            var resp = await _http.SendAsync(Req(HttpMethod.Delete, url));
            return resp.IsSuccessStatusCode;
        }
    }
}
