using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Infrastructure.ExternalServices;

namespace TheThroneOfGames.Application;

public class UsuarioService //: IUsuarioService
{
//    private readonly IUserRepository _userRepository;
//    //private readonly IEmailService _emailService;

//    //public UserService(IUserRepository userRepository, IEmailService emailService)
//    //{
//    //    _userRepository = userRepository;
//    //    _emailService = emailService;
//    //}

//    //public async Task PreRegisterUserAsync(string email, string name)
//    //{
//    //    // Lógica de pré-cadastro
//    //    var user = new User(name, email, "hashSenha", "User");
//    //    await _userRepository.AddAsync(user);

//    //    // Enviar e-mail de ativação
//    //    await _emailService.SendEmailAsync(email, "Ativação de Conta", "Clique no link para ativar sua conta.");
//    //}

//    public async Task ActivateUserAsync(string activationToken)
//    {
//        // Lógica de ativação
//        var user = await _userRepository.GetByActivationTokenAsync(activationToken);
//        if (user == null)
//            throw new Exception("Token inválido ou expirado.");

//        user.Activate();
//        await _userRepository.UpdateAsync(user);
//    }
}
