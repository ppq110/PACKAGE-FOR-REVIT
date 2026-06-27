namespace DynLock.EncryptorGui.Auth
{
    // Luu thong tin leader dang dang nhap trong phien lam viec hien tai
    internal static class SessionContext
    {
        public static string Email     { get; set; }
        public static string FullName  { get; set; }
        public static bool   CanManage { get; set; }
    }
}
