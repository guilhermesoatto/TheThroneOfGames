using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TheThroneOfGames.API.Controllers.Base;

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