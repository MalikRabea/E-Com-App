using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.Entites.Products;
using E_Com.Core.interfaces;
using E_Com.infrastructure.Data;

namespace E_Com.infrastructure.Repositries
{
    public class PhotoRepositry : GenericRepositry<Photo>, IPhotoRepositry
    {
        public PhotoRepositry(AppDbContext context) : base(context)
        {
        }
    }
}
