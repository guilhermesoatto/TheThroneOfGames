using GameStore.Catalogo.API.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Catalogo.API.Controllers.Admin;

/// <summary>
/// Admin controller for promotion management in the Catalogo bounded context.
/// Note: Promotion feature is being redesigned. This is a placeholder.
/// </summary>
public class PromotionController : AdminControllerBase
{
    public PromotionController()
    {
    }

    /// <summary>
    /// Get all promotions (Feature under development)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public IActionResult GetAll()
    {
        // TODO: Implement promotion queries when Promotion bounded context is defined
        return Ok(new { message = "Promotion feature under development" });
    }

    /// <summary>
    /// Get promotion by ID (Feature under development)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public IActionResult GetById(Guid id)
    {
        return NotFound(new { message = "Promotion feature under development" });
    }

    /// <summary>
    /// Create promotion (Feature under development)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(501)]
    public IActionResult Create([FromBody] object dto)
    {
        return StatusCode(501, new { message = "Promotion feature under development" });
    }

    /// <summary>
    /// Update promotion (Feature under development)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(501)]
    public IActionResult Update(Guid id, [FromBody] object dto)
    {
        return StatusCode(501, new { message = "Promotion feature under development" });
    }

    /// <summary>
    /// Delete promotion (Feature under development)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(501)]
    public IActionResult Delete(Guid id)
    {
        return StatusCode(501, new { message = "Promotion feature under development" });
    }
}
