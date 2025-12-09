using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheThroneOfGames.Application.Interface;

namespace TheThroneOfGames.API.Controllers;

[ApiController]
[Route("api/game")]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    // GET /api/game - accessible to authenticated users (User or Admin)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get()
    {
        var games = await _gameService.GetAllGames();
        return Ok(games);
    }
}
