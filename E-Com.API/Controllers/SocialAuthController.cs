using E_Com.Core.Entites;
using E_Com.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SocialAuthController : ControllerBase
    {
        private readonly UserManager<AppUser>  _userManager;
        private readonly IGenerateToken        _tokenService;
        private readonly IHttpClientFactory    _httpClientFactory;

        public SocialAuthController(
            UserManager<AppUser> userManager,
            IGenerateToken tokenService,
            IHttpClientFactory httpClientFactory)
        {
            _userManager       = userManager;
            _tokenService      = tokenService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDTO dto)
        {
            if (string.IsNullOrEmpty(dto.AccessToken))
                return BadRequest("Access token required");

            // Verify token with Google
            GoogleUserInfo? googleUser = null;
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", dto.AccessToken);

                var resp = await client.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                if (!resp.IsSuccessStatusCode)
                    return Unauthorized("Invalid Google token");

                var json  = await resp.Content.ReadAsStringAsync();
                googleUser = JsonSerializer.Deserialize<GoogleUserInfo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return Unauthorized("Could not verify Google token");
            }

            if (googleUser?.Email == null) return Unauthorized("No email from Google");

            // Find or create user
            var user = await _userManager.FindByEmailAsync(googleUser.Email);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName       = googleUser.Email,
                    Email          = googleUser.Email,
                    EmailConfirmed = true,
                    DisplayName    = googleUser.Name ?? googleUser.Email.Split('@')[0]
                };
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                    return BadRequest("Could not create user account");

                await _userManager.AddToRoleAsync(user, "User");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GetAndCreateToken(user, roles);

            // Store token in cookie same as regular login
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.None,
                Expires  = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("token", token, cookieOptions);

            return Ok(new { user.DisplayName, user.Email, Token = token });
        }

        private class GoogleUserInfo
        {
            public string? Sub     { get; set; }
            public string? Email   { get; set; }
            public string? Name    { get; set; }
            public string? Picture { get; set; }
        }
    }

    public class GoogleLoginDTO { public string AccessToken { get; set; } }
}
