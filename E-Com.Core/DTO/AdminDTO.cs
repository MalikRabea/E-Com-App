namespace E_Com.Core.DTO
{
    public record UpdateStatusDTO { public string Status { get; set; } = ""; }

    public record UserDTO
    {
        public string Id          { get; set; } = "";
        public string Email       { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Role        { get; set; } = "";
    }

    public record MonthlySalesDTO
    {
        public int     Year    { get; set; }
        public int     Month   { get; set; }
        public decimal Revenue { get; set; }
        public int     Count   { get; set; }
    }

    public record AdminStatsDTO
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalUsers { get; set; }
        public List<RecentOrderDTO> RecentOrders { get; set; } = new();
    }

    public record RecentOrderDTO
    {
        public int Id { get; set; }
        public string BuyerEmail { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }
        public int ItemCount { get; set; }
    }

    public record AdminOrderDTO
    {
        public int Id { get; set; }
        public string BuyerEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public int ItemCount { get; set; }
        public string DeliveryMethod { get; set; }
    }

    public record AdminOrderListDTO
    {
        public List<AdminOrderDTO> Orders { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
