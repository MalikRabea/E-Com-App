namespace E_Com.Core.Entites
{
    public class AbandonedCartTracker : BaseEntity<int>
    {
        public string    UserEmail       { get; set; }
        public string    BasketId        { get; set; }
        public string?   PaymentIntentId { get; set; }
        public DateTime  CreatedAt       { get; set; } = DateTime.UtcNow;
        public bool      EmailSent       { get; set; } = false;
        public DateTime? EmailSentAt     { get; set; }
    }
}
