﻿using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;

namespace TheThroneOfGames.Application;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _userRepository;

    public UsuarioService(IUsuarioRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task ActivateUserAsync(string activationToken)
    {
        throw new NotImplementedException();
    }

    public async Task PreRegisterUserAsync(string email, string name)
    {
        // Lógica de pré-cadastro
        var user = new Usuario(name, email, "hashSenha", "User", "activationToken");
        await _userRepository.AddAsync(user);

    }

    //public async Task ActivateUserAsync(string activationToken)
    //{
    //    // Lógica de ativação
    //    var user = await _userRepository.GetByActivationTokenAsync(activationToken);
    //    if (user == null)
    //        throw new Exception("Token inválido ou expirado.");

    //    user.Activate();
    //    await _userRepository.UpdateAsync(user);
    //}
}
