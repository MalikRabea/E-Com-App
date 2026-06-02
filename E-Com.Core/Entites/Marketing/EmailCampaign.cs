namespace E_Com.Core.Entites.Marketing
{
    public class EmailCampaign : BaseEntity<int>
    {
        public string   Subject     { get; set; }
        public string   Body        { get; set; }
        public string   Segment     { get; set; } // All | VIP | Inactive | New
        public int      Recipients  { get; set; }
        public string   SentByUserId{ get; set; }
        public DateTime SentAt      { get; set; } = DateTime.UtcNow;
    }
}
