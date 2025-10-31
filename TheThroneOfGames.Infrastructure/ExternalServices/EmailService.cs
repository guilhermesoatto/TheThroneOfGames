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
        private readonly string _outboxPath;

        public EmailService()
        {
            // Default Outbox in project folder for development and tests
            _outboxPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox");
            if (!Directory.Exists(_outboxPath))
                Directory.CreateDirectory(_outboxPath);
        }

        /// <summary>
        /// Writes an e-mail file to the Outbox folder (development-friendly). The file contains a simple plain-text representation.
        /// </summary>
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var fileName = $"email_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid()}.eml";
            var path = Path.Combine(_outboxPath, fileName);

            var sb = new StringBuilder();
            sb.AppendLine($"From: Plataforma de Jogos <noreply@plataformajogos.com>");
            sb.AppendLine($"To: {to}");
            sb.AppendLine($"Subject: {subject}");
            sb.AppendLine("Content-Type: text/plain; charset=utf-8");
            sb.AppendLine();
            sb.AppendLine(body);

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }
    }
}
