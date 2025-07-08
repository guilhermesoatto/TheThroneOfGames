using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using TheThroneOfGames.API.Models.DTO;
using TheThroneOfGames.Application;
using TheThroneOfGames.Infrastructure.Email;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TheThroneOfGames.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        EmailService _emailService;
        TheThroneOfGames.Application. UsuarioService _userService;
        // GET: api/<UsuarioController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<UsuarioController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<UsuarioController>
        [HttpPost("register")]
        public IActionResult Post([FromBody] UserDTO value)
        {
            return Ok("Usuário registrado com sucesso!");
        }

        // PUT api/<UsuarioController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<UsuarioController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        [HttpPost("pre-register")]
        public async Task<IActionResult> PreRegisterUser([FromBody] UserDTO userDto)
        {
            // Validação do formato do e-mail
            if (!Regex.IsMatch(userDto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest("E-mail inválido.");

            // Enviar e-mail de ativação
            await _emailService.SendEmailAsync(userDto.Email, "Ativação de Conta", "Clique no link para ativar sua conta.");

            return Ok("E-mail de ativação enviado.");
        }

        [HttpPost("activate")]
        public IActionResult ActivateUser([FromQuery] string activationToken)
        {
            // Lógica para validar o token e ativar o usuário
            var user = 0;// _userService.ActivateUserAsync(activationToken);
            if (user == null)
                return BadRequest("Token inválido ou expirado.");

            return Ok("Usuário ativado com sucesso.");
        }
    }
}
