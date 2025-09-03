using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using E_Com.Core.Entites;
using E_Com.Core.interfaces;
using StackExchange.Redis;

namespace E_Com.infrastructure.Repositries
{
    public class CustomerBasketRepository : ICustomerBasketRepository
    {
        private readonly IDatabase _database;
        public CustomerBasketRepository(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }
        public Task<bool> DeleteBasketAsync(string id)
        {
            return _database.KeyDeleteAsync(id);
        }

        public async Task<CustomerBasket> GetBasketAsync(string id)
        {
            var result = await _database.StringGetAsync(id);
            if (!string.IsNullOrEmpty(result))
            { 
                return  JsonSerializer.Deserialize<CustomerBasket>(result);
            }
            return null;
        }

        public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
        {
         
            var _basket = await _database.StringSetAsync(basket.Id.ToString(), JsonSerializer.Serialize(basket),TimeSpan.FromDays(3));
            if (_basket )
            { 
                return await GetBasketAsync(basket.Id);

            }
            return null;
        }

    }


}
