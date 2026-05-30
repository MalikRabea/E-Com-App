namespace E_Com.Core.Entites.Loyalty
{
    public class LoyaltyAccount : BaseEntity<int>
    {
        public string UserId { get; set; }
        public int Points { get; set; } = 0;
        public string Tier { get; set; } = "Bronze";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<PointsTransaction> Transactions { get; set; }
    }
}
