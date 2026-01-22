using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GameStore.Catalogo.Application.Services;

namespace GameStore.Catalogo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameService _gameService;

        public GameController(GameService gameService)
        {
            _gameService = gameService;
        }

        /// <summary>
        /// Get all games
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
        /// Get game by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var game = await _gameService.GetByIdAsync(id);
                if (game == null)
                    return NotFound();
                
                return Ok(game);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get available games (in stock)
        /// </summary>
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailable()
        {
            try
            {
                var games = await _gameService.GetAvailableGames();
                return Ok(games);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get games by genre
        /// </summary>
        [HttpGet("genre/{genero}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByGenre(string genero)
        {
            try
            {
                var games = await _gameService.GetGamesByGenre(genero);
                return Ok(games);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get games by price range
        /// </summary>
        [HttpGet("price")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByPriceRange([FromQuery] decimal min, [FromQuery] decimal max)
        {
            try
            {
                var games = await _gameService.GetGamesByPriceRange(min, max);
                return Ok(games);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
