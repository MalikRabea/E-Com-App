using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.DTO;
using E_Com.Core.Entites.Order;

namespace E_Com.Core.Services
{
    public interface IOrderService
    {
        Task<Orders> CreateOrdersAsync(OrderDTO orderDTO, string BuyerEmail);
        Task<IReadOnlyList<OrderToReturnDTO>> GetAllOrdersForUserAsync(string BuyerEmail);
        Task<OrderToReturnDTO> GetOrderByIdAsync(int Id, string BuyerEmail);
        Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodAsync();
    }
}
