using AutoMapper;
using E_Com.API.Helper;
using E_Com.Core.DTO;
using E_Com.Core.Entites;
using E_Com.Core.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using static E_Com.Core.DTO.OrderDTO;

namespace E_Com.API.Controllers
{
    public class AccountController : BaseController
    {
        private readonly UserManager<AppUser> _userManager;

        public AccountController(IUnitOfWork work, IMapper mapper, UserManager<AppUser> userManager) : base(work, mapper)
        {
            _userManager = userManager;
        }

        [HttpGet("IsUserAuth")]
        public IActionResult IsUserAuth()
        {
            return Ok(new { isAuthenticated = User?.Identity?.IsAuthenticated ?? false });
        }

        [Authorize]
        [HttpGet("is-admin")]
        public IActionResult IsAdmin()
        {
            return Ok(new { isAdmin = User.IsInRole("Admin") });
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

        [EnableRateLimiting("auth")]
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterDTO registerDTO)
        {
            Console.WriteLine("➡️ Register endpoint called");

            try
            {
                string result = await work.Auth.RegisterAsync(registerDTO);
                Console.WriteLine("✅ RegisterAsync result: " + result);

                if (result != "done")
                {
                    Console.WriteLine("⚠️ Register failed: " + result);
                    return BadRequest(new ResponseAPI(400, result));
                }

                return Ok(new ResponseAPI(200, result));
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Exception in Register: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new ResponseAPI(500, ex.Message));
            }
        }

        [EnableRateLimiting("auth")]
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            try
            {
                string result = await work.Auth.LoginEmail(loginDTO);

                if (result == "Invalid email or password." ||
                    result.StartsWith("Email not confirmed"))
                    return BadRequest(new ResponseAPI(400, result));

                Response.Cookies.Append("token", result, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    IsEssential = true,
                    Expires = DateTime.Now.AddDays(1)
                });

                return Ok(new ResponseAPI(200, "Login successful"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN ERROR] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[LOGIN STACK] {ex.StackTrace}");
                return StatusCode(500, new ResponseAPI(500, $"Login failed: {ex.Message}"));
            }
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

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var email = User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return NotFound();
            return Ok(new { email = user.Email, displayName = user.DisplayName });
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
        {
            var email = User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return NotFound();
            user.DisplayName = dto.DisplayName;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return Ok(new ResponseAPI(200, "Profile updated"));
            return BadRequest(new ResponseAPI(400, result.Errors.FirstOrDefault()?.Description ?? "Update failed"));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
        {
            var email = User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ResponseAPI(401, "Not logged in"));

            var result = await work.Auth.ChangePassword(email, dto);
            if (result == "done") return Ok(new ResponseAPI(200, "Password changed successfully"));
            return BadRequest(new ResponseAPI(400, result));
        }
    }
}
