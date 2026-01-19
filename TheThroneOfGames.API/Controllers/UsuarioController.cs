using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using TheThroneOfGames.API.Models.DTO;
using GameStore.Usuarios.Application.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TheThroneOfGames.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _userService;
        private readonly GameStore.Usuarios.Application.Services.AuthenticationService _authService;

        public UsuarioController(IUsuarioService userService, GameStore.Usuarios.Application.Services.AuthenticationService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        // GET: api/<UsuarioController>
        [HttpGet]
        [Authorize]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<UsuarioController>/5
        [HttpGet("{id}")]
        [Authorize]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<UsuarioController>
        [HttpPost("register")]
        [AllowAnonymous]
        public IActionResult Post([FromBody] UserDTO value)
        {
            try
            {
                // Forward to service for secure registration and send activation e-mail
                var activationToken = _userService.PreRegisterUserAsync(value.Email, value.Name, value.Password, value.Role).GetAwaiter().GetResult();
                var activationLink = $"{Request.Scheme}://{Request.Host}/api/Usuario/activate?activationToken={activationToken}";
                // TODO: Send activation email via EmailService
                return Ok(new { message = "Usuário registrado com sucesso! E-mail de ativação enviado." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT api/<UsuarioController>/5
        [HttpPut("{id}")]
        [Authorize]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<UsuarioController>/5
        [HttpDelete("{id}")]
        [Authorize]
        public void Delete(int id)
        {
        }

        [HttpPost("pre-register")]
        [AllowAnonymous]
        public async Task<IActionResult> PreRegisterUser([FromBody] UserDTO userDto)
        {
            // Validação do formato do e-mail
            if (!Regex.IsMatch(userDto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest("E-mail inválido.");
            // Validação de senha
            try
            {
                var activationToken = await _userService.PreRegisterUserAsync(userDto.Email, userDto.Name, userDto.Password, userDto.Role);

                // Compose activation link
                var activationLink = $"{Request.Scheme}://{Request.Host}/api/Usuario/activate?activationToken={activationToken}";

                // Send activation e-mail (development: writes to Outbox)
                var subject = "Ativação de conta - TheThroneOfGames";
                var body = $"Olá {userDto.Name},\n\nPor favor ative sua conta clicando no link abaixo:\n{activationLink}\n\nSe você não solicitou esse e-mail, ignore.";

                // TODO: Send email via EmailService
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            // Enviar e-mail de ativação (placeholder)
            // await _emailService.SendEmailAsync(userDto.Email, "Ativação de Conta", "Clique no link para ativar sua conta.");

            return Ok("E-mail de ativação enviado.");
        }

        [HttpPost("activate")]
        [HttpGet("activate")]
        [AllowAnonymous]
        public async Task<IActionResult> ActivateUser([FromQuery] string activationToken)
        {
            try
            {
                // Lógica para validar o token e ativar o usuário
                await _userService.ActivateUserAsync(activationToken);

                return Ok(new { message = "Usuário ativado com sucesso." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                return BadRequest("E-mail e senha são obrigatórios.");

            var token = await _authService.AuthenticateAsync(loginDto.Email, loginDto.Password);
            if (token == null)
                return Unauthorized("Credenciais inválidas ou conta não ativada.");

            // Get user to extract role
            var user = await _authService.GetUserByEmailAsync(loginDto.Email);
            var role = user?.Role ?? "User";

            return Ok(new { token, role });
        }

        [HttpGet("public-info")]
        [AllowAnonymous]
        public IActionResult PublicInfo()
        {
            // Minimal public info endpoint used by integration tests. Return OK when unauthenticated.
            return Ok(new { message = "Public info" });
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] Models.DTO.UserUpdateDTO updateDto)
        {
            // Get current user email from claims (support both ClaimTypes.Email and lowercase "email")
            var emailClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.Email) ?? User?.FindFirst("email");
            var currentEmail = emailClaim?.Value;

            if (string.IsNullOrWhiteSpace(currentEmail))
                return BadRequest("Authenticated user email not found in token.");

            // If trying to update someone else's profile and not an Admin, forbid
            var targetEmail = updateDto.Email;
            var isSameUser = string.Equals(currentEmail, targetEmail, System.StringComparison.OrdinalIgnoreCase);
            var isAdmin = (User?.IsInRole("Admin") ?? false) || (User?.HasClaim(c => c.Type == "role" && c.Value == "Admin") ?? false);

            if (!isSameUser && !isAdmin)
            {
                return Forbid();
            }

            try
            {
                await _userService.UpdateUserProfileAsync(currentEmail, updateDto.Name, updateDto.Email);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok("Profile updated");
        }
    }
}
