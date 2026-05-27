namespace GameStore.Vendas.Domain.Shared;

// Domain errors for the Vendas bounded context
public sealed record PedidoNaoPendenteError(string Status)
    : DomainError("PEDIDO_NAO_PENDENTE", $"Operação inválida: pedido está no status '{Status}'.");

public sealed record PedidoSemItensError()
    : DomainError("PEDIDO_SEM_ITENS", "Não é possível finalizar um pedido sem itens.");

public sealed record JogoJaAdicionadoError(Guid JogoId)
    : DomainError("JOGO_JA_ADICIONADO", $"O jogo '{JogoId}' já foi adicionado ao pedido.");

public sealed record ItemNaoEncontradoError(Guid JogoId)
    : DomainError("ITEM_NAO_ENCONTRADO", $"Item com jogo '{JogoId}' não encontrado no pedido.");

public sealed record PedidoNaoEncontradoError(Guid Id)
    : DomainError("PEDIDO_NAO_ENCONTRADO", $"Pedido com id '{Id}' não encontrado.");
