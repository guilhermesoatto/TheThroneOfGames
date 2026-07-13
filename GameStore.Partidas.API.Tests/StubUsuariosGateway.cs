using GameStore.Partidas.Application.Ports;

namespace GameStore.Partidas.API.Tests;

/// <summary>
/// Dublê de IUsuariosGateway para os testes de integração deste serviço — testar a chamada
/// HTTP real a GameStore.Usuarios exigiria subir os dois serviços juntos, o que é
/// responsabilidade do script de validação ponta a ponta (partidas-T08), não deste projeto.
/// Aqui testamos SÓ o comportamento do GameStore.Partidas (Mongo real via Testcontainers).
/// </summary>
public class StubUsuariosGateway : IUsuariosGateway
{
    private readonly bool _possuiTodosOsJogos;

    public StubUsuariosGateway(bool possuiTodosOsJogos = true)
    {
        _possuiTodosOsJogos = possuiTodosOsJogos;
    }

    public Task<bool> PossuiJogoAsync(Guid jogoId, string bearerToken, CancellationToken ct = default)
        => Task.FromResult(_possuiTodosOsJogos);
}
