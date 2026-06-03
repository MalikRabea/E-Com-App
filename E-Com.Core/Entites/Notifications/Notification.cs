namespace E_Com.Core.Entites.Notifications
{
    public class Notification : BaseEntity<int>
    {
        public string   UserId    { get; set; }   // recipient user id
        public string   Type      { get; set; } = "info"; // order | success | warning | promo | support | info
        public string   Icon      { get; set; } = "notifications";
        public string   Title     { get; set; }
        public string   Message   { get; set; }
        public string?  Link      { get; set; }
        public bool     IsRead    { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
