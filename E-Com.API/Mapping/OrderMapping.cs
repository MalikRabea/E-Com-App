using AutoMapper;
using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.Entites.Order;
using static E_Com.Core.DTO.OrderDTO;

namespace E_Com.API.Mapping
{
    public class OrderMapping:Profile
    {
        public OrderMapping()
        {
            CreateMap<Orders, OrderToReturnDTO>()
               .ForMember(d => d.deliveryMethod,
               o => o.
               MapFrom(s => s.deliveryMethod.Name))
               .ReverseMap();

            CreateMap<OrderItem, OrderItemDTO>().ReverseMap();
            CreateMap<ShippingAddress, ShipAddressDTO>().ReverseMap();
            CreateMap<Address, ShipAddressDTO>().ReverseMap();
        }
    }
}
