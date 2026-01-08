using GameStore.Catalogo.Application.Commands;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using GameStore.Catalogo.Application.Queries;
using GameStore.Catalogo.API.Controllers.Base;
using GameStore.CQRS.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Catalogo.API.Controllers.Admin;

/// <summary>
/// Admin controller for game management in the Catalogo bounded context.
/// Uses CQRS pattern with Commands and Queries.
/// </summary>
public class GameController : AdminControllerBase
{
    private readonly ICommandHandler<CreateGameCommand> _createGameHandler;
    private readonly ICommandHandler<UpdateGameCommand> _updateGameHandler;
    private readonly ICommandHandler<RemoveGameCommand> _removeGameHandler;
    private readonly IQueryHandler<GetAllGamesQuery, IEnumerable<GameDTO>> _getAllGamesHandler;
    private readonly IQueryHandler<GetGameByIdQuery, GameDTO?> _getGameByIdHandler;

    public GameController(
        ICommandHandler<CreateGameCommand> createGameHandler,
        ICommandHandler<UpdateGameCommand> updateGameHandler,
        ICommandHandler<RemoveGameCommand> removeGameHandler,
        IQueryHandler<GetAllGamesQuery, IEnumerable<GameDTO>> getAllGamesHandler,
        IQueryHandler<GetGameByIdQuery, GameDTO?> getGameByIdHandler)
    {
        _createGameHandler = createGameHandler;
        _updateGameHandler = updateGameHandler;
        _removeGameHandler = removeGameHandler;
        _getAllGamesHandler = getAllGamesHandler;
        _getGameByIdHandler = getGameByIdHandler;
    }

    /// <summary>
    /// Get all games (Admin view with all details)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GameDTO>), 200)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var query = new GetAllGamesQuery();
            var games = await _getAllGamesHandler.HandleAsync(query);
            return Ok(games);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get game by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GameDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var query = new GetGameByIdQuery(id);
            var game = await _getGameByIdHandler.HandleAsync(query);
            
            if (game == null)
            {
                return NotFound(new { error = $"Game with ID {id} not found." });
            }

            return Ok(game);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Create a new game
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GameDTO), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateGameCommand command)
    {
        try
        {
            var result = await _createGameHandler.HandleAsync(command);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Message });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.EntityId },
                result.Data);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Update an existing game
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GameDTO), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGameCommand command)
    {
        try
        {
            // Ensure ID matches route
            if (command.GameId != id)
            {
                return BadRequest(new { error = "ID mismatch between route and body." });
            }

            var result = await _updateGameHandler.HandleAsync(command);

            if (!result.Success)
            {
                if (result.Message?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.Message });
                }
                return BadRequest(new { error = result.Message });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Delete a game
    /// </summary>
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
                if (result.Message?.Contains("not found") == true)
                {
                    return NotFound(new { error = result.Message });
                }
                return BadRequest(new { error = result.Message });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
