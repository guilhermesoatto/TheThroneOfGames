using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GameStore.Partidas.Application.UseCases;

namespace GameStore.Partidas.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PartidaController : ControllerBase
{
    private readonly BuscarPartidaUseCase _buscarPartidaUseCase;
    private readonly ConfirmarPartidaUseCase _confirmarPartidaUseCase;
    private readonly DesistirPartidaUseCase _desistirPartidaUseCase;
    private readonly ConsultarStatusUseCase _consultarStatusUseCase;

    public PartidaController(
        BuscarPartidaUseCase buscarPartidaUseCase,
        ConfirmarPartidaUseCase confirmarPartidaUseCase,
        DesistirPartidaUseCase desistirPartidaUseCase,
        ConsultarStatusUseCase consultarStatusUseCase)
    {
        _buscarPartidaUseCase = buscarPartidaUseCase;
        _confirmarPartidaUseCase = confirmarPartidaUseCase;
        _desistirPartidaUseCase = desistirPartidaUseCase;
        _consultarStatusUseCase = consultarStatusUseCase;
    }

    private Guid? JogadorIdAtual()
    {
        var claim = User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private string? BearerTokenAtual()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..] : null;
    }

    /// <summary>Entra na fila de busca por partida para um jogo. Forma a partida na hora (1v1) se já houver alguém esperando.</summary>
    [HttpPost("buscar")]
    public async Task<IActionResult> Buscar([FromBody] BuscarPartidaRequest request)
    {
        var jogadorId = JogadorIdAtual();
        var token = BearerTokenAtual();
        if (jogadorId is null || token is null)
            return Unauthorized();

        var result = await _buscarPartidaUseCase.ExecuteAsync(jogadorId.Value, request.JogoId, token);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(new
        {
            solicitacaoId = result.Value.Solicitacao.Id,
            status = result.Value.Solicitacao.Status.ToString(),
            partidaId = result.Value.Partida?.Id,
        });
    }

    /// <summary>Confirma participação numa partida já formada. Quando todos confirmam, a partida começa.</summary>
    [HttpPost("{partidaId:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid partidaId)
    {
        var jogadorId = JogadorIdAtual();
        if (jogadorId is null)
            return Unauthorized();

        var result = await _confirmarPartidaUseCase.ExecuteAsync(partidaId, jogadorId.Value);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(new { partidaId = result.Value.Id, status = result.Value.Status.ToString() });
    }

    /// <summary>Desiste antes da confirmação mútua — desfaz a partida e devolve o outro jogador à fila.</summary>
    [HttpPost("{partidaId:guid}/desistir")]
    public async Task<IActionResult> Desistir(Guid partidaId)
    {
        var jogadorId = JogadorIdAtual();
        if (jogadorId is null)
            return Unauthorized();

        var result = await _desistirPartidaUseCase.ExecuteAsync(partidaId, jogadorId.Value);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(new { partidaId = result.Value.Id, status = result.Value.Status.ToString() });
    }

    /// <summary>Consulta de status por polling — usada pelo cliente e pelo script de validação (partidas-T08).</summary>
    [HttpGet("solicitacoes/{solicitacaoId:guid}")]
    public async Task<IActionResult> ConsultarStatus(Guid solicitacaoId)
    {
        var result = await _consultarStatusUseCase.ExecuteAsync(solicitacaoId);
        if (result.IsFailure)
            return NotFound(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(new
        {
            solicitacaoId = result.Value.Solicitacao.Id,
            solicitacaoStatus = result.Value.Solicitacao.Status.ToString(),
            partidaId = result.Value.Partida?.Id,
            partidaStatus = result.Value.Partida?.Status.ToString(),
        });
    }
}

public class BuscarPartidaRequest
{
    public required Guid JogoId { get; set; }
}
