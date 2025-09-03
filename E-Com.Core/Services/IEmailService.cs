using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.DTO;

namespace E_Com.Core.Services
{
    public interface IEmailService
    {
        Task SendEmail(EmailDTO emailDTO);
    }
}
