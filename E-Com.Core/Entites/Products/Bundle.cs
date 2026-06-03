using System.ComponentModel.DataAnnotations.Schema;

namespace E_Com.Core.Entites.Products
{
    // "Buy together & save" — a curated set of products at a discounted total
    public class Bundle : BaseEntity<int>
    {
        public string   Name            { get; set; }
        public string   Description     { get; set; } = "";
        public decimal  DiscountPercent { get; set; } = 10;
        public bool     IsActive        { get; set; } = true;
        public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
        public virtual ICollection<BundleItem> Items { get; set; }
    }

    public class BundleItem : BaseEntity<int>
    {
        public int      BundleId  { get; set; }
        [ForeignKey(nameof(BundleId))]
        public virtual Bundle Bundle { get; set; }
        public int      ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }
    }
}
