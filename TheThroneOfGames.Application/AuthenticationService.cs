namespace TheThroneOfGames.Application
{
    public class AuthenticationService
    {
        //    private readonly IAuthenticationRepository _repository;
        //    private readonly IJwtTokenGenerator _jwtTokenGenerator;

        //    public AuthenticationService(IAuthenticationRepository repository, IJwtTokenGenerator jwtTokenGenerator)
        //    {
        //        _repository = repository;
        //        _jwtTokenGenerator = jwtTokenGenerator;
        //    }

        //    public async Task<string?> AuthenticateAsync(string email, string password)
        //    {
        //        var user = await _repository.GetByEmailAsync(email);
        //        if (user == null || user.PasswordHash != password || !user.IsActive)
        //            return null;
        //        return _jwtTokenGenerator.GenerateToken(user);
        //    }
        //}

        //public interface IJwtTokenGenerator
        //{
        //    string GenerateToke(Usuario user);
        //}
    }
}
