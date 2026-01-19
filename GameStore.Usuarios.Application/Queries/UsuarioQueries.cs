using GameStore.Usuarios.Application.DTOs;
using GameStore.Usuarios.Application.Mappers;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using GameStore.CQRS.Abstractions;

namespace GameStore.Usuarios.Application.Queries
{
    /// <summary>
    /// Query para obter usuário por ID.
    /// </summary>
    public record GetUserByIdQuery(Guid UserId) : IQuery<UsuarioDTO?>;

    /// <summary>
    /// Query para obter usuário por email.
    /// </summary>
    public record GetUserByEmailQuery(string Email) : IQuery<UsuarioDTO?>;

    /// <summary>
    /// Query para obter todos os usuários.
    /// </summary>
    public record GetAllUsersQuery() : IQuery<IEnumerable<UsuarioDTO>>;

    /// <summary>
    /// Query para obter usuários por role.
    /// </summary>
    public record GetUsersByRoleQuery(string Role) : IQuery<IEnumerable<UsuarioDTO>>;

    /// <summary>
    /// Query para obter usuários ativos.
    /// </summary>
    public record GetActiveUsersQuery() : IQuery<IEnumerable<UsuarioDTO>>;

    /// <summary>
    /// Query para verificar se email existe.
    /// </summary>
    public record CheckEmailExistsQuery(string Email) : IQuery<bool>;

    /// <summary>
    /// Handler para GetUserByIdQuery.
    /// </summary>
    public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UsuarioDTO?>
    {
        private readonly IUsuarioRepository _userRepository;

        public GetUserByIdQueryHandler(IUsuarioRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UsuarioDTO?> HandleAsync(GetUserByIdQuery query)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(query.UserId);
                return user != null ? UsuarioMapper.ToDTO(user) : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Handler para GetUserByEmailQuery.
    /// </summary>
    public class GetUserByEmailQueryHandler : IQueryHandler<GetUserByEmailQuery, UsuarioDTO?>
    {
        private readonly IUsuarioRepository _userRepository;

        public GetUserByEmailQueryHandler(IUsuarioRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UsuarioDTO?> HandleAsync(GetUserByEmailQuery query)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(query.Email);
                return user != null ? UsuarioMapper.ToDTO(user) : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Handler para GetAllUsersQuery.
    /// </summary>
    public class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, IEnumerable<UsuarioDTO>>
    {
        private readonly IUsuarioRepository _userRepository;

        public GetAllUsersQueryHandler(IUsuarioRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UsuarioDTO>> HandleAsync(GetAllUsersQuery query)
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                return UsuarioMapper.ToDTOList(users);
            }
            catch
            {
                return Enumerable.Empty<UsuarioDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetUsersByRoleQuery.
    /// </summary>
    public class GetUsersByRoleQueryHandler : IQueryHandler<GetUsersByRoleQuery, IEnumerable<UsuarioDTO>>
    {
        private readonly IUsuarioRepository _userRepository;

        public GetUsersByRoleQueryHandler(IUsuarioRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UsuarioDTO>> HandleAsync(GetUsersByRoleQuery query)
        {
            try
            {
                var users = await _userRepository.GetByRoleAsync(query.Role);
                return UsuarioMapper.ToDTOList(users);
            }
            catch
            {
                return Enumerable.Empty<UsuarioDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetActiveUsersQuery.
    /// </summary>
    public class GetActiveUsersQueryHandler : IQueryHandler<GetActiveUsersQuery, IEnumerable<UsuarioDTO>>
    {
        private readonly IUsuarioRepository _userRepository;

        public GetActiveUsersQueryHandler(IUsuarioRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UsuarioDTO>> HandleAsync(GetActiveUsersQuery query)
        {
            try
            {
                var users = await _userRepository.GetActiveUsersAsync();
                return UsuarioMapper.ToDTOList(users);
            }
            catch
            {
                return Enumerable.Empty<UsuarioDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para CheckEmailExistsQuery.
    /// </summary>
    public class CheckEmailExistsQueryHandler : IQueryHandler<CheckEmailExistsQuery, bool>
    {
        private readonly IUsuarioRepository _userRepository;

        public CheckEmailExistsQueryHandler(IUsuarioRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> HandleAsync(CheckEmailExistsQuery query)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(query.Email);
                return user != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
