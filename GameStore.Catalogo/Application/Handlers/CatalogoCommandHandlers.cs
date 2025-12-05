using GameStore.Catalogo.Application.Commands;
using GameStore.Catalogo.Application.Validators;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using TheThroneOfGames.Infrastructure.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GameStore.CQRS.Abstractions;

namespace GameStore.Catalogo.Application.Handlers
{
    /// <summary>
    /// Handler para CreateGameCommand.
    /// </summary>
    public class CreateGameCommandHandler : ICommandHandler<CreateGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventBus _eventBus;

        public CreateGameCommandHandler(IGameRepository gameRepository, IEventBus eventBus)
        {
            _gameRepository = gameRepository;
            _eventBus = eventBus;
        }

        public async Task<CommandResult> HandleAsync(CreateGameCommand command)
        {
            // Validar command
            var validation = CatalogoValidators.Validate(command);
            if (!validation.IsValid)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Validação falhou",
                    Errors = validation.Errors
                };
            }

            try
            {
                // Verificar se jogo já existe
                var existingGame = await _gameRepository.GetByNameAsync(command.Name);
                if (existingGame != null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Jogo já existe",
                        Errors = new List<string> { $"Jogo '{command.Name}' já está cadastrado" }
                    };
                }

                // Criar jogo
                var game = new GameEntity
                {
                    Id = Guid.NewGuid(),
                    Name = command.Name,
                    Genre = command.Genre,
                    Price = command.Price,
                    Description = command.Description,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _gameRepository.AddAsync(game);

                return new CommandResult
                {
                    Success = true,
                    Message = "Jogo criado com sucesso",
                    EntityId = game.Id,
                    Data = GameMapper.ToDTO(game)
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao criar jogo",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para UpdateGameCommand.
    /// </summary>
    public class UpdateGameCommandHandler : ICommandHandler<UpdateGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventBus _eventBus;

        public UpdateGameCommandHandler(IGameRepository gameRepository, IEventBus eventBus)
        {
            _gameRepository = gameRepository;
            _eventBus = eventBus;
        }

        public async Task<CommandResult> HandleAsync(UpdateGameCommand command)
        {
            // Validar command
            var validation = CatalogoValidators.Validate(command);
            if (!validation.IsValid)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Validação falhou",
                    Errors = validation.Errors
                };
            }

            try
            {
                // Buscar jogo
                var game = await _gameRepository.GetByIdAsync(command.GameId);
                if (game == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Jogo não encontrado",
                        Errors = new List<string> { $"Jogo com ID {command.GameId} não encontrado" }
                    };
                }

                // Verificar se nome já existe para outro jogo
                var existingGame = await _gameRepository.GetByNameAsync(command.Name);
                if (existingGame != null && existingGame.Id != command.GameId)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Nome já está em uso",
                        Errors = new List<string> { $"Jogo '{command.Name}' já está cadastrado com outro ID" }
                    };
                }

                // Atualizar jogo
                game.Name = command.Name;
                game.Genre = command.Genre;
                game.Price = command.Price;
                game.Description = command.Description;
                game.UpdatedAt = DateTime.UtcNow;

                await _gameRepository.UpdateAsync(game);

                return new CommandResult
                {
                    Success = true,
                    Message = "Jogo atualizado com sucesso",
                    EntityId = game.Id,
                    Data = GameMapper.ToDTO(game)
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao atualizar jogo",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para RemoveGameCommand.
    /// </summary>
    public class RemoveGameCommandHandler : ICommandHandler<RemoveGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventBus _eventBus;

        public RemoveGameCommandHandler(IGameRepository gameRepository, IEventBus eventBus)
        {
            _gameRepository = gameRepository;
            _eventBus = eventBus;
        }

        public async Task<CommandResult> HandleAsync(RemoveGameCommand command)
        {
            // Validar command
            var validation = CatalogoValidators.Validate(command);
            if (!validation.IsValid)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Validação falhou",
                    Errors = validation.Errors
                };
            }

            try
            {
                // Buscar jogo
                var game = await _gameRepository.GetByIdAsync(command.GameId);
                if (game == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Jogo não encontrado",
                        Errors = new List<string> { $"Jogo com ID {command.GameId} não encontrado" }
                    };
                }

                // Verificar se há compras ativas
                var hasActivePurchases = await _gameRepository.HasActivePurchasesAsync(command.GameId);
                if (hasActivePurchases)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Jogo não pode ser removido",
                        Errors = new List<string> { "Jogo possui compras ativas e não pode ser removido" }
                    };
                }

                // Remover jogo (soft delete)
                game.IsAvailable = false;
                game.UpdatedAt = DateTime.UtcNow;
                await _gameRepository.UpdateAsync(game);

                return new CommandResult
                {
                    Success = true,
                    Message = "Jogo removido com sucesso",
                    EntityId = game.Id,
                    Data = GameMapper.ToDTO(game)
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao remover jogo",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
