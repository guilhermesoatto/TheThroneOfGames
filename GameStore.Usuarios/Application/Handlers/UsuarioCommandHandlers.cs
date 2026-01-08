using GameStore.Usuarios.Application.Commands;
using GameStore.Usuarios.Application.Validators;
using GameStore.Usuarios.Application.DTOs;
using GameStore.Usuarios.Application.Mappers;
using GameStore.Usuarios.Domain.Entities;
using GameStore.Usuarios.Domain.Interfaces;
using GameStore.Common.Events;
using GameStore.Usuarios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GameStore.CQRS.Abstractions;

namespace GameStore.Usuarios.Application.Handlers
{
    /// <summary>
    /// Handler para ActivateUserCommand.
    /// </summary>
    public class ActivateUserCommandHandler : ICommandHandler<ActivateUserCommand>
    {
        private readonly IUsuarioRepository _userRepository;
        private readonly IEventBus _eventBus;

        public ActivateUserCommandHandler(IUsuarioRepository userRepository, IEventBus eventBus)
        {
            _userRepository = userRepository;
            _eventBus = eventBus;
        }

        public async Task<GameStore.CQRS.Abstractions.CommandResult> HandleAsync(ActivateUserCommand command)
        {
            // Validar command
            var validation = UsuarioValidators.Validate(command);
            if (!validation.IsValid)
            {
                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = false,
                    Message = "Validação falhou",
                    Errors = validation.Errors.Select(e => e.ErrorMessage).ToList()
                };
            }

