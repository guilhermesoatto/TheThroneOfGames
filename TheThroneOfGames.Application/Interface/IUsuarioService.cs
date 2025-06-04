using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Application.Interface
{
    public interface IUsuarioService
    {

        Task PreRegisterUserAsync(string email, string name);
        Task ActivateUserAsync(string activationToken);
    }
}
