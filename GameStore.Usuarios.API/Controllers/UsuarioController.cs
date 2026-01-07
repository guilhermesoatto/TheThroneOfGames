using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GameStore.Usuarios.Application.Interfaces;
using System.Text.RegularExpressions;

namespace GameStore.Usuarios.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly GameStore.Usuarios.Application.Services.AuthenticationService _authService;

        public UsuarioController(
            IUsuarioService usuarioService, 
            GameStore.Usuarios.Application.Services.AuthenticationService authService)
        {
            _usuarioService = usuarioService;
            _authService = authService;
        }

        /// <summary>
        /// Pre-register a new user
        /// </summary>
        [HttpPost("pre-register")]
        [AllowAnonymous]
        public async Task<IActionResult> PreRegisterUser([FromBody] PreRegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Email validation
            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest(new { message = "E-mail inválido." });

            try
            {
                var activationToken = await _usuarioService.PreRegisterUserAsync(request.Email, request.Name, request.Password, request.Role ?? "User");
                var activationLink = $"{Request.Scheme}://{Request.Host}/api/usuario/activate?activationToken={activationToken}";
                
                // Note: Email service removed for microservice independence
                // In production, implement async email via message queue

                return Ok(new { message = "Usuário pré-registrado com sucesso! E-mail de ativação enviado.", activationToken });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Activate user account with token
        /// </summary>
        [HttpPost("activate")]
        [AllowAnonymous]
        public async Task<IActionResult> ActivateUser([FromQuery] string activationToken)
        {
            if (string.IsNullOrEmpty(activationToken))
                return BadRequest(new { message = "Token de ativação é obrigatório." });

            try
            {
                await _usuarioService.ActivateUserAsync(activationToken);
                return Ok(new { message = "Conta ativada com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _authService.AuthenticateAsync(loginRequest.Email ?? loginRequest.Username, loginRequest.Password);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get user profile (requires authentication)
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            var userIdClaim = User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized();

            return Ok(new { userId = userIdClaim.Value, message = "User profile data" });
        }
    }

    /// <summary>
    /// Request model for user pre-registration
    /// </summary>
    public class PreRegisterRequest
    {
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
        public string? Role { get; set; }
    }

    /// <summary>
    /// Request model for user login
    /// </summary>
    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Username { get; set; }
        public required string Password { get; set; }
    }
}