            try
            {
                // Buscar usuário pelo token
                var user = await _userRepository.GetByActivationTokenAsync(command.ActivationToken);
                if (user == null)
                {
                    return new GameStore.CQRS.Abstractions.CommandResult
                    {
                        Success = false,
                        Message = "Token inválido ou expirado",
                        Errors = new List<string> { "Token não encontrado ou já utilizado" }
                    };
                }

                // Ativar usuário
                user.Activate();
                await _userRepository.UpdateAsync(user);

                // Publicar evento de domínio
                var usuarioAtivadoEvent = new UsuarioAtivadoEvent(
                    UsuarioId: user.Id,
                    Email: user.Email,
                    Nome: user.Name
                );
                await _eventBus.PublishAsync(usuarioAtivadoEvent);

                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = true,
                    Message = "Usuário ativado com sucesso",
                    EntityId = user.Id,
                    Data = UsuarioMapper.ToDTO(user)
                };
            }
            catch (Exception ex)
            {
                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = false,
                    Message = "Erro ao ativar usuário",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para UpdateUserProfileCommand.
    /// </summary>
    public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand>
    {
        private readonly IUsuarioRepository _userRepository;
        private readonly IEventBus _eventBus;

        public UpdateUserProfileCommandHandler(IUsuarioRepository userRepository, IEventBus eventBus)
        {
            _userRepository = userRepository;
            _eventBus = eventBus;
        }

        public async Task<GameStore.CQRS.Abstractions.CommandResult> HandleAsync(UpdateUserProfileCommand command)
        {
            // Validar command
            var validation = UsuarioValidators.Validate(command);
            if (!validation.IsValid)
            {
                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = false,
                    Message = "Validação falhou",
                    Errors = validation.Errors.Select(e => e.ErrorMessage).ToList()
                };
            }

            try
            {
                // Buscar usuário
                var user = await _userRepository.GetByEmailAsync(command.ExistingEmail);
                if (user == null)
                {
                    return new GameStore.CQRS.Abstractions.CommandResult
                    {
                        Success = false,
                        Message = "Usuário não encontrado",
                        Errors = new List<string> { $"Usuário com email {command.ExistingEmail} não encontrado" }
                    };
                }

                // Verificar se novo email já existe (se diferente)
                if (!string.Equals(command.ExistingEmail, command.NewEmail, StringComparison.OrdinalIgnoreCase))
                {
                    var existingUser = await _userRepository.GetByEmailAsync(command.NewEmail);
                    if (existingUser != null)
                    {
                        return new GameStore.CQRS.Abstractions.CommandResult
                        {
                            Success = false,
                            Message = "Email já está em uso",
                            Errors = new List<string> { $"O email {command.NewEmail} já está cadastrado" }
                        };
                    }
                }

                // Atualizar perfil
                user.UpdateProfile(command.NewName, command.NewEmail);
                await _userRepository.UpdateAsync(user);

                // Publicar evento de domínio
                var perfilAtualizadoEvent = new UsuarioPerfillAtualizadoEvent(
                    UsuarioId: user.Id,
                    NovoNome: command.NewName,
                    NovoEmail: command.NewEmail
                );
                await _eventBus.PublishAsync(perfilAtualizadoEvent);

                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = true,
                    Message = "Perfil atualizado com sucesso",
                    EntityId = user.Id,
                    Data = UsuarioMapper.ToDTO(user)
                };
            }
            catch (Exception ex)
            {
                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = false,
                    Message = "Erro ao atualizar perfil",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para CreateUserCommand.
    /// </summary>
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
    {
        private readonly IUsuarioRepository _userRepository;

        public CreateUserCommandHandler(IUsuarioRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<GameStore.CQRS.Abstractions.CommandResult> HandleAsync(CreateUserCommand command)
        {
            // Validar command
            var validation = UsuarioValidators.Validate(command);
            if (!validation.IsValid)
            {
                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = false,
                    Message = "Validação falhou",
                    Errors = validation.Errors.Select(e => e.ErrorMessage).ToList()
                };
            }

            try
            {
                // Verificar se email já existe
                var existingUser = await _userRepository.GetByEmailAsync(command.Email);
                if (existingUser != null)
                {
                    return new GameStore.CQRS.Abstractions.CommandResult
                    {
                        Success = false,
                        Message = "Email já está em uso",
                        Errors = new List<string> { $"O email {command.Email} já está cadastrado" }
                    };
                }

                // Criar usuário
                var user = new Usuario(
                    id: Guid.NewGuid(),
                    name: command.Name,
                    email: command.Email,
                    passwordHash: "", // Será gerado pelo serviço de senha
                    role: command.Role,
                    isActive: false, // Usuário começa inativo
                    activeToken: Guid.NewGuid().ToString() // Token para ativação
                );

                await _userRepository.AddAsync(user);

                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = true,
                    Message = "Usuário criado com sucesso",
                    EntityId = user.Id,
                    Data = UsuarioMapper.ToDTO(user)
                };
            }
            catch (Exception ex)
            {
                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = false,
                    Message = "Erro ao criar usuário",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Handler para ChangeUserRoleCommand.
    /// </summary>
    public class ChangeUserRoleCommandHandler : ICommandHandler<ChangeUserRoleCommand>
    {
        private readonly IUsuarioRepository _userRepository;

        public ChangeUserRoleCommandHandler(IUsuarioRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<GameStore.CQRS.Abstractions.CommandResult> HandleAsync(ChangeUserRoleCommand command)
        {
            // Validar command
            var validation = UsuarioValidators.Validate(command);
            if (!validation.IsValid)
            {
                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = false,
                    Message = "Validação falhou",
                    Errors = validation.Errors.Select(e => e.ErrorMessage).ToList()
                };
            }

            try
            {
                // Buscar usuário
                var user = await _userRepository.GetByEmailAsync(command.Email);
                if (user == null)
                {
                    return new GameStore.CQRS.Abstractions.CommandResult
                    {
                        Success = false,
                        Message = "Usuário não encontrado",
                        Errors = new List<string> { $"Usuário com email {command.Email} não encontrado" }
                    };
                }

                // Alterar role
                user.ChangeRole(command.NewRole);
                await _userRepository.UpdateAsync(user);

                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = true,
                    Message = "Role alterada com sucesso",
                    EntityId = user.Id,
                    Data = UsuarioMapper.ToDTO(user)
                };
            }
            catch (Exception ex)
            {
                return new GameStore.CQRS.Abstractions.CommandResult
                {
                    Success = false,
                    Message = "Erro ao alterar role",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
