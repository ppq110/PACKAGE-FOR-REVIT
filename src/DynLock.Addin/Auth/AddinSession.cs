namespace DynLock.Addin.Auth
{
    internal static class AddinSession
    {
        public static string Email     { get; set; }
        public static string FullName  { get; set; }
        public static bool   IsLoggedIn => !string.IsNullOrEmpty(Email);

        public static void Clear()
        {
            Email    = null;
            FullName = null;
        }
    }
}
