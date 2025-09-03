using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.Entites;

namespace E_Com.Core.interfaces
{
    public interface ICustomerBasketRepository
    {
        Task<CustomerBasket> GetBasketAsync(string id);
        Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket);
        Task<bool> DeleteBasketAsync(string id);
    }
}
