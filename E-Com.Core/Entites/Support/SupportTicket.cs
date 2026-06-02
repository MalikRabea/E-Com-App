namespace E_Com.Core.Entites.Support
{
    public class SupportTicket : BaseEntity<int>
    {
        public string   UserId    { get; set; }
        public string   UserEmail { get; set; }
        public string   Subject   { get; set; }
        public string   Category  { get; set; } = "General"; // General | Order | Payment | Product | Other
        public string   Status    { get; set; } = "Open";    // Open | Pending | Resolved | Closed
        public string   Priority  { get; set; } = "Normal";  // Low | Normal | High
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<TicketMessage> Messages { get; set; }
    }

    public class TicketMessage : BaseEntity<int>
    {
        public int      TicketId  { get; set; }
        public virtual SupportTicket Ticket { get; set; }
        public string   SenderId  { get; set; }
        public bool     IsAdmin   { get; set; } = false;
        public string   Body      { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
