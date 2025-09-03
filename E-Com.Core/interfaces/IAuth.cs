using E_Com.Core.DTO;
using E_Com.Core.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static E_Com.Core.DTO.OrderDTO;

namespace E_Com.Core.interfaces
{
    public interface IAuth
    {
        Task<string> RegisterAsync(RegisterDTO registerDTO);

        Task<string> LoginEmail(LoginDTO login);

        Task<bool> SendEmailForForgetPassword(string email);

        Task<string> Resetpassword(RestPassowrdDTO restPassowrd);

        Task<bool> ActiveAccount(ActiveAccountDTO accountDTO);

        Task<bool> UpdateAddress(string email, Address address);

        Task<Address> getUserAddress(string email);

    }
}
