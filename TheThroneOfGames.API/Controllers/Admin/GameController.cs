using Microsoft.AspNetCore.Mvc;
using TheThroneOfGames.API.Controllers.Base;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Entities;
using ApiDTO = TheThroneOfGames.API.Models.DTO;

namespace TheThroneOfGames.API.Controllers.Admin;

public class GameController : AdminControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ApiDTO.GameListDTO>), 200)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var games = await _gameService.GetAllAsync();

            var gameDtos = games.Select(g => new ApiDTO.GameListDTO
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
    [ProducesResponseType(typeof(ApiDTO.GameDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var game = await _gameService.GetByIdAsync(id);

            if (game == null)
            {
                return NotFound(new { Message = $"Jogo com ID {id} não encontrado" });
            }

            var gameDto = new ApiDTO.GameDTO
            {
                Id = game.Id,
                Name = game.Name,
                Genre = game.Genre,
                Price = game.Price,
                Description = game.Description,
                CreatedAt = game.CreatedAt,
                UpdatedAt = game.UpdatedAt,
                IsAvailable = game.IsAvailable
            };

            return Ok(gameDto);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDTO.GameDTO), 201)]
    public async Task<IActionResult> Create([FromBody] ApiDTO.GameDTO gameDto)
    {
        try
        {
            var game = new GameEntity
            {
                Id = Guid.NewGuid(),
                Name = gameDto.Name,
                Genre = gameDto.Genre,
                Price = gameDto.Price,
                Description = gameDto.Description,
                IsAvailable = gameDto.IsAvailable
            };

            await _gameService.AddAsync(game);

            var createdDto = new ApiDTO.GameDTO
            {
                Id = game.Id,
                Name = game.Name,
                Genre = game.Genre,
                Price = game.Price,
                Description = game.Description,
                CreatedAt = game.CreatedAt,
                IsAvailable = game.IsAvailable
            };

            return CreatedAtAction(nameof(GetById), new { id = game.Id }, createdDto);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiDTO.GameDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ApiDTO.GameDTO gameDto)
    {
        try
        {
            var game = await _gameService.GetByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { Message = $"Jogo com ID {id} não encontrado" });
            }

            game.Name = gameDto.Name;
            game.Genre = gameDto.Genre;
            game.Price = gameDto.Price;
            game.Description = gameDto.Description;
            game.IsAvailable = gameDto.IsAvailable;
            game.UpdatedAt = DateTime.UtcNow;

            await _gameService.UpdateAsync(game);

            var updatedDto = new ApiDTO.GameDTO
            {
                Id = game.Id,
                Name = game.Name,
                Genre = game.Genre,
                Price = game.Price,
                Description = game.Description,
                CreatedAt = game.CreatedAt,
                UpdatedAt = game.UpdatedAt,
                IsAvailable = game.IsAvailable
            };

            return Ok(updatedDto);
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
                return NotFound(new { Message = $"Jogo com ID {id} não encontrado" });
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
