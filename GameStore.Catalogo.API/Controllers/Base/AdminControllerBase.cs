using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Catalogo.API.Controllers.Base;

/// <summary>
/// Base controller for admin operations in the Catalogo bounded context.
/// All admin controllers inherit from this to get authentication and error handling.
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public abstract class AdminControllerBase : ControllerBase
{
    protected IActionResult HandleError(Exception ex)
    {
        // Log error details here if needed
        if (ex is ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }

        return StatusCode(500, new { error = "An unexpected error occurred." });
    }

    protected IActionResult NotFoundById<T>(Guid id)
    {
        return NotFound(new { error = $"{typeof(T).Name} with ID {id} not found." });
    }
}
