using System;
using System.Net;
using Newtonsoft.Json.Linq;

#pragma warning disable SYSLIB0014

namespace DynLock.Addin.Auth
{
    internal static class AddinAuthService
    {
        public static string LastError { get; private set; }

        public static bool TryLogin(string rawEmail)
        {
            LastError = null;
            string email = NormalizeGmail(rawEmail);
            if (email == null)
                return false;

            try
            {
                string url = AddinConfig.AuthServerUrl + "/api/auth/check?email="
                    + Uri.EscapeDataString(email);

                using (var wc = new WebClient())
                {
                    wc.Headers.Add("Accept", "application/json");

                    string json = wc.DownloadString(url);
                    var row = JObject.Parse(json);
                    if (row["isActive"]?.ToObject<bool>() != true) return false;

                    AddinSession.Email    = row["email"]?.ToString()    ?? email;
                    AddinSession.FullName = row["fullName"]?.ToString() ?? "";
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (ex is WebException webEx &&
                    webEx.Response is HttpWebResponse resp &&
                    resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                LastError = ex.Message;
                return false;
            }
        }

        public static bool IsGmail(string rawEmail)
        {
            return NormalizeGmail(rawEmail) != null;
        }

        private static string NormalizeGmail(string rawEmail)
        {
            string email = (rawEmail ?? "").Trim().ToLowerInvariant();
            return email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)
                && email.Length > "@gmail.com".Length
                ? email
                : null;
        }

    }
}

#pragma warning restore SYSLIB0014
