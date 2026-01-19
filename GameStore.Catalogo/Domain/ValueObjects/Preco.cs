using System;

namespace GameStore.Catalogo.Domain.ValueObjects
{
    public class Preco
    {
        public decimal Valor { get; private set; }
        public string Moeda { get; private set; }

        public Preco(decimal valor, string moeda = "BRL")
        {
            if (valor < 0)
                throw new ArgumentException("Preço não pode ser negativo", nameof(valor));

            if (string.IsNullOrWhiteSpace(moeda))
                throw new ArgumentException("Moeda é obrigatória", nameof(moeda));

            Valor = valor;
            Moeda = moeda;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Preco other)
                return false;

            return Valor == other.Valor && Moeda == other.Moeda;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Valor, Moeda);
        }

        public override string ToString()
        {
            return $"{Valor:N2} {Moeda}";
        }
    }
}