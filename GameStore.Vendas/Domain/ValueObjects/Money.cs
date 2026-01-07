namespace GameStore.Vendas.Domain.ValueObjects
{
    public class Money : IEquatable<Money>
    {
        public decimal Amount { get; private set; }
        public string Currency { get; private set; } = "BRL";

        private Money() { } // EF Core

        public Money(decimal amount, string currency = "BRL")
        {
            if (amount < 0)
                throw new ArgumentException("O valor não pode ser negativo", nameof(amount));

            Amount = amount;
            Currency = currency ?? "BRL";
        }

        public static Money Zero => new Money(0);

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException("Não é possível somar valores de moedas diferentes");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException("Não é possível subtrair valores de moedas diferentes");

            return new Money(Amount - other.Amount, Currency);
        }

        public bool Equals(Money? other)
        {
            if (other is null) return false;
            return Amount == other.Amount && Currency == other.Currency;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Money);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Currency);
        }

        public static bool operator ==(Money left, Money right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Money left, Money right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Currency} {Amount:F2}";
        }
    }
}