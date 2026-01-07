using GameStore.Vendas.Application.Commands;
using GameStore.Vendas.Application.Interfaces;
using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Application.Handlers
{
    /// <summary>
    /// Handler para CriarPedidoCommand.
    /// </summary>
    public class CriarPedidoCommandHandler : ICommandHandler<CriarPedidoCommand>
    {
        private readonly IPedidoService _pedidoService;

        public CriarPedidoCommandHandler(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService ?? throw new ArgumentNullException(nameof(pedidoService));
        }

        public async Task<CommandResult> HandleAsync(CriarPedidoCommand command)
        {
            try
            {
                var pedidoDto = await _pedidoService.CriarPedidoAsync(command.UsuarioId);

                return new CommandResult
                {
                    Success = true,
                    Message = "Pedido criado com sucesso",
                    EntityId = pedidoDto.Id,
                    Data = pedidoDto
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
        private readonly IPedidoService _pedidoService;

        public AdicionarItemPedidoCommandHandler(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService ?? throw new ArgumentNullException(nameof(pedidoService));
        }

        public async Task<CommandResult> HandleAsync(AdicionarItemPedidoCommand command)
        {
            var success = await _pedidoService.AdicionarItemAoPedidoAsync(
                command.PedidoId,
                command.JogoId,
                command.NomeJogo,
                command.Preco);

            if (success)
            {
                return new CommandResult
                {
                    Success = true,
                    Message = "Item adicionado ao pedido com sucesso",
                    EntityId = command.PedidoId
                };
            }
            else
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao adicionar item ao pedido",
                    Errors = new List<string> { "Pedido não encontrado ou operação inválida" }
                };
            }
        }
    }

    /// <summary>
    /// Handler para RemoverItemPedidoCommand.
    /// </summary>
    public class RemoverItemPedidoCommandHandler : ICommandHandler<RemoverItemPedidoCommand>
    {
        private readonly IPedidoService _pedidoService;

        public RemoverItemPedidoCommandHandler(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService ?? throw new ArgumentNullException(nameof(pedidoService));
        }

        public async Task<CommandResult> HandleAsync(RemoverItemPedidoCommand command)
        {
            var success = await _pedidoService.RemoverItemDoPedidoAsync(command.PedidoId, command.JogoId);

            if (success)
            {
                return new CommandResult
                {
                    Success = true,
                    Message = "Item removido do pedido com sucesso",
                    EntityId = command.PedidoId
                };
            }
            else
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao remover item do pedido",
                    Errors = new List<string> { "Pedido não encontrado ou item não existe no pedido" }
                };
            }
        }
    }

    /// <summary>
    /// Handler para FinalizarPedidoCommand.
    /// </summary>
    public class FinalizarPedidoCommandHandler : ICommandHandler<FinalizarPedidoCommand>
    {
        private readonly IPedidoService _pedidoService;

        public FinalizarPedidoCommandHandler(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService ?? throw new ArgumentNullException(nameof(pedidoService));
        }

        public async Task<CommandResult> HandleAsync(FinalizarPedidoCommand command)
        {
            var success = await _pedidoService.FinalizarPedidoAsync(command.PedidoId, command.MetodoPagamento);

            if (success)
            {
                return new CommandResult
                {
                    Success = true,
                    Message = "Pedido finalizado com sucesso",
                    EntityId = command.PedidoId
                };
            }
            else
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao finalizar pedido",
                    Errors = new List<string> { "Pedido não encontrado ou não pode ser finalizado" }
                };
            }
        }
    }

    /// <summary>
    /// Handler para CancelarPedidoCommand.
    /// </summary>
    public class CancelarPedidoCommandHandler : ICommandHandler<CancelarPedidoCommand>
    {
        private readonly IPedidoService _pedidoService;

        public CancelarPedidoCommandHandler(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService ?? throw new ArgumentNullException(nameof(pedidoService));
        }

        public async Task<CommandResult> HandleAsync(CancelarPedidoCommand command)
        {
            var success = await _pedidoService.CancelarPedidoAsync(command.PedidoId, command.Motivo);

            if (success)
            {
                return new CommandResult
                {
                    Success = true,
                    Message = "Pedido cancelado com sucesso",
                    EntityId = command.PedidoId
                };
            }
            else
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Erro ao cancelar pedido",
                    Errors = new List<string> { "Pedido não encontrado ou não pode ser cancelado" }
                };
            }
        }
    }
}