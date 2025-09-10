using E_Com.Core.Entites;
using E_Com.Core.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace E_Com.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteRepository _favoriteRepo;
        private readonly UserManager<AppUser> _userManager;

        public FavoritesController(IFavoriteRepository favoriteRepo, UserManager<AppUser> userManager)
        {
            _favoriteRepo = favoriteRepo;
            _userManager = userManager;
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> AddToFavorites(int productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return Unauthorized();

            var existing = await _favoriteRepo.GetByUserAndProductAsync(user.Id, productId);
            if (existing != null)
                return BadRequest("Already in favorites");

            var favorite = new Favorite { UserId = user.Id, ProductId = productId };
            await _favoriteRepo.AddAsync(favorite);

            return Ok(favorite);
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromFavorites(int productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return Unauthorized();

            var favorite = await _favoriteRepo.GetByUserAndProductAsync(user.Id, productId);
            if (favorite == null)
                return NotFound();

            await _favoriteRepo.RemoveAsync(favorite);
            return Ok(new { message = "Removed from favorites" });
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return Unauthorized();

            var favorites = await _favoriteRepo.GetUserFavoritesAsync(user.Id);
            return Ok(favorites);
        }
    }
}
