using System.ComponentModel.DataAnnotations.Schema;

namespace E_Com.Core.Entites.Products
{
    public class ProductVariant : BaseEntity<int>
    {
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }
        public string Type { get; set; }  // "Color", "Size"
        public virtual ICollection<VariantOption> Options { get; set; }
    }

    public class VariantOption : BaseEntity<int>
    {
        public int VariantId { get; set; }
        [ForeignKey(nameof(VariantId))]
        public virtual ProductVariant Variant { get; set; }
        public string Value { get; set; }       // "Red", "XL"
        public int Stock { get; set; } = 0;
        public decimal PriceAdjustment { get; set; } = 0;
    }
}
