namespace E_Com.Core.Entites.Marketing
{
    // One per user — their unique referral code + counters
    public class ReferralProfile : BaseEntity<int>
    {
        public string UserId        { get; set; }
        public string Code          { get; set; }
        public int    TotalReferred { get; set; } = 0;
        public int    PointsEarned  { get; set; } = 0;
        public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    }

    // A record of a successful referral (referred user placed first order)
    public class Referral : BaseEntity<int>
    {
        public string   ReferrerUserId { get; set; }
        public string   ReferredEmail  { get; set; }
        public string   Code           { get; set; }
        public string   Status         { get; set; } = "Pending"; // Pending | Completed
        public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt   { get; set; }
    }
}
