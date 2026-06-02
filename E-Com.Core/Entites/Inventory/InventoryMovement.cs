namespace E_Com.Core.Entites.Inventory
{
    public class InventoryMovement : BaseEntity<int>
    {
        public int      ProductId   { get; set; }
        public string   ProductName { get; set; }
        public int      Change      { get; set; }   // +in / -out
        public int      NewStock    { get; set; }
        public string   Reason      { get; set; }   // "Order #12" | "Restock" | "Manual adjustment"
        public string?  PerformedBy { get; set; }
        public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    }
}
