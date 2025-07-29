using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;

namespace TheThroneOfGames.Infrastructure.Repository
{
    public class UsuarioRepository : IUsuarioRepository
    {
        public Task AddAsync(Usuario entity)
        {
            // Implementação do método para adicionar um usuário  
            throw new NotImplementedException();
        }

        public Task DeleteAsync(Guid id)
        {
            // Implementação do método para deletar um usuário  
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Usuario>> GetAllAsync()
        {
            // Implementação do método para obter todos os usuários  
            throw new NotImplementedException();
        }

        public Task<Usuario?> GetByIdAsync(Guid id)
        {
            // Implementação do método para obter um usuário pelo ID  
            throw new NotImplementedException();
        }

        public Task<Usuario?> GetByActivationTokenAsync(string activationToken)
        {
            // Implementação do método para obter um usuário pelo token de ativação
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Usuario entity)
        {
            // Implementação do método para atualizar um usuário  
            throw new NotImplementedException();
        }
    }
}
