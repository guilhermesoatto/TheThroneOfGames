using GameStore.Vendas.Application.Commands;
using GameStore.Vendas.Domain.Repositories;
using GameStore.Vendas.Domain.Entities;
using GameStore.Vendas.Domain.ValueObjects;
using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Application.Handlers
{
    /// <summary>
    /// Handler para CriarPedidoCommand.
    /// </summary>
    public class CriarPedidoCommandHandler : ICommandHandler<CriarPedidoCommand>
    {
        private readonly IPedidoRepository _pedidoRepository;

        public CriarPedidoCommandHandler(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
        }

        public async Task<CommandResult> HandleAsync(CriarPedidoCommand command)
        {
            try
            {
                var pedido = new Pedido(command.UsuarioId);
                await _pedidoRepository.AddAsync(pedido);

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

        public AdicionarItemPedidoCommandHandler(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
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

        public RemoverItemPedidoCommandHandler(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
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

        public FinalizarPedidoCommandHandler(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
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

                pedido.Finalizar(command.MetodoPagamento);
                await _pedidoRepository.UpdateAsync(pedido);

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

        public CancelarPedidoCommandHandler(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
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