using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Entities;

namespace GameStore.Catalogo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GameController(IGameService gameService)
        {
            _gameService = gameService;
        }

        /// <summary>
        /// Get all available games
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var games = await _gameService.GetAllGames();
                return Ok(games);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get games available for authenticated user
        /// </summary>
        [HttpGet("available")]
        [Authorize]
        public async Task<IActionResult> GetAvailableForUser()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("nameid");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized();

                var games = await _gameService.GetAvailableGames(userId);
                return Ok(games);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get owned games for authenticated user
        /// </summary>
        [HttpGet("owned")]
        [Authorize]
        public async Task<IActionResult> GetOwnedGames()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("nameid");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized();

                var games = await _gameService.GetOwnedGames(userId);
                return Ok(games);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Buy a game (requires authentication)
        /// </summary>
        [HttpPost("{gameId}/buy")]
        [Authorize]
        public async Task<IActionResult> BuyGame(Guid gameId)
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("nameid");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized();

                await _gameService.BuyGame(gameId, userId);
                return Ok(new { message = "Game purchased successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Search games by name
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchGames([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Search term is required" });

            try
            {
                var games = await _gameService.GetAllGames();
                var results = games.Where(g => g.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
