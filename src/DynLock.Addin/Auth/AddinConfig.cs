using DynLock.Core;

namespace DynLock.Addin.Auth
{
    internal static class AddinConfig
    {
        public static string AuthServerUrl => DynLockRuntimeConfig.GetRequiredAuthClientSettings().AuthServerUrl;
    }
}
