using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using TheThroneOfGames.API.Models.DTO;
using TheThroneOfGames.Application;

namespace TheThroneOfGames.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly UsuarioService _userService;

        public UsuarioController(UsuarioService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        [HttpPost("register")]
        public IActionResult Post([FromBody] UserDTO value)
        {
            return Ok("Usuário registrado com sucesso!");
        }

        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        [HttpPost("pre-register")]
        public async Task<IActionResult> PreRegisterUser([FromBody] UserDTO userDto)
        {
            if (!Regex.IsMatch(userDto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest("E-mail inválido.");

            // Aqui você pode chamar um serviço de domínio para enviar o e-mail
            // await _userService.PreRegisterUserAsync(userDto.Email, userDto.Name);

            return Ok("E-mail de ativação enviado.");
        }

        [HttpPost("activate")]
        public async Task<IActionResult> ActivateUser([FromQuery] string activationToken)
        {
            // Lógica para validar o token e ativar o usuário
            // var user = await _userService.ActivateUserAsync(activationToken);
            // if (user == null)
            //     return BadRequest("Token inválido ou expirado.");

            return Ok("Usuário ativado com sucesso.");
        }
    }
}
