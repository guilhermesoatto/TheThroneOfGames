using Microsoft.AspNetCore.Mvc;
using TheThroneOfGames.API.Controllers.Base;
using TheThroneOfGames.API.Models.DTO;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.API.Controllers.Admin;

public class GameController : AdminControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<GameListDTO>), 200)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var games = await _gameService.GetAllAsync();
            var gameDtos = games.Select(g => new GameListDTO
            {
                Id = g.Id,
                Name = g.Name,
                Genre = g.Genre,
                Price = g.Price
            }).ToList();

            return Ok(gameDtos);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GameDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var game = await _gameService.GetByIdAsync(id);
            if (game == null)
            {
                return NotFoundById<GameEntity>(id);
            }

            var gameDto = new GameDTO
            {
                Id = game.Id,
                Name = game.Name,
                Genre = game.Genre,
                Price = game.Price
            };

            return Ok(gameDto);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(GameDTO), 201)]
    public async Task<IActionResult> Create([FromBody] GameDTO gameDto)
    {
        try
        {
            var game = new GameEntity
            {
                Name = gameDto.Name,
                Genre = gameDto.Genre,
                Price = gameDto.Price
            };

            await _gameService.AddAsync(game);

            gameDto.Id = game.Id;
            return CreatedAtAction(nameof(GetById), new { id = game.Id }, gameDto);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GameDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] GameDTO gameDto)
    {
        try
        {
            var existingGame = await _gameService.GetByIdAsync(id);
            if (existingGame == null)
            {
                return NotFoundById<GameEntity>(id);
            }

            existingGame.Name = gameDto.Name;
            existingGame.Genre = gameDto.Genre;
            existingGame.Price = gameDto.Price;

            await _gameService.UpdateAsync(existingGame);

            gameDto.Id = id;
            return Ok(gameDto);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var game = await _gameService.GetByIdAsync(id);
            if (game == null)
            {
                return NotFoundById<GameEntity>(id);
            }

            await _gameService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}