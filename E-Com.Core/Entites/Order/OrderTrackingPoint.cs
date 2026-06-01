namespace E_Com.Core.Entites.Order
{
    public class OrderTrackingPoint : BaseEntity<int>
    {
        public int      OrderId   { get; set; }
        public string   Status    { get; set; }   // e.g. "Processing", "Shipped", "Out for delivery", "Delivered"
        public string   Location  { get; set; }   // human-readable place name
        public double   Latitude  { get; set; }
        public double   Longitude { get; set; }
        public string?  Note      { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
