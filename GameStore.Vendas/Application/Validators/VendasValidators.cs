using GameStore.Vendas.Application.Commands;
using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Application.Validators
{
    /// <summary>
    /// Validator para Commands do contexto de Vendas.
    /// </summary>
    public static class VendasValidators
    {
        /// <summary>
        /// Valida um CreatePurchaseCommand.
        /// </summary>
        public static ValidationResult Validate(CreatePurchaseCommand command)
        {
            var result = new ValidationResult { IsValid = true };

            if (command.UserId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("ID do usuário é obrigatório.");
            }

            if (command.GameId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("ID do jogo é obrigatório.");
            }

            if (command.Price <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("Preço deve ser maior que zero.");
            }
            else if (command.Price > 10000)
            {
                result.IsValid = false;
                result.Errors.Add("Preço não pode exceder R$ 10.000,00.");
            }

            return result;
        }

        /// <summary>
        /// Valida um FinalizePurchaseCommand.
        /// </summary>
        public static ValidationResult Validate(FinalizePurchaseCommand command)
        {
            var result = new ValidationResult { IsValid = true };

            if (command.PurchaseId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("ID da compra é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(command.PaymentMethod))
            {
                result.IsValid = false;
                result.Errors.Add("Método de pagamento é obrigatório.");
            }
            else if (!new[] { "CreditCard", "DebitCard", "PayPal", "Pix" }.Contains(command.PaymentMethod))
            {
                result.IsValid = false;
                result.Errors.Add("Método de pagamento inválido. Opções: CreditCard, DebitCard, PayPal, Pix.");
            }

            return result;
        }

        /// <summary>
        /// Valida um CancelPurchaseCommand.
        /// </summary>
        public static ValidationResult Validate(CancelPurchaseCommand command)
        {
            var result = new ValidationResult { IsValid = true };

            if (command.PurchaseId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("ID da compra é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                result.IsValid = false;
                result.Errors.Add("Motivo do cancelamento é obrigatório.");
            }
            else if (command.Reason.Length < 5)
            {
                result.IsValid = false;
                result.Errors.Add("Motivo do cancelamento deve ter pelo menos 5 caracteres.");
            }

            return result;
        }
    }
}
