using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using E_Com.Core.DTO;
using E_Com.Core.Entites.Order;
using E_Com.Core.interfaces;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace E_Com.infrastructure.Repositries.Service
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPaymentService _paymentService;

        public OrderService(IUnitOfWork unitOfWork, AppDbContext context, IMapper mapper, IPaymentService paymentService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _mapper = mapper;
            _paymentService = paymentService;
        }
        public async Task<Orders> CreateOrdersAsync(OrderDTO orderDTO, string BuyerEmail)
        {
            var basket = await _unitOfWork.CustomerBasket.GetBasketAsync(orderDTO.basketId);

            List<OrderItem> orderItems = new List<OrderItem>();

            foreach (var item in basket.basketItems)
            {
                var product = await _unitOfWork.ProductRepositry.GetByIdAsync(item.Id);

                if (product == null)
                    throw new Exception($"Product with Id {item.Id} not found");

                // ⬅️ هنا تحديث عدد مرات البيع
                product.SoldCount += item.Quantity;
                await _unitOfWork.ProductRepositry.UpdateAsync(product);

                var orderItem = new OrderItem(
                    product.Id,
                    item.Image,
                    product.Name,
                    item.Price,
                    item.Quantity
                );

                orderItems.Add(orderItem);
            }

            var deliverMethod = await _context.DeliveryMethods
                .FirstOrDefaultAsync(m => m.Id == orderDTO.deliveryMethodId);

            var subTotal = orderItems.Sum(m => m.Price * m.Quantity);

            var ship = _mapper.Map<ShippingAddress>(orderDTO.shipAddress);

            var existingOrder = await _context.Orders
                .FirstOrDefaultAsync(m => m.PaymentIntentId == basket.PaymentIntentId);

            if (existingOrder is not null)
            {
                _context.Orders.Remove(existingOrder);
                await _paymentService.CreateOrUpdatePaymentAsync(basket.PaymentIntentId, deliverMethod.Id);
            }

            var order = new Orders(
                BuyerEmail,
                subTotal,
                ship,
                deliverMethod,
                orderItems,
                basket.PaymentIntentId
            );
            order.OrderDate = DateTime.UtcNow;


            await _context.Orders.AddAsync(order);

            // هنا الحفظ رح يشمل كمان تحديث الـ SoldCount للمنتجات
            await _context.SaveChangesAsync();

            // Clear the basket after order creation
            await _unitOfWork.CustomerBasket.DeleteBasketAsync(orderDTO.basketId);

            return order;
        }


        public async Task<IReadOnlyList<OrderToReturnDTO>> GetAllOrdersForUserAsync(string BuyerEmail)
        {
            // جلب الطلبات مع العناصر وطريقة التوصيل مع تأمين الـ Product
            var orders = await _context.Orders
                .Where(o => o.BuyerEmail == BuyerEmail)
                .Include(o => o.orderItems)
                .Include(o => o.deliveryMethod)
                .ToListAsync();

            // إذا لم توجد طلبات، أرجع لائحة فارغة
            if (orders == null || !orders.Any())
                return new List<OrderToReturnDTO>();

            // تحويل باستخدام AutoMapper
            var result = _mapper.Map<IReadOnlyList<OrderToReturnDTO>>(orders);

            // تأمين كل OrderItem داخل كل Order
            foreach (var orderDto in result)
            {
                if (orderDto.orderItems != null)
                {
                    foreach (var item in orderDto.orderItems)
                    {
                        item.ProductName ??= "";
                        item.MainImage ??= "";
                    }
                }
                else
                {
                    orderDto.orderItems = new List<OrderItemDTO>();
                }

               

            }

            // ترتيب الطلبات حسب Id تنازلي
            result = result.OrderByDescending(o => o.Id).ToList();

            return result;
        }


        public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodAsync()
        => await _context.DeliveryMethods.AsNoTracking().ToListAsync();

        public async Task<OrderToReturnDTO> GetOrderByIdAsync(int Id, string BuyerEmail)
        {
           var order = await _context.Orders.Where(m=>m.Id==Id&&m.BuyerEmail==BuyerEmail)
                .Include( inc=>inc.orderItems).Include( inc=>inc.deliveryMethod)
                .FirstOrDefaultAsync();
            var result = _mapper.Map<OrderToReturnDTO>(order);
            return result;
        }
    }
}
