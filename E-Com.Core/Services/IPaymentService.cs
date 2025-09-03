using E_Com.Core.Entites;
using E_Com.Core.Entites.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.Core.Services
{
    public interface IPaymentService
    {
        Task<CustomerBasket> CreateOrUpdatePaymentAsync(string basketId,int?deliveryId);
        Task<Orders> UpdateOrderSuccess(string PaymentInten);
        Task<Orders> UpdateOrderFaild(string PaymentInten);
    }
}
