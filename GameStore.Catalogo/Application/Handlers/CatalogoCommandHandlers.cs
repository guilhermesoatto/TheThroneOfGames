using GameStore.Catalogo.Application.Commands;
using GameStore.Catalogo.Application.Validators;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Catalogo.Application.Mappers;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Catalogo.Domain.Interfaces;
using GameStore.Common.Events;
using GameStore.Catalogo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GameStore.CQRS.Abstractions;

namespace GameStore.Catalogo.Application.Handlers
{
    /// <summary>
    /// Handler para CreateGameCommand.
    /// </summary>
    public class CreateGameCommandHandler : ICommandHandler<CreateGameCommand>
    {
        private readonly IJogoRepository _jogoRepository;
        private readonly IEventBus _eventBus;

        public CreateGameCommandHandler(IJogoRepository jogoRepository, IEventBus eventBus)
        {
            _jogoRepository = jogoRepository;
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
                var existingGame = await _jogoRepository.GetByNomeAsync(command.Name);
                if (existingGame != null && existingGame.Any())
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Jogo já existe",
                        Errors = new List<string> { $"Jogo '{command.Name}' já está cadastrado" }
                    };
                }

                // Criar jogo usando construtor da entidade Jogo
                var jogo = new Jogo(
                    nome: command.Name,
                    descricao: command.Description ?? "Sem descrição",
                    preco: command.Price,
                    genero: command.Genre,
                    desenvolvedora: "Unknown", // TODO: adicionar campo ao command
                    dataLancamento: DateTime.UtcNow,
                    imagemUrl: "", // TODO: adicionar campo ao command
                    estoque: 100 // TODO: adicionar campo ao command
                );

                await _jogoRepository.AddAsync(jogo);
                // Publicar evento de domínio
                var gameCriadoEvent = new GameStore.Common.Events.GameCriadoEvent
                {
                    GameId = jogo.Id,
                    Nome = jogo.Nome,
                    Preco = jogo.Preco,
                    OccurredOn = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(gameCriadoEvent);
                return new CommandResult
                {
                    Success = true,
                    Message = "Jogo criado com sucesso",
                    EntityId = jogo.Id,
                    Data = GameMapper.ToDTO(jogo)
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
        private readonly IJogoRepository _jogoRepository;
        private readonly IEventBus _eventBus;

        public UpdateGameCommandHandler(IJogoRepository jogoRepository, IEventBus eventBus)
        {
            _jogoRepository = jogoRepository;
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
                var jogo = await _jogoRepository.GetByIdAsync(command.GameId);
                if (jogo == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Jogo não encontrado",
                        Errors = new List<string> { $"Jogo com ID {command.GameId} não encontrado" }
                    };
                }

                // Verificar se nome já existe para outro jogo
                var existingGames = await _jogoRepository.GetByNomeAsync(command.Name);
                var existingGame = existingGames.FirstOrDefault(j => j.Id != command.GameId);
                if (existingGame != null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Nome já está em uso",
                        Errors = new List<string> { $"Jogo '{command.Name}' já está cadastrado com outro ID" }
                    };
                }

                // Atualizar jogo usando método do domínio
                jogo.AtualizarInformacoes(
                    nome: command.Name,
                    descricao: command.Description ?? "Sem descrição",
                    preco: command.Price,
                    genero: command.Genre,
                    desenvolvedora: jogo.Desenvolvedora, // mantém atual
                    imagemUrl: jogo.ImagemUrl // mantém atual
                );

                await _jogoRepository.UpdateAsync(jogo);

                // Publicar evento de domínio
                var gameAtualizadoEvent = new GameStore.Common.Events.GameAtualizadoEvent
                {
                    GameId = jogo.Id,
                    Nome = jogo.Nome,
                    Preco = jogo.Preco,
                    OccurredOn = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(gameAtualizadoEvent);

                return new CommandResult
                {
                    Success = true,
                    Message = "Jogo atualizado com sucesso",
                    EntityId = jogo.Id,
                    Data = GameMapper.ToDTO(jogo)
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
        private readonly IJogoRepository _jogoRepository;
        private readonly IEventBus _eventBus;

        public RemoveGameCommandHandler(IJogoRepository jogoRepository, IEventBus eventBus)
        {
            _jogoRepository = jogoRepository;
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
                var jogo = await _jogoRepository.GetByIdAsync(command.GameId);
                if (jogo == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Jogo não encontrado",
                        Errors = new List<string> { $"Jogo com ID {command.GameId} não encontrado" }
                    };
                }

                // Remover jogo (soft delete usando método do domínio)
                jogo.Indisponibilizar();
                await _jogoRepository.UpdateAsync(jogo);

                // Publicar evento de domínio
                var gameRemovidoEvent = new GameStore.Common.Events.GameRemovidoEvent
                {
                    GameId = jogo.Id,
                    OccurredOn = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(gameRemovidoEvent);

                return new CommandResult
                {
                    Success = true,
                    Message = "Jogo removido com sucesso",
                    EntityId = jogo.Id,
                    Data = GameMapper.ToDTO(jogo)
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
