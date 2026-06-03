namespace E_Com.Core.Entites.Products
{
    // Temporarily holds stock for a basket during checkout so it can't be oversold
    public class StockReservation : BaseEntity<int>
    {
        public int      ProductId  { get; set; }
        public string   BasketId   { get; set; }
        public int      Quantity   { get; set; }
        public DateTime ExpiresAt  { get; set; }
        public bool     Released   { get; set; } = false;
        public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    }
}
