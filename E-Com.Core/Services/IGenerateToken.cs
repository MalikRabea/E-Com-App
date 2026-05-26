using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.Entites;

namespace E_Com.Core.Services
{
    public interface IGenerateToken
    {
        string GetAndCreateToken(AppUser user);
    }
}
