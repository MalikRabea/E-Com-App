using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace E_Com.Core.Entites
{
    public class CustomerBasket
    {
        public CustomerBasket()
        {

        }
        public CustomerBasket(string id)
        {
            Id = id;
        }
        public string Id { get; set; } //key

        public string? PaymentIntentId { get; set; } 
        public string? ClientSecret { get; set; } 
        public List<BasketItem> basketItems { get; set; } = new List<BasketItem>(); //value
    }
}
