using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GameStore.Vendas.Application.Commands;
using GameStore.CQRS.Abstractions;
using GameStore.Vendas.Domain.Repositories;

namespace GameStore.Vendas.API.Controllers
{
    [ApiController]
    [Route("api/pedidos")]
    public class PedidoController : ControllerBase
    {
        private readonly ICommandHandler<CriarPedidoCommand> _criarPedidoHandler;
        private readonly ICommandHandler<AdicionarItemPedidoCommand> _adicionarItemHandler;
        private readonly ICommandHandler<FinalizarPedidoCommand> _finalizarPedidoHandler;
        private readonly ICommandHandler<CancelarPedidoCommand> _cancelarPedidoHandler;
        private readonly IPedidoRepository _pedidoRepository;

        public PedidoController(
            ICommandHandler<CriarPedidoCommand> criarPedidoHandler,
            ICommandHandler<AdicionarItemPedidoCommand> adicionarItemHandler,
            ICommandHandler<FinalizarPedidoCommand> finalizarPedidoHandler,
            ICommandHandler<CancelarPedidoCommand> cancelarPedidoHandler,
            IPedidoRepository pedidoRepository)
        {
            _criarPedidoHandler = criarPedidoHandler;
            _adicionarItemHandler = adicionarItemHandler;
            _finalizarPedidoHandler = finalizarPedidoHandler;
            _cancelarPedidoHandler = cancelarPedidoHandler;
            _pedidoRepository = pedidoRepository;
        }

        /// <summary>
        /// Obter todos os pedidos do usuário autenticado
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserOrders()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("nameid");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized();

                var pedidos = await _pedidoRepository.GetByUsuarioIdAsync(userId);
                return Ok(pedidos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter pedido por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            try
            {
                var pedido = await _pedidoRepository.GetByIdAsync(id);
                if (pedido == null)
                    return NotFound();

                return Ok(pedido);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Criar novo pedido
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("nameid");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized();

                var command = new CriarPedidoCommand(userId);
                var result = await _criarPedidoHandler.HandleAsync(command);

                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(nameof(GetOrderById), new { id = result.EntityId }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Adicionar item ao pedido
        /// </summary>
        [HttpPost("{pedidoId}/itens")]
        [Authorize]
        public async Task<IActionResult> AddItemToOrder(Guid pedidoId, [FromBody] AddOrderItemRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var command = new AdicionarItemPedidoCommand(
                    pedidoId,
                    request.JogoId,
                    request.NomeJogo,
                    request.Preco
                );

                var result = await _adicionarItemHandler.HandleAsync(command);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Finalizar pedido
        /// </summary>
        [HttpPost("{pedidoId}/finalizar")]
        [Authorize]
        public async Task<IActionResult> FinalizeOrder(Guid pedidoId, [FromBody] FinalizeOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var command = new FinalizarPedidoCommand(pedidoId, request.MetodoPagamento);
                var result = await _finalizarPedidoHandler.HandleAsync(command);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cancelar pedido
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(Guid id, [FromQuery] string motivo = "Cancelado pelo usuário")
        {
            try
            {
                var command = new CancelarPedidoCommand(id, motivo);
                var result = await _cancelarPedidoHandler.HandleAsync(command);

                if (!result.Success)
                    return BadRequest(result);

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request para adicionar item ao pedido
    /// </summary>
    public class AddOrderItemRequest
    {
        public Guid JogoId { get; set; }
        public string NomeJogo { get; set; } = string.Empty;
        public decimal Preco { get; set; }
    }

    /// <summary>
    /// Request para finalizar pedido
    /// </summary>
    public class FinalizeOrderRequest
    {
        public string MetodoPagamento { get; set; } = string.Empty;
    }
}
