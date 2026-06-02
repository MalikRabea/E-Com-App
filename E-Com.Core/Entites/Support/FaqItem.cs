namespace E_Com.Core.Entites.Support
{
    public class FaqItem : BaseEntity<int>
    {
        public string Question  { get; set; }
        public string Answer    { get; set; }
        public string Category  { get; set; } = "General";
        public int    SortOrder { get; set; } = 0;
        public bool   IsActive  { get; set; } = true;
    }
}
