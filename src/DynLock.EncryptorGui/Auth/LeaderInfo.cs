namespace DynLock.EncryptorGui.Auth
{
    internal class LeaderInfo
    {
        public string Email     { get; set; }
        public string FullName  { get; set; }
        public bool   IsActive  { get; set; }
        public bool   CanManage { get; set; }
        public string AddedBy   { get; set; }
        public string CreatedAt { get; set; }
        public string LastLogin { get; set; }
    }
}
