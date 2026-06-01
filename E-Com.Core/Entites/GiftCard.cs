namespace E_Com.Core.Entites
{
    public class GiftCard : BaseEntity<int>
    {
        public string   Code             { get; set; }
        public decimal  InitialBalance   { get; set; }
        public decimal  CurrentBalance   { get; set; }
        public string?  IssuedToEmail    { get; set; }
        public string?  PurchasedByUserId{ get; set; }
        public string?  Message          { get; set; }
        public bool     IsActive         { get; set; } = true;
        public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate      { get; set; }
    }
}
