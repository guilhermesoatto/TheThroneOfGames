using GameStore.Catalogo.Domain.Shared;

namespace GameStore.Catalogo.Domain.ValueObjects;

/// <summary>
/// Value Object imutável representando um preço com moeda.
/// Use Preco.Create() — nunca o construtor diretamente.
/// </summary>
public sealed record Preco
{
    public decimal Valor { get; init; }
    public string Moeda { get; init; }

    private Preco(decimal valor, string moeda)
    {
        Valor = valor;
        Moeda = moeda;
    }

    public static Result<Preco> Create(decimal valor, string moeda = "BRL")
    {
        if (valor < 0)
            return new PrecoNegativoError(valor);

        if (string.IsNullOrWhiteSpace(moeda))
            return new MoedaVaziaError();

        return new Preco(valor, moeda);
    }

    public override string ToString() => $"{Valor:N2} {Moeda}";
}

public sealed record PrecoNegativoError(decimal Valor)
    : DomainError("PRECO_NEGATIVO", $"O preço '{Valor}' não pode ser negativo.");

public sealed record MoedaVaziaError()
    : DomainError("MOEDA_VAZIA", "A moeda é obrigatória.");