using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Domain.Games.Interfaces
{
    public class GamesDomain : GameEntity
    {
        public GamesDomain(string name, string genre, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome do jogo é obrigatório.");
            if (string.IsNullOrWhiteSpace(genre)) throw new ArgumentException("Gênero do jogo é obrigatório.");
            if (price <= 0) throw new ArgumentException("Preço deve ser maior que zero.");

            Id = Guid.NewGuid();
            Name = name;
            Genre = genre;
            Price = price;
        }
    }
}
