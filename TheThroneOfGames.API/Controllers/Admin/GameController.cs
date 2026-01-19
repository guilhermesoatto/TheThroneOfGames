using Microsoft.AspNetCore.Mvc;
using TheThroneOfGames.API.Controllers.Base;
using GameStore.CQRS.Abstractions;
using GameStore.Catalogo.Application.Commands;
using GameStore.Catalogo.Application.Queries;
using CatalogoDTO = GameStore.Catalogo.Application.DTOs.GameDTO;
using ApiDTO = TheThroneOfGames.API.Models.DTO;

namespace TheThroneOfGames.API.Controllers.Admin;

public class GameController : AdminControllerBase
{
    private readonly ICommandHandler<CreateGameCommand> _createGameHandler;
    private readonly ICommandHandler<UpdateGameCommand> _updateGameHandler;
    private readonly ICommandHandler<RemoveGameCommand> _removeGameHandler;
    private readonly IQueryHandler<GetAllGamesQuery, IEnumerable<CatalogoDTO>> _getAllGamesHandler;
    private readonly IQueryHandler<GetGameByIdQuery, CatalogoDTO?> _getGameByIdHandler;

    public GameController(
        ICommandHandler<CreateGameCommand> createGameHandler,
        ICommandHandler<UpdateGameCommand> updateGameHandler,
        ICommandHandler<RemoveGameCommand> removeGameHandler,
        IQueryHandler<GetAllGamesQuery, IEnumerable<CatalogoDTO>> getAllGamesHandler,
        IQueryHandler<GetGameByIdQuery, CatalogoDTO?> getGameByIdHandler)
    {
        _createGameHandler = createGameHandler;
        _updateGameHandler = updateGameHandler;
        _removeGameHandler = removeGameHandler;
        _getAllGamesHandler = getAllGamesHandler;
        _getGameByIdHandler = getGameByIdHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ApiDTO.GameListDTO>), 200)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var query = new GetAllGamesQuery();
            var games = await _getAllGamesHandler.HandleAsync(query);
            
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
            var query = new GetGameByIdQuery(id);
            var game = await _getGameByIdHandler.HandleAsync(query);
            
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
            var command = new CreateGameCommand(
                Name: gameDto.Name,
                Genre: gameDto.Genre,
                Price: gameDto.Price,
                Description: gameDto.Description ?? ""
            );

            var result = await _createGameHandler.HandleAsync(command);

            if (!result.Success)
            {
                return BadRequest(new { result.Message, result.Errors });
            }

            var createdGame = result.Data as CatalogoDTO;
            return CreatedAtAction(nameof(GetById), new { id = result.EntityId }, createdGame);
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
            var command = new UpdateGameCommand(
                GameId: id,
                Name: gameDto.Name,
                Genre: gameDto.Genre,
                Price: gameDto.Price,
                Description: gameDto.Description ?? ""
            );

            var result = await _updateGameHandler.HandleAsync(command);

            if (!result.Success)
            {
                if (result.Message.Contains("não encontrado"))
                {
                    return NotFound(new { result.Message });
                }
                return BadRequest(new { result.Message, result.Errors });
            }

            var updatedGame = result.Data as CatalogoDTO;
            return Ok(updatedGame);
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
            var command = new RemoveGameCommand(id);
            var result = await _removeGameHandler.HandleAsync(command);

            if (!result.Success)
            {
                if (result.Message.Contains("não encontrado"))
                {
                    return NotFound(new { result.Message });
                }
                return BadRequest(new { result.Message, result.Errors });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}