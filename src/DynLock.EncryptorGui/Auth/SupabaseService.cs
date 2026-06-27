using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DynLock.EncryptorGui.Auth
{
    internal static class SupabaseService
    {
        static readonly HttpClient           _http = new HttpClient();
        static readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        static readonly string               _base = SupabaseConfig.ProjectUrl + "/rest/v1/";

        //  Helpers 

        static HttpRequestMessage Req(HttpMethod method, string url, string key, string body = null)
        {
            var r = new HttpRequestMessage(method, url);
            r.Headers.Add("apikey", key);
            r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
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

        //  Login 

        // Kiểm tra email. Nếu hợp lệ -> cập nhật SessionContext và trả về true.
        public static async Task<bool> LoginAsync(string email)
        {
            var url = _base
                + "authorized_leaders"
                + "?email=eq." + Uri.EscapeDataString(email)
                + "&is_active=eq.true"
                + "&select=email,full_name,can_manage"
                + "&limit=1";

            var resp = await _http.SendAsync(Req(HttpMethod.Get, url, SupabaseConfig.AnonKey));
            if (!resp.IsSuccessStatusCode) return false;

            var list = _json.Deserialize<List<Dictionary<string, object>>>(
                await resp.Content.ReadAsStringAsync());

            if (list == null || list.Count == 0) return false;

            var row = list[0];
            SessionContext.Email     = Get<string>(row, "email");
            SessionContext.FullName  = Get<string>(row, "full_name", "");
            SessionContext.CanManage = Get<bool>(row, "can_manage", false);

            _ = UpdateLastLoginAsync(email);   // fire-and-forget
            return true;
        }

        public static async Task UpdateLastLoginAsync(string email)
        {
            try
            {
                var url  = _base + "authorized_leaders?email=eq." + Uri.EscapeDataString(email);
                var body = _json.Serialize(new Dictionary<string, object>
                    { ["last_login"] = DateTime.UtcNow.ToString("o") });
                await _http.SendAsync(Req(new HttpMethod("PATCH"), url, SupabaseConfig.ServiceKey, body));
            }
            catch { }
        }

        //  Management 

        // Lấy toàn bộ danh sách leader (yêu cầu ServiceKey).
        public static async Task<List<LeaderInfo>> GetAllLeadersAsync()
        {
            var url = _base
                + "authorized_leaders"
                + "?order=created_at.asc"
                + "&select=email,full_name,is_active,can_manage,added_by,created_at,last_login";

            var resp = await _http.SendAsync(Req(HttpMethod.Get, url, SupabaseConfig.ServiceKey));
            resp.EnsureSuccessStatusCode();

            var raw    = _json.Deserialize<List<Dictionary<string, object>>>(
                await resp.Content.ReadAsStringAsync());
            var result = new List<LeaderInfo>();

            foreach (var row in raw)
                result.Add(new LeaderInfo
                {
                    Email     = Get<string>(row, "email"),
                    FullName  = Get<string>(row, "full_name", ""),
                    IsActive  = Get<bool>(row, "is_active", false),
                    CanManage = Get<bool>(row, "can_manage", false),
                    AddedBy   = Get<string>(row, "added_by", ""),
                    CreatedAt = Get<string>(row, "created_at", ""),
                    LastLogin = Get<string>(row, "last_login", ""),
                });

            return result;
        }

        // Thêm leader moi. Email phai chua ton tai trong DB.
        public static async Task<bool> AddLeaderAsync(string email, string fullName)
        {
            var url  = _base + "authorized_leaders";
            var body = _json.Serialize(new Dictionary<string, object>
            {
                ["email"]      = email,
                ["full_name"]  = fullName,
                ["is_active"]  = true,
                ["can_manage"] = false,
                ["added_by"]   = SessionContext.Email,
            });

            var req = Req(HttpMethod.Post, url, SupabaseConfig.ServiceKey, body);
            req.Headers.Add("Prefer", "return=minimal");
            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        // Bat hoac tat quyen truy cap cua mot leader.
        public static async Task<bool> SetActiveAsync(string email, bool isActive)
        {
            var url  = _base + "authorized_leaders?email=eq." + Uri.EscapeDataString(email);
            var body = _json.Serialize(new Dictionary<string, object> { ["is_active"] = isActive });
            var resp = await _http.SendAsync(Req(new HttpMethod("PATCH"), url, SupabaseConfig.ServiceKey, body));
            return resp.IsSuccessStatusCode;
        }

        // Xóa han mot leader khoi DB.
        public static async Task<bool> DeleteLeaderAsync(string email)
        {
            var url  = _base + "authorized_leaders?email=eq." + Uri.EscapeDataString(email);
            var resp = await _http.SendAsync(Req(new HttpMethod("DELETE"), url, SupabaseConfig.ServiceKey));
            return resp.IsSuccessStatusCode;
        }
    }
}
