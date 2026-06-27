using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DynLock.Core
{
    public sealed class SupabaseSettings
    {
        public string ProjectUrl { get; set; }
        public string AnonKey { get; set; }
        public string ServiceKey { get; set; }
        public string SuperAdminEmail { get; set; }
    }

    internal sealed class SecretsFile
    {
        public string MasterKeyBase64 { get; set; }
    }

    internal sealed class SupabaseSettingsFile
    {
        public string ProjectUrl { get; set; }
        public string AnonKey { get; set; }
        public string ServiceKey { get; set; }
        public string SuperAdminEmail { get; set; }
    }

    public static class DynLockRuntimeConfig
    {
        public const string MasterKeyEnvVar = "DYNLOCK_MASTER_KEY_BASE64";
        public const string SupabaseUrlEnvVar = "DYNLOCK_SUPABASE_URL";
        public const string SupabaseAnonKeyEnvVar = "DYNLOCK_SUPABASE_ANON_KEY";
        public const string SupabaseServiceKeyEnvVar = "DYNLOCK_SUPABASE_SERVICE_KEY";
        public const string SupabaseAdminEmailEnvVar = "DYNLOCK_SUPABASE_ADMIN_EMAIL";

        public static string ConfigRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BIMLab", "DynLock");

        public static string SecretsConfigPath => Path.Combine(ConfigRoot, "secrets.json");
        public static string SupabaseConfigPath => Path.Combine(ConfigRoot, "supabase.json");

        public static string GetRequiredMasterKeyBase64()
        {
            string value = ResolveMasterKeyBase64();
            if (!IsConfiguredValue(value))
            {
                throw new InvalidOperationException(
                    "Missing DynLock master key. Set environment variable " + MasterKeyEnvVar +
                    " or create " + SecretsConfigPath + " with a valid MasterKeyBase64 value.");
            }

            return value.Trim();
        }

        public static byte[] GetRequiredMasterKeyBytes()
        {
            if (!TryGetMasterKeyBytes(out byte[] key, out string error))
                throw new InvalidOperationException(error);

            return key;
        }

        public static bool TryGetMasterKeyBytes(out byte[] key, out string error)
        {
            key = null;
            error = null;

            string base64 = ResolveMasterKeyBase64();
            if (!IsConfiguredValue(base64))
            {
                error =
                    "Missing DynLock master key. Set environment variable " + MasterKeyEnvVar +
                    " or create " + SecretsConfigPath + " with a valid MasterKeyBase64 value.";
                return false;
            }

            try
            {
                key = Convert.FromBase64String(base64.Trim());
                return true;
            }
            catch (FormatException ex)
            {
                error = "Invalid DynLock master key in environment or " + SecretsConfigPath + ": " + ex.Message;
                return false;
            }
        }

        public static SupabaseSettings GetRequiredSupabaseSettings()
        {
            if (!TryLoadSupabaseSettings(out SupabaseSettings settings, out string error))
                throw new InvalidOperationException(error);

            return settings;
        }

        public static bool TryLoadSupabaseSettings(out SupabaseSettings settings, out string error)
        {
            settings = LoadSupabaseSettingsCore();
            var missing = GetMissingSupabaseFields(settings).ToList();
            if (missing.Count > 0)
            {
                error =
                    "Missing DynLock Supabase settings: " + string.Join(", ", missing) +
                    ". Set the corresponding environment variables or create " + SupabaseConfigPath + ".";
                return false;
            }

            error = null;
            return true;
        }

        public static IEnumerable<string> GetMissingSupabaseFields(SupabaseSettings settings)
        {
            if (settings == null)
            {
                yield return nameof(SupabaseSettings.ProjectUrl);
                yield return nameof(SupabaseSettings.AnonKey);
                yield return nameof(SupabaseSettings.ServiceKey);
                yield return nameof(SupabaseSettings.SuperAdminEmail);
                yield break;
            }

            if (!IsConfiguredValue(settings.ProjectUrl)) yield return nameof(SupabaseSettings.ProjectUrl);
            if (!IsConfiguredValue(settings.AnonKey)) yield return nameof(SupabaseSettings.AnonKey);
            if (!IsConfiguredValue(settings.ServiceKey)) yield return nameof(SupabaseSettings.ServiceKey);
            if (!IsConfiguredValue(settings.SuperAdminEmail)) yield return nameof(SupabaseSettings.SuperAdminEmail);
        }

        private static SupabaseSettings LoadSupabaseSettingsCore()
        {
            var file = LoadJson<SupabaseSettingsFile>(SupabaseConfigPath) ?? new SupabaseSettingsFile();

            return new SupabaseSettings
            {
                ProjectUrl = FirstConfigured(
                    Environment.GetEnvironmentVariable(SupabaseUrlEnvVar),
                    file.ProjectUrl),
                AnonKey = FirstConfigured(
                    Environment.GetEnvironmentVariable(SupabaseAnonKeyEnvVar),
                    file.AnonKey),
                ServiceKey = FirstConfigured(
                    Environment.GetEnvironmentVariable(SupabaseServiceKeyEnvVar),
                    file.ServiceKey),
                SuperAdminEmail = FirstConfigured(
                    Environment.GetEnvironmentVariable(SupabaseAdminEmailEnvVar),
                    file.SuperAdminEmail),
            };
        }

        private static string ResolveMasterKeyBase64()
        {
            string env = Environment.GetEnvironmentVariable(MasterKeyEnvVar);
            if (IsConfiguredValue(env))
                return env.Trim();

            var file = LoadJson<SecretsFile>(SecretsConfigPath);
            return file?.MasterKeyBase64;
        }

        private static string FirstConfigured(params string[] values)
        {
            foreach (string value in values)
            {
                if (IsConfiguredValue(value))
                    return value.Trim();
            }

            return null;
        }

        private static bool IsConfiguredValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
            if (value.StartsWith("<", StringComparison.Ordinal) ||
                value.StartsWith("YOUR", StringComparison.OrdinalIgnoreCase) ||
                value.IndexOf("CHANGE_ME", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return true;
        }

        private static T LoadJson<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
