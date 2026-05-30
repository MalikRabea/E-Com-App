namespace E_Com.Core.Entites.Loyalty
{
    public enum PointsType { Earned, Redeemed, Bonus }

    public class PointsTransaction : BaseEntity<int>
    {
        public int LoyaltyAccountId { get; set; }
        public virtual LoyaltyAccount LoyaltyAccount { get; set; }
        public int Points { get; set; }
        public PointsType Type { get; set; }
        public string Description { get; set; }
        public int? OrderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
