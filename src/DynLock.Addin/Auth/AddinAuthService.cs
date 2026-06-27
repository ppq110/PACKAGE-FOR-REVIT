using System;
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
                string url   = AddinConfig.ProjectUrl + "/rest/v1/authorized_leaders"
                             + "?email=eq."       + Uri.EscapeDataString(email)
                             + "&is_active=eq.true"
                             + "&select=email,full_name"
                             + "&limit=1";

                using (var wc = new System.Net.WebClient())
                {
                    wc.Headers.Add("apikey",        AddinConfig.AnonKey);
                    wc.Headers.Add("Authorization", "Bearer " + AddinConfig.AnonKey);
                    wc.Headers.Add("Accept",        "application/json");

                    string json = wc.DownloadString(url);
                    var arr = JArray.Parse(json);
                    if (arr.Count == 0) return false;

                    var row = (JObject)arr[0];
                    AddinSession.Email    = row["email"]?.ToString()     ?? email;
                    AddinSession.FullName = row["full_name"]?.ToString() ?? "";
                    return true;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }
    }
}

#pragma warning restore SYSLIB0014
