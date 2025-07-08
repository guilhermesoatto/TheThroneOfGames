using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Infrastructure.Repository.Interfaces;
using TheThroneOfGames.Infrastructure.Entities;
using System.Threading.Tasks;

namespace TheThroneOfGames.Application;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _userRepository;
    //private readonly IEmailService _emailService;

    public UsuarioService(IUsuarioRepository userRepository/*, IEmailService emailService*/)
    {
        _userRepository = userRepository;
        // _emailService = emailService;
    }

    public async Task PreRegisterUserAsync(string email, string name)
    {
        // Lógica de pré-cadastro
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Password = "hashSenha", // TODO: Hash real
            Role = "User",
            IsActive = false,
            ActiveToken = Guid.NewGuid().ToString(),
            Nickname = string.Empty
        };
        await _userRepository.AddAsync(user);

        // Enviar e-mail de ativação (descomente se IEmailService estiver disponível)
        // await _emailService.SendEmailAsync(email, "Ativação de Conta", "Clique no link para ativar sua conta.");
    }

    public async Task ActivateUserAsync(string activationToken)
    {
        // Lógica de ativação
        var user = await _userRepository.GetByActivationTokenAsync(activationToken);
        if (user == null)
            throw new Exception("Token inválido ou expirado.");

        user.IsActive = true;
        await _userRepository.UpdateAsync(user);
    }
}
