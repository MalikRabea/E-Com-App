using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.DTO;
using E_Com.Core.Services;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace E_Com.infrastructure.Repositries.Service
{
    
    public class EmailService : IEmailService
    {//SMTP
        private readonly IConfiguration configuration;

        public EmailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendEmail(EmailDTO emailDTO)
        {
           MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("My Shop", configuration["EmailSetting:From"]));
            message.Subject = emailDTO.Subject;
            message.To.Add(new MailboxAddress(emailDTO.To, emailDTO.To));
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = emailDTO.Content
            };
            using (var smtp = new MailKit.Net.Smtp.SmtpClient())
            {

                try
                {
                    await smtp.ConnectAsync(configuration["EmailSetting:Smtp"],
                        int.Parse(configuration["EmailSetting:Port"]), MailKit.Security.SecureSocketOptions.StartTls
);
                    await smtp.AuthenticateAsync(configuration["EmailSetting:UserName"],
                        configuration["EmailSetting:Password"]);

                    await smtp.SendAsync(message);

                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                { 
                    smtp.Disconnect(true);
                    smtp.Dispose();
                }
            
            
            }
        }
    }
}
