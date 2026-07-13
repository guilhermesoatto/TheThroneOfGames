using GameStore.Vendas.Application.Commands;
using GameStore.Vendas.Domain.Repositories;
using GameStore.Vendas.Domain.Entities;
using GameStore.Vendas.Domain.ValueObjects;
using GameStore.Vendas.Domain.EventSourcing;
using GameStore.CQRS.Abstractions;
using GameStore.Common.Events;
using Microsoft.Extensions.Logging;

namespace GameStore.Vendas.Application.Handlers
{
    /// <summary>
    /// Handler para CriarPedidoCommand.
    /// </summary>
    public class CriarPedidoCommandHandler : ICommandHandler<CriarPedidoCommand>
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IEventStore _eventStore;

        public CriarPedidoCommandHandler(IPedidoRepository pedidoRepository, IEventStore eventStore)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task<CommandResult> HandleAsync(CriarPedidoCommand command)
        {
            try
            {
                var pedido = new Pedido(command.UsuarioId);
                await _pedidoRepository.AddAsync(pedido);

                await _eventStore.AppendAsync(new PedidoDomainEvent
                {
                    EventType = "PedidoCriado",
                    EventVersion = 1,
                    AggregateId = pedido.Id,
                    AggregateName = nameof(Pedido),
                    CorrelationId = pedido.Id.ToString(),
                    Payload = new Dictionary<string, object> { ["UsuarioId"] = pedido.UsuarioId }
                });

                return new CommandResult
                {
                    Success = true,
                    Message = "Pedido criado com sucesso",
                    EntityId = pedido.Id,
                    Data = pedido
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao criar pedido",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para AdicionarItemPedidoCommand.
    /// </summary>
    public class AdicionarItemPedidoCommandHandler : ICommandHandler<AdicionarItemPedidoCommand>
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IEventStore _eventStore;

        public AdicionarItemPedidoCommandHandler(IPedidoRepository pedidoRepository, IEventStore eventStore)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task<CommandResult> HandleAsync(AdicionarItemPedidoCommand command)
        {
            try
            {
                var pedido = await _pedidoRepository.GetByIdAsync(command.PedidoId);
                if (pedido == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Pedido não encontrado",
                        Errors = new List<string> { $"Pedido {command.PedidoId} não existe" }
                    };
                }

                pedido.AdicionarItem(command.JogoId, command.NomeJogo, new Money(command.Preco));
                await _pedidoRepository.UpdateAsync(pedido);

                await _eventStore.AppendAsync(new PedidoDomainEvent
                {
                    EventType = "ItemAdicionado",
                    EventVersion = 1,
                    AggregateId = pedido.Id,
                    AggregateName = nameof(Pedido),
                    CorrelationId = pedido.Id.ToString(),
                    Payload = new Dictionary<string, object>
                    {
                        ["JogoId"] = command.JogoId,
                        ["NomeJogo"] = command.NomeJogo,
                        ["Preco"] = command.Preco
                    }
                });

                return new CommandResult
                {
                    Success = true,
                    Message = "Item adicionado ao pedido com sucesso",
                    EntityId = command.PedidoId
                };
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao adicionar item ao pedido",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para RemoverItemPedidoCommand.
    /// </summary>
    public class RemoverItemPedidoCommandHandler : ICommandHandler<RemoverItemPedidoCommand>
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IEventStore _eventStore;

        public RemoverItemPedidoCommandHandler(IPedidoRepository pedidoRepository, IEventStore eventStore)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task<CommandResult> HandleAsync(RemoverItemPedidoCommand command)
        {
            try
            {
                var pedido = await _pedidoRepository.GetByIdAsync(command.PedidoId);
                if (pedido == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Pedido não encontrado",
                        Errors = new List<string> { $"Pedido {command.PedidoId} não existe" }
                    };
                }

                pedido.RemoverItem(command.JogoId);
                await _pedidoRepository.UpdateAsync(pedido);

                await _eventStore.AppendAsync(new PedidoDomainEvent
                {
                    EventType = "ItemRemovido",
                    EventVersion = 1,
                    AggregateId = pedido.Id,
                    AggregateName = nameof(Pedido),
                    CorrelationId = pedido.Id.ToString(),
                    Payload = new Dictionary<string, object> { ["JogoId"] = command.JogoId }
                });

                return new CommandResult
                {
                    Success = true,
                    Message = "Item removido do pedido com sucesso",
                    EntityId = command.PedidoId
                };
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao remover item do pedido",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para FinalizarPedidoCommand.
    /// </summary>
    public class FinalizarPedidoCommandHandler : ICommandHandler<FinalizarPedidoCommand>
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IEventStore _eventStore;
        private readonly IEventBus _eventBus;
        private readonly ILogger<FinalizarPedidoCommandHandler> _logger;

        public FinalizarPedidoCommandHandler(
            IPedidoRepository pedidoRepository,
            IEventStore eventStore,
            IEventBus eventBus,
            ILogger<FinalizarPedidoCommandHandler> logger)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CommandResult> HandleAsync(FinalizarPedidoCommand command)
        {
            try
            {
                var pedido = await _pedidoRepository.GetByIdAsync(command.PedidoId);
                if (pedido == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Pedido não encontrado",
                        Errors = new List<string> { $"Pedido {command.PedidoId} não existe" }
                    };
                }

                var pedidoFinalizadoEvent = pedido.Finalizar(command.MetodoPagamento);
                await _pedidoRepository.UpdateAsync(pedido);

                await _eventStore.AppendAsync(new PedidoDomainEvent
                {
                    EventType = "PedidoFinalizado",
                    EventVersion = 1,
                    AggregateId = pedido.Id,
                    AggregateName = nameof(Pedido),
                    CorrelationId = pedido.Id.ToString(),
                    Payload = new Dictionary<string, object>
                    {
                        ["MetodoPagamento"] = command.MetodoPagamento,
                        ["ValorTotal"] = pedido.ValorTotal.Amount
                    }
                });

                // Publica no barramento de eventos (RabbitMQ) para que Usuarios e as Functions
                // serverless (notificação/pagamento) reajam ao pedido finalizado. Best-effort: uma
                // falha no broker não deve reverter uma compra já persistida e registrada no Event Store.
                try
                {
                    await _eventBus.PublishAsync(pedidoFinalizadoEvent);
                }
                catch (Exception publishEx)
                {
                    _logger.LogWarning(publishEx,
                        "Falha ao publicar PedidoFinalizadoEvent para o pedido {PedidoId} no barramento de eventos",
                        pedido.Id);
                }

                // Um GameCompradoEvent por item — Usuarios consome (GameCompradoEventConsumer)
                // para popular o Inventário do jogador (ver ItemInventario), fonte da verdade de
                // "o jogador possui este jogo?" usada pelo bounded context de Partidas antes de
                // liberar a busca por partida. Best-effort, mesmo racional do publish acima.
                foreach (var item in pedido.Itens)
                {
                    try
                    {
                        await _eventBus.PublishAsync(new GameCompradoEvent(
                            item.JogoId, pedido.UsuarioId, item.Preco.Amount, item.NomeJogo));
                    }
                    catch (Exception publishEx)
                    {
                        _logger.LogWarning(publishEx,
                            "Falha ao publicar GameCompradoEvent para o jogo {JogoId} do pedido {PedidoId} no barramento de eventos",
                            item.JogoId, pedido.Id);
                    }
                }

                return new CommandResult
                {
                    Success = true,
                    Message = "Pedido finalizado com sucesso",
                    EntityId = command.PedidoId
                };
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao finalizar pedido",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para CancelarPedidoCommand.
    /// </summary>
    public class CancelarPedidoCommandHandler : ICommandHandler<CancelarPedidoCommand>
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IEventStore _eventStore;

        public CancelarPedidoCommandHandler(IPedidoRepository pedidoRepository, IEventStore eventStore)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task<CommandResult> HandleAsync(CancelarPedidoCommand command)
        {
            try
            {
                var pedido = await _pedidoRepository.GetByIdAsync(command.PedidoId);
                if (pedido == null)
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = "Pedido não encontrado",
                        Errors = new List<string> { $"Pedido {command.PedidoId} não existe" }
                    };
                }

                pedido.Cancelar(command.Motivo);
                await _pedidoRepository.UpdateAsync(pedido);

                await _eventStore.AppendAsync(new PedidoDomainEvent
                {
                    EventType = "PedidoCancelado",
                    EventVersion = 1,
                    AggregateId = pedido.Id,
                    AggregateName = nameof(Pedido),
                    CorrelationId = pedido.Id.ToString(),
                    Payload = new Dictionary<string, object> { ["Motivo"] = command.Motivo }
                });

                return new CommandResult
                {
                    Success = true,
                    Message = "Pedido cancelado com sucesso",
                    EntityId = command.PedidoId
                };
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao cancelar pedido",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
