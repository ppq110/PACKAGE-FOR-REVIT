using DynLock.Core;

namespace DynLock.Addin.Auth
{
    internal static class AddinConfig
    {
        public static string ProjectUrl => DynLockRuntimeConfig.GetRequiredSupabaseSettings().ProjectUrl;
        public static string AnonKey => DynLockRuntimeConfig.GetRequiredSupabaseSettings().AnonKey;
    }
}
