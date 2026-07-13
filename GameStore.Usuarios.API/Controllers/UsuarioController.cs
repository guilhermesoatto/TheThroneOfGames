using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GameStore.Usuarios.Application.Interfaces;
using GameStore.Usuarios.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace GameStore.Usuarios.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly GameStore.Usuarios.Application.Services.AuthenticationService _authService;
        private readonly IInventarioRepository _inventarioRepository;

        public UsuarioController(
            IUsuarioService usuarioService,
            GameStore.Usuarios.Application.Services.AuthenticationService authService,
            IInventarioRepository inventarioRepository)
        {
            _usuarioService = usuarioService;
            _authService = authService;
            _inventarioRepository = inventarioRepository;
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

            var email = loginRequest.Email ?? loginRequest.Username;
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email é obrigatório." });

            try
            {
                var token = await _authService.AuthenticateAsync(email, loginRequest.Password);
                if (token == null)
                    return Unauthorized(new { message = "Credenciais inválidas ou conta não ativada." });

                var user = await _authService.GetUserByEmailAsync(email);
                var role = user?.Role ?? "User";

                return Ok(new { token, role });
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

        /// <summary>
        /// Diz se o jogador logado (identificado pelo JWT) já comprou o jogo informado.
        /// Consultado pelo bounded context de Partidas (GameStore.Partidas) antes de liberar a
        /// busca por partida — só quem tem o jogo pode entrar na fila de matchmaking dele.
        /// </summary>
        [HttpGet("possui-jogo/{jogoId:guid}")]
        [Authorize]
        public async Task<IActionResult> PossuiJogo(Guid jogoId)
        {
            var userIdClaim = User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var usuarioId))
                return Unauthorized();

            var possuiJogo = await _inventarioRepository.PossuiJogoAsync(usuarioId, jogoId);
            return Ok(new { possuiJogo });
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

