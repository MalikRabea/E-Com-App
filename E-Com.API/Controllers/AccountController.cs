using AutoMapper;
using E_Com.API.Helper;
using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static E_Com.Core.DTO.OrderDTO;

namespace E_Com.API.Controllers
{
    public class AccountController : BaseController
    {
        public AccountController(IUnitOfWork work, IMapper mapper) : base(work, mapper)
        {
        }

        [HttpGet("IsUserAuth")]
        public IActionResult IsUserAuth()
        {
            return Ok(new { isAuthenticated = User?.Identity?.IsAuthenticated ?? false });
        }

        [HttpGet("get-address-for-user")]
        public async Task<IActionResult> GetAddress()
        {
            var email = User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ResponseAPI(401, "User not logged in"));

            var address = await work.Auth.getUserAddress(email);
            if (address == null)
                return NotFound(new ResponseAPI(404, "Address not found"));

            var result = mapper.Map<ShipAddressDTO>(address);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("update-address")]
        public async Task<IActionResult> UpdateAddress(ShipAddressDTO addressDTO)
        {
            var email = User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ResponseAPI(401, "User not logged in"));

            var address = mapper.Map<Address>(addressDTO);
            var result = await work.Auth.UpdateAddress(email, address);

            return result ? Ok(new ResponseAPI(200)) : BadRequest(new ResponseAPI(400, "Failed to update address"));
        }

        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Append("token", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Expires = DateTime.Now.AddDays(-1)
            });

            return Ok(new ResponseAPI(200, "Logged out"));
        }

        [Authorize]
        [HttpGet("get-user-name")]
        public IActionResult GetUserName()
        {
            var userName = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
                return Unauthorized(new ResponseAPI(401, "User name not found"));

            return Ok(new ResponseAPI(200, userName));
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterDTO registerDTO)
        {
            string result = await work.Auth.RegisterAsync(registerDTO);
            if (result != "done")
                return BadRequest(new ResponseAPI(400, result));

            return Ok(new ResponseAPI(200, result));
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            string result = await work.Auth.LoginEmail(loginDTO);
            if (result.StartsWith("please"))
                return BadRequest(new ResponseAPI(400, result));

            Response.Cookies.Append("token", result, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Domain = "localhost",
                Expires = DateTime.Now.AddDays(1)
            });

            return Ok(new ResponseAPI(200, "Login successful"));
        }

        [HttpPost("active-account")]
        public async Task<ActionResult<ActiveAccountDTO>> ActiveAccount(ActiveAccountDTO accountDTO)
        {
            var result = await work.Auth.ActiveAccount(accountDTO);
            return result ? Ok(new ResponseAPI(200)) : BadRequest(new ResponseAPI(400));
        }

        [HttpGet("send-email-forget-password")]
        public async Task<IActionResult> ForgetPassword(string email)
        {
            var result = await work.Auth.SendEmailForForgetPassword(email);
            return result ? Ok(new ResponseAPI(200)) : BadRequest(new ResponseAPI(400));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(RestPassowrdDTO restPasswordDTO)
        {
            var result = await work.Auth.Resetpassword(restPasswordDTO);
            if (result == "done")
                return Ok(new ResponseAPI(200));

            return BadRequest(new ResponseAPI(400));
        }
    }
}
