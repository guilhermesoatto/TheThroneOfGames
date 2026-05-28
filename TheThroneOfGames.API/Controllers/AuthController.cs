using Microsoft.AspNetCore.Mvc;

namespace TheThroneOfGames.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Authentication has been moved to the GameStore.Usuarios bounded context.
        // This endpoint is kept for backwards compatibility but always redirects.
        [HttpPost("login")]
        public IActionResult Login()
        {
            return StatusCode(410, new
            {
                message = "Este endpoint foi descontinuado. Use o serviço de Usuários: POST /api/Usuario/login",
                movedTo = "/api/Usuario/login"
            });
        }
    }
}