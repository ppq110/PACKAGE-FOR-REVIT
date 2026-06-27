using System;
using System.Net;
using Newtonsoft.Json.Linq;

#pragma warning disable SYSLIB0014  // WebClient obsolete in net8 - don gian hon HttpClient cho sync call

namespace DynLock.Addin.Auth
{
    internal static class AddinAuthService
    {
        public static string LastError { get; private set; }

        public static bool TryLogin(string rawEmail)
        {
            LastError = null;
            try
            {
                string email = rawEmail.Trim().ToLowerInvariant();
                string url = AddinConfig.AuthServerUrl + "/api/auth/check?email="
                           + Uri.EscapeDataString(email);

                using (var wc = new System.Net.WebClient())
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
    }
}

#pragma warning restore SYSLIB0014
