using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.interfaces;
using E_Com.Core.Services;
using E_Com.Core.Sharing;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace E_Com.infrastructure.Repositries
{
    public class AuthRepositry : IAuth
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IEmailService emailService;
        private readonly SignInManager<AppUser> signInManager;
        private readonly IGenerateToken generateToken;
        private readonly AppDbContext context;

        public AuthRepositry(UserManager<AppUser> userManager, IEmailService emailService, SignInManager<AppUser> signInManager, IGenerateToken generateToken, AppDbContext context)
        {
            this.userManager = userManager;
            this.emailService = emailService;
            this.signInManager = signInManager;
            this.generateToken = generateToken;
            this.context = context;
        }
        public async Task<string> RegisterAsync(RegisterDTO registerDTO)
        {
            try
            {
                if (registerDTO == null)
                {
                    Console.WriteLine("⚠️ RegisterAsync called with NULL DTO");
                    return null;
                }

                Console.WriteLine("➡️ Checking username: " + registerDTO.UserName);

                if (await userManager.FindByNameAsync(registerDTO.UserName) is not null)
                {
                    return "Username already exists";
                }

                if (await userManager.FindByEmailAsync(registerDTO.Email) is not null)
                {
                    return "Email already exists";
                }

                var user = new AppUser
                {
                    UserName = registerDTO.UserName,
                    Email = registerDTO.Email,
                    DisplayName = registerDTO.DisplayName,
                };

                Console.WriteLine("➡️ Creating user in Identity...");
                var result = await userManager.CreateAsync(user, registerDTO.Password);

                if (!result.Succeeded)
                {
                    var err = result.Errors.FirstOrDefault()?.Description;
                    Console.WriteLine("❌ CreateAsync failed: " + err);
                    return err;
                }

                Console.WriteLine("✅ User created successfully, generating email token...");
                string token = await userManager.GenerateEmailConfirmationTokenAsync(user);

                Console.WriteLine("➡️ Sending activation email...");
                //await SendEmail(user.Email, token, "active", "ActiveEmail", "please active your email ,click on button to active");

                Console.WriteLine("✅ Email sent successfully");
                return "done";
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Exception in RegisterAsync: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task SendEmail(string email , string code , string component , string subject ,string message)
        {
            var result = new EmailDTO(email,
                "02205a3a53fcdf",
                subject
                , EmailStringBody.send(email, code, component, message));
            await emailService.SendEmail(result);
            
        }
        public async Task<string> LoginEmail(LoginDTO login)
        {
            if (login == null) 
            {
                return null;
            }
            var finduser = await userManager.FindByEmailAsync(login.Email);
            if (!finduser.EmailConfirmed)
            {
                string token = await userManager.GenerateEmailConfirmationTokenAsync(finduser);
                await SendEmail(finduser.Email, token, "active", "ActiveEmail", "please active your email ,click on button to active");
                return "Email not confirmed, please check your email for confirmation link.";
            }
            var result = await signInManager.CheckPasswordSignInAsync(finduser, login.Password, true);
            if (result.Succeeded) {

                return generateToken.GetAndCreateToken(finduser);
            }
            return "Invalid email or password.";
        }
        public async Task<bool> SendEmailForForgetPassword(string email)
        { 
            var finduser = await userManager.FindByEmailAsync(email);
            if (finduser is null)
            {
                return false;
            }
            var token = await userManager.GeneratePasswordResetTokenAsync(finduser);
            await SendEmail(finduser.Email, token, "Reset-Password", "Reset Password", " click on button to Reset your password ");
            return true;



        }
        public async Task<string> Resetpassword(RestPassowrdDTO restPassowrd)
        {
            var findUser = await userManager.FindByEmailAsync(restPassowrd.Email);
            if (findUser is null)
            {
                return null;
            }

            var result = await userManager.ResetPasswordAsync(findUser, restPassowrd.Token, restPassowrd.Password);

            if (result.Succeeded) {

                return "Password change sucess";
            }

            return result.Errors.ToList()[0].Description;
        
        }
        public async Task<bool> ActiveAccount(ActiveAccountDTO accountDTO)
        {
            var findUser = await userManager.FindByEmailAsync(accountDTO.Email);
            if (findUser is null)
            {
                return false;
            }

            var reslt = await userManager.ConfirmEmailAsync(findUser, accountDTO.Token);
            if (reslt.Succeeded)
            
                return true;
            var token = await userManager.GenerateEmailConfirmationTokenAsync(findUser);

            await SendEmail(findUser.Email, token, "active", "ActiveEmail", "please active your email ,click on button to active");
            return false;

        }

        public async Task<bool> UpdateAddress(string email, Address address)
        {
            var findUser = await userManager.FindByEmailAsync(email);
            if (findUser is null)
            {
                return false;
            }
            var Myaddress = await context.Addresses.FirstOrDefaultAsync(m => m.AppUserId == findUser.Id);
            if (Myaddress is null)
            {
                address.AppUserId = findUser.Id;
                await context.Addresses.AddAsync(address);
            }
            else 
            {
                Myaddress.FirstName = address.FirstName;
                Myaddress.LastName = address.LastName;
                Myaddress.City = address.City;
                Myaddress.ZipCode = address.ZipCode;
                Myaddress.Street = address.Street;
                Myaddress.State = address.State;
            }
            await context.SaveChangesAsync();
            return true;

        }

        public async Task<Address> getUserAddress(string email)
        {
            var User = await userManager.FindByEmailAsync(email);
            var address = await context.Addresses.FirstOrDefaultAsync(m => m.AppUserId == User.Id);
            return address;
        }
    }
}
