using Microsoft.AspNetCore.Mvc;
using TheThroneOfGames.API.Controllers.Base;
using TheThroneOfGames.API.Models.DTO;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.API.Controllers.Admin;

[Route("api/admin/user-management")]
public class UserManagementController : AdminControllerBase
{
    private readonly IUsuarioService _userService;

    public UserManagementController(IUsuarioService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserListDTO>), 200)]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            var dtos = users.Select(u => new UserListDTO
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserListDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            var dto = new UserListDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive
            };

            return Ok(dto);
        }
        catch (ArgumentException)
        {
            return NotFoundById<Usuario>(id);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UserRoleUpdateDTO dto)
    {
        try
        {
            await _userService.UpdateUserRoleAsync(id, dto.Role);
            return Ok();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFoundById<Usuario>(id);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DisableUser(Guid id)
    {
        try
        {
            await _userService.DisableUserAsync(id);
            return Ok();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFoundById<Usuario>(id);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> EnableUser(Guid id)
    {
        try
        {
            await _userService.EnableUserAsync(id);
            return Ok();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFoundById<Usuario>(id);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}