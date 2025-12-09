using GameStore.Usuarios.Application.DTOs;
using TheThroneOfGames.Domain.Entities;
using System.Linq;
using System.Collections.Generic;
using System;

namespace GameStore.Usuarios.Application.Mappers
{
    /// <summary>
    /// Mapper para converter entre a entidade Usuario (local do contexto) e UsuarioDTO.
    /// Responsável por mapeamentos bidirecionais mantendo a integridade dos dados.
    /// </summary>
    public static class UsuarioMapper
    {
        /// <summary>
        /// Converte um Usuario para UsuarioDTO.
        /// Remove informações sensíveis como senha e tokens de ativação.
        /// </summary>
        public static UsuarioDTO ToDTO(Usuario usuario)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario));

            return new UsuarioDTO
            {
                Id = usuario.Id,
                Name = usuario.Name,
                Email = usuario.Email,
                Role = usuario.Role,
                IsActive = usuario.IsActive,
                CreatedAt = DateTime.UtcNow, // Será ajustado quando Usuario tiver CreatedAt
                UpdatedAt = null // Será ajustado quando Usuario tiver UpdatedAt
            };
        }

        /// <summary>
        /// Converte um UsuarioDTO para Usuario.
        /// Este método cria uma instância básica sem informações confidenciais.
        /// Usado principalmente para reconstrução de objetos a partir de DTOs.
        /// </summary>
        public static Usuario FromDTO(UsuarioDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Criando um Usuario com dados básicos do DTO
            // Nota: PasswordHash e ActiveToken serão gerenciados pelo serviço de aplicação
            var usuario = new Usuario(
                id: dto.Id,
                name: dto.Name,
                email: dto.Email,
                passwordHash: string.Empty, // Será preenchido pelo serviço
                role: dto.Role,
                isActive: dto.IsActive,
                activeToken: string.Empty // Será preenchido pelo serviço
            );

            return usuario;
        }

        /// <summary>
        /// Converte uma coleção de Usuarios para uma coleção de UsuarioDTOs.
        /// </summary>
        public static IEnumerable<UsuarioDTO> ToDTOList(IEnumerable<Usuario> usuarios)
        {
            if (usuarios == null)
                throw new ArgumentNullException(nameof(usuarios));

            return usuarios.Select(ToDTO).ToList();
        }
    }
}
