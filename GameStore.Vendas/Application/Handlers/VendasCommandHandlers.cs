using GameStore.Vendas.Application.Commands;
using GameStore.Vendas.Application.Validators;
using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Application.Mappers;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Application.Handlers
{
    /// <summary>
    /// Handler para CreatePurchaseCommand.
    /// </summary>
    public class CreatePurchaseCommandHandler : ICommandHandler<CreatePurchaseCommand>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IEventBus _eventBus;

        public CreatePurchaseCommandHandler(
            IPurchaseRepository purchaseRepository, 
            IGameRepository gameRepository,
            IEventBus eventBus)
        {
            _purchaseRepository = purchaseRepository;
            _gameRepository = gameRepository;
            _eventBus = eventBus;
        }

        public async Task<CommandResult> HandleAsync(CreatePurchaseCommand command)
        {
            // Validar command
            var validation = VendasValidators.Validate(command);
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
                // Verificar se jogo existe e está disponível
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

                if (!game.IsAvailable)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Jogo não disponível",
                        Errors = new List<string> { "Jogo não está disponível para compra" }
                    };
                }

                // Verificar se usuário já comprou este jogo
                var existingPurchase = await _purchaseRepository.GetUserPurchaseAsync(command.UserId, command.GameId);
                if (existingPurchase != null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Jogo já comprado",
                        Errors = new List<string> { "Usuário já possui este jogo" }
                    };
                }

                // Criar purchase
                var purchase = new PurchaseEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = command.UserId,
                    GameId = command.GameId,
                    TotalPrice = command.Price,
                    PurchaseDate = DateTime.UtcNow,
                    Status = "Pending"
                };

                await _purchaseRepository.AddAsync(purchase);

                // Publicar evento de domínio
                var gameCompradoEvent = new GameCompradoEvent(
                    GameId: command.GameId,
                    UserId: command.UserId,
                    Preco: command.Price,
                    NomeJogo: game.Name
                );
                await _eventBus.PublishAsync(gameCompradoEvent);

                return new CommandResult
                {
                    Success = true,
                    Message = "Compra criada com sucesso",
                    EntityId = purchase.Id,
                    Data = PurchaseMapper.ToPurchaseDTO(purchase)
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao criar compra",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para FinalizePurchaseCommand.
    /// </summary>
    public class FinalizePurchaseCommandHandler : ICommandHandler<FinalizePurchaseCommand>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IEventBus _eventBus;

        public FinalizePurchaseCommandHandler(IPurchaseRepository purchaseRepository, IEventBus eventBus)
        {
            _purchaseRepository = purchaseRepository;
            _eventBus = eventBus;
        }

        public async Task<CommandResult> HandleAsync(FinalizePurchaseCommand command)
        {
            // Validar command
            var validation = VendasValidators.Validate(command);
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
                // Buscar purchase
                var purchase = await _purchaseRepository.GetByIdAsync(command.PurchaseId);
                if (purchase == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Compra não encontrada",
                        Errors = new List<string> { $"Compra com ID {command.PurchaseId} não encontrada" }
                    };
                }

                if (purchase.Status != "Pending")
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Compra já processada",
                        Errors = new List<string> { $"Compra já está com status: {purchase.Status}" }
                    };
                }

                // Finalizar compra
                purchase.Status = "Completed";
                purchase.PaymentMethod = command.PaymentMethod;
                purchase.CompletedAt = DateTime.UtcNow;

                await _purchaseRepository.UpdateAsync(purchase);

                // Publicar evento de domínio
                var pedidoFinalizadoEvent = new PedidoFinalizadoEvent(
                    PedidoId: purchase.Id,
                    UserId: purchase.UserId,
                    TotalPrice: purchase.TotalPrice,
                    ItemCount: 1
                );
                await _eventBus.PublishAsync(pedidoFinalizadoEvent);

                return new CommandResult
                {
                    Success = true,
                    Message = "Compra finalizada com sucesso",
                    EntityId = purchase.Id,
                    Data = PurchaseMapper.ToPurchaseDTO(purchase)
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao finalizar compra",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para CancelPurchaseCommand.
    /// </summary>
    public class CancelPurchaseCommandHandler : ICommandHandler<CancelPurchaseCommand>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IEventBus _eventBus;

        public CancelPurchaseCommandHandler(IPurchaseRepository purchaseRepository, IEventBus eventBus)
        {
            _purchaseRepository = purchaseRepository;
            _eventBus = eventBus;
        }

        public async Task<CommandResult> HandleAsync(CancelPurchaseCommand command)
        {
            // Validar command
            var validation = VendasValidators.Validate(command);
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
                // Buscar purchase
                var purchase = await _purchaseRepository.GetByIdAsync(command.PurchaseId);
                if (purchase == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Compra não encontrada",
                        Errors = new List<string> { $"Compra com ID {command.PurchaseId} não encontrada" }
                    };
                }

                if (purchase.Status == "Cancelled")
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Compra já cancelada",
                        Errors = new List<string> { "Compra já está cancelada" }
                    };
                }

                if (purchase.Status == "Completed")
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Compra já finalizada",
                        Errors = new List<string> { "Não é possível cancelar uma compra já finalizada" }
                    };
                }

                // Cancelar compra
                purchase.Status = "Cancelled";
                purchase.CancellationReason = command.Reason;
                purchase.CancelledAt = DateTime.UtcNow;

                await _purchaseRepository.UpdateAsync(purchase);

                return new CommandResult
                {
                    Success = true,
                    Message = "Compra cancelada com sucesso",
                    EntityId = purchase.Id,
                    Data = PurchaseMapper.ToPurchaseDTO(purchase)
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao cancelar compra",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
