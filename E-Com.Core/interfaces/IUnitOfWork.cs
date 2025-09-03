using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.Core.interfaces
{
    public interface IUnitOfWork
    {
        public ICategoryRepositry CategoryRepositry { get; }

        public IPhotoRepositry PhotoRepositry { get; }

        public IProductRepositry ProductRepositry { get; }

        public ICustomerBasketRepository CustomerBasket { get; }

        public IAuth Auth { get; }

    }
}
