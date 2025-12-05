using GameStore.Catalogo.Application.Commands;
using GameStore.CQRS.Abstractions;

namespace GameStore.Catalogo.Application.Validators
{
    /// <summary>
    /// Validator para Commands do contexto de Catálogo.
    /// </summary>
    public static class CatalogoValidators
    {
        /// <summary>
        /// Valida um CreateGameCommand.
        /// </summary>
        public static ValidationResult Validate(CreateGameCommand command)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Nome do jogo é obrigatório.");
            }
            else if (command.Name.Length < 2)
            {
                result.IsValid = false;
                result.Errors.Add("Nome do jogo deve ter pelo menos 2 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(command.Genre))
            {
                result.IsValid = false;
                result.Errors.Add("Gênero do jogo é obrigatório.");
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
        /// Valida um UpdateGameCommand.
        /// </summary>
        public static ValidationResult Validate(UpdateGameCommand command)
        {
            var result = new ValidationResult { IsValid = true };

            if (command.GameId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("ID do jogo é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Nome do jogo é obrigatório.");
            }
            else if (command.Name.Length < 2)
            {
                result.IsValid = false;
                result.Errors.Add("Nome do jogo deve ter pelo menos 2 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(command.Genre))
            {
                result.IsValid = false;
                result.Errors.Add("Gênero do jogo é obrigatório.");
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
        /// Valida um RemoveGameCommand.
        /// </summary>
        public static ValidationResult Validate(RemoveGameCommand command)
        {
            var result = new ValidationResult { IsValid = true };

            if (command.GameId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("ID do jogo é obrigatório.");
            }

            return result;
        }
    }
}
