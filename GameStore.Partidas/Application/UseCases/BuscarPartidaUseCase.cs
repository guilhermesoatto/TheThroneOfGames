using GameStore.Partidas.Application.Ports;
using GameStore.Partidas.Application.Services;
using GameStore.Partidas.Domain.Entities;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Domain.Shared;

namespace GameStore.Partidas.Application.UseCases;

public sealed record BuscarPartidaResultado(SolicitacaoBusca Solicitacao, Partida? Partida);

/// <summary>
/// Um jogador procura partida para um jogo. Só entra na fila se já possuir o jogo (verificado
/// via GameStore.Usuarios — ver IUsuariosGateway). Se já houver alguém esperando o mesmo jogo,
/// a partida é formada na hora (1v1 imediato).
/// </summary>
public class BuscarPartidaUseCase
{
    private readonly IUsuariosGateway _usuariosGateway;
    private readonly ISolicitacaoBuscaRepository _solicitacaoRepository;
    private readonly MotorDePareamento _motorDePareamento;

    public BuscarPartidaUseCase(
        IUsuariosGateway usuariosGateway,
        ISolicitacaoBuscaRepository solicitacaoRepository,
        MotorDePareamento motorDePareamento)
    {
        _usuariosGateway = usuariosGateway;
        _solicitacaoRepository = solicitacaoRepository;
        _motorDePareamento = motorDePareamento;
    }

    public async Task<Result<BuscarPartidaResultado>> ExecuteAsync(
        Guid jogadorId, Guid jogoId, string bearerToken, CancellationToken ct = default)
    {
        var possuiJogo = await _usuariosGateway.PossuiJogoAsync(jogoId, bearerToken, ct);
        if (!possuiJogo)
            return new JogadorNaoPossuiJogoError(jogadorId, jogoId);

        var jaExiste = await _solicitacaoRepository.GetAguardandoDoJogadorAsync(jogadorId, jogoId, ct);
        if (jaExiste is not null)
            return new SolicitacaoJaExisteError(jogadorId, jogoId);

        var (solicitacao, partida) = await _motorDePareamento.TentarParearAsync(jogadorId, jogoId, ct);
        return new BuscarPartidaResultado(solicitacao, partida);
    }
}
