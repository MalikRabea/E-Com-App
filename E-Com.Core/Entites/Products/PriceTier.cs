using System.ComponentModel.DataAnnotations.Schema;

namespace E_Com.Core.Entites.Products
{
    // Tiered / wholesale pricing — lower unit price when buying a minimum quantity
    public class PriceTier : BaseEntity<int>
    {
        public int     ProductId   { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }
        public int     MinQuantity { get; set; }   // e.g. 5
        public decimal UnitPrice   { get; set; }   // discounted per-unit price at this quantity
    }
}
