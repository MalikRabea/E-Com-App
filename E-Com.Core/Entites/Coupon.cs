using System;

namespace E_Com.Core.Entites
{
    public class Coupon : BaseEntity<int>
    {
        public string Code { get; set; } = "";
        public decimal DiscountPercent { get; set; }
        public int MaxUses { get; set; } = 100;
        public int CurrentUses { get; set; } = 0;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
