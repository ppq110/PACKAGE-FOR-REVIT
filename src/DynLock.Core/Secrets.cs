using System;

namespace DynLock.Core
{
    /// <summary>
    /// Loads the master encryption key from environment variables or a local config file.
    /// The key is no longer hardcoded in source code.
    /// </summary>
    public static class Secrets
    {
        public static string MasterKeyBase64 => DynLockRuntimeConfig.GetRequiredMasterKeyBase64();

        public static bool TryGetMasterKeyBytes(out byte[] key, out string error)
        {
            return DynLockRuntimeConfig.TryGetMasterKeyBytes(out key, out error);
        }

        public static byte[] GetMasterKeyBytes()
        {
            return DynLockRuntimeConfig.GetRequiredMasterKeyBytes();
        }
    }
}
