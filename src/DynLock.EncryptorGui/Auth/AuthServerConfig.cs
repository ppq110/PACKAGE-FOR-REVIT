using DynLock.Core;

namespace DynLock.EncryptorGui.Auth
{
    internal static class AuthServerConfig
    {
        public static string AuthServerUrl => DynLockRuntimeConfig.GetRequiredAuthClientSettings().AuthServerUrl;
        public static string SuperAdminEmail => DynLockRuntimeConfig.GetRequiredAuthClientSettings().SuperAdminEmail;

        public static bool TryLoad(out AuthServerSettings settings, out string error)
        {
            return DynLockRuntimeConfig.TryLoadAuthClientSettings(out settings, out error);
        }

        public static string ConfigRoot => DynLockRuntimeConfig.ConfigRoot;
        public static string AuthServerConfigPath => DynLockRuntimeConfig.AuthServerConfigPath;
        public static string SecretsConfigPath => DynLockRuntimeConfig.SecretsConfigPath;
    }
}
