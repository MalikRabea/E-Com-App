namespace E_Com.Core.Entites.Security
{
    public class OtpCode : BaseEntity<int>
    {
        public string   Email     { get; set; }
        public string   Code      { get; set; }
        public string   Purpose   { get; set; } = "Login"; // Login | Verify
        public bool     Used      { get; set; } = false;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
