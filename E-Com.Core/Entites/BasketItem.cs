namespace E_Com.Core.Entites
{
    public class BasketItem
    {
        public int Id { get; set; } //key
        public string Name { get; set; }//name of the product
        public string Description { get; set; }
        public string Image { get; set; } //image of the product
        public int Quantity { get; set; } //quantity of the product in the basket
        public decimal Price { get; set; } //price of the product
        public string Category { get; set; } 
    }
}