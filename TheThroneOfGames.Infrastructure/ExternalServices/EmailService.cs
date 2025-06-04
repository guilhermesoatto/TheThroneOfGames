using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;

namespace TheThroneOfGames.Infrastructure.ExternalServices
{
    public class EmailService
    {
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Plataforma de Jogos", "noreply@plataformajogos.com"));
            email.To.Add(new MailboxAddress(name: to, address: to));
            email.Subject = subject;
            email.Body = new TextPart("plain") { Text = body };

            using var smtp = new SmtpClient();
            //await smtp.ConnectAsync("smtp.seuservidor.com", 587, false);
            //await smtp.AuthenticateAsync("seuusuario", "suasenha");
            //await smtp.SendAsync(email);
            //await smtp.DisconnectAsync(true);
        }
    }
}
