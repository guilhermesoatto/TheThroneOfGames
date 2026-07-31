using GameStore.Usuarios.API.Controllers.Base;
using GameStore.Usuarios.Application.Commands;
using GameStore.Usuarios.Application.DTOs;
using GameStore.Usuarios.Application.Interfaces;
using GameStore.Usuarios.Application.Mappers;
using GameStore.CQRS.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Usuarios.API.Controllers.Admin;

/// <summary>
/// Admin controller for user management in the Usuarios bounded context — cadastro de
/// usuários, ativar/desativar contas e alterar role (RN Fase 1: "Administrador... administra
/// usuários"). Toda a lógica de aplicação já existia (IUsuarioService, CQRS command handlers);
/// este controller só expõe o que faltava por HTTP — ver docs/ai/tasks/
/// fix-admin-user-management-gap.md para o achado original.
/// Mirrors GameStore.Catalogo.API/Controllers/Admin/GameController.cs.
/// </summary>
/// <remarks>
/// Rota explícita: o token [controller] herdado de AdminControllerBase geraria
/// "UserManagement" (PascalCase, nome da classe) — o API Gateway já espera
/// "/api/admin/user-management" (kebab-case, ver api-gateway/nginx.conf), e o projeto não tem
/// nenhum transformador de rota kebab-case configurado. Precisa ser explícito.
/// </remarks>
[Route("api/admin/user-management")]
public class UserManagementController : AdminControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly ICommandHandler<CreateUserCommand> _createUserHandler;
    private readonly ICommandHandler<ChangeUserRoleCommand> _changeUserRoleHandler;

    public UserManagementController(
        IUsuarioService usuarioService,
        ICommandHandler<CreateUserCommand> createUserHandler,
        ICommandHandler<ChangeUserRoleCommand> changeUserRoleHandler)
    {
        _usuarioService = usuarioService;
        _createUserHandler = createUserHandler;
        _changeUserRoleHandler = changeUserRoleHandler;
    }

    /// <summary>
    /// List all users (Admin view)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UsuarioDTO>), 200)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var usuarios = await _usuarioService.GetAllUsersAsync();
            return Ok(UsuarioMapper.ToDTOList(usuarios));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UsuarioDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var usuario = await _usuarioService.GetUserByIdAsync(id);
            return Ok(UsuarioMapper.ToDTO(usuario));
        }
        catch (ArgumentException)
        {
            return NotFoundById<UsuarioDTO>(id);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Create a new user (Admin). Usuário nasce inativo (RN-005, mesmo padrão do self-registro
    /// público em /api/usuario/pre-register) — use POST .../{id}/enable para ativar sem
    /// depender do fluxo de token de ativação por e-mail.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDTO), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
    {
        try
        {
            var result = await _createUserHandler.HandleAsync(command);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Message, details = result.Errors });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.EntityId }, result.Data);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Change a user's role (User/Admin). O command existente (ChangeUserRoleCommand) opera
    /// por e-mail, não por id — resolvemos o e-mail a partir do id da rota para manter a API
    /// consistente (id no path) sem duplicar o command já validado em UsuarioValidators.
    /// </summary>
    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(UsuarioDTO), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        try
        {
            var usuario = await _usuarioService.GetUserByIdAsync(id);

            var command = new ChangeUserRoleCommand(usuario.Email, request.NewRole);
            var result = await _changeUserRoleHandler.HandleAsync(command);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Message, details = result.Errors });
            }

            return Ok(result.Data);
        }
        catch (ArgumentException)
        {
            return NotFoundById<UsuarioDTO>(id);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Disable a user account. Bloqueia dois casos que travariam o próprio sistema de
    /// administração (não vem do edital, é uma trava de bom senso): desativar a própria conta,
    /// e desativar o último Admin ativo restante.
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Disable(Guid id)
    {
        try
        {
            var callerIdClaim = User.FindFirst("sub");
            if (callerIdClaim != null && Guid.TryParse(callerIdClaim.Value, out var callerId) && callerId == id)
            {
                return BadRequest(new { error = "Não é possível desativar a própria conta." });
            }

            var usuario = await _usuarioService.GetUserByIdAsync(id);

            if (usuario.Role == "Admin" && usuario.IsActive)
            {
                var todos = await _usuarioService.GetAllUsersAsync();
                var outrosAdminsAtivos = todos.Any(u => u.Id != id && u.Role == "Admin" && u.IsActive);
                if (!outrosAdminsAtivos)
                {
                    return BadRequest(new { error = "Não é possível desativar o último administrador ativo." });
                }
            }

            await _usuarioService.DisableUserAsync(id);
            return Ok(new { message = "Usuário desativado com sucesso." });
        }
        catch (ArgumentException)
        {
            return NotFoundById<UsuarioDTO>(id);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Enable a user account.
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Enable(Guid id)
    {
        try
        {
            await _usuarioService.EnableUserAsync(id);
            return Ok(new { message = "Usuário ativado com sucesso." });
        }
        catch (ArgumentException)
        {
            return NotFoundById<UsuarioDTO>(id);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}

/// <summary>
/// Request model for PATCH .../{id}/role
/// </summary>
public class ChangeRoleRequest
{
    public required string NewRole { get; set; }
}
