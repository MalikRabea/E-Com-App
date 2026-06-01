using System.ComponentModel.DataAnnotations.Schema;
using E_Com.Core.Entites.Products;

namespace E_Com.Core.Entites
{
    public class Subscription : BaseEntity<int>
    {
        public string   UserId          { get; set; }
        public string   UserEmail       { get; set; }
        public int      ProductId       { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product   { get; set; }
        public int      Quantity        { get; set; } = 1;
        public string   Interval        { get; set; } = "Monthly"; // Weekly | Monthly | Quarterly
        public decimal  DiscountPercent { get; set; } = 10;
        public DateTime NextDeliveryDate{ get; set; }
        public bool     IsActive        { get; set; } = true;
        public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
        public DateTime? LastProcessed  { get; set; }
    }
}
