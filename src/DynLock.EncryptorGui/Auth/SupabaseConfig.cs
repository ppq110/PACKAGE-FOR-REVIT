using DynLock.Core;

namespace DynLock.EncryptorGui.Auth
{
    internal static class SupabaseConfig
    {
        public static string ProjectUrl => DynLockRuntimeConfig.GetRequiredSupabaseSettings().ProjectUrl;
        public static string AnonKey => DynLockRuntimeConfig.GetRequiredSupabaseSettings().AnonKey;
        public static string ServiceKey => DynLockRuntimeConfig.GetRequiredSupabaseSettings().ServiceKey;
        public static string SuperAdminEmail => DynLockRuntimeConfig.GetRequiredSupabaseSettings().SuperAdminEmail;

        public static bool TryLoad(out SupabaseSettings settings, out string error)
        {
            return DynLockRuntimeConfig.TryLoadSupabaseSettings(out settings, out error);
        }

        public static string ConfigRoot => DynLockRuntimeConfig.ConfigRoot;
        public static string SupabaseConfigPath => DynLockRuntimeConfig.SupabaseConfigPath;
        public static string SecretsConfigPath => DynLockRuntimeConfig.SecretsConfigPath;
    }
}
