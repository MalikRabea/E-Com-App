namespace E_Com.Core.Entites
{
    public class ReturnRequest : BaseEntity<int>
    {
        public string UserId      { get; set; }
        public string UserEmail   { get; set; }
        public int    OrderId     { get; set; }
        public string Reason      { get; set; }
        public string Description { get; set; } = "";
        public string Status      { get; set; } = "Pending"; // Pending | Approved | Rejected
        public string AdminNote   { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt{ get; set; }
    }
}
