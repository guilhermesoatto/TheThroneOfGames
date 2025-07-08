using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TheThroneOfGames.Infrastructure.Email
{
    public class SmtpSettings
    {
        [Required]
        public string Host { get; set; } = null!;

        [Required]
        [Range(1, 65535)]
        public int Port { get; set; }

        [Required]
        public string User { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string From { get; set; } = null!;

        public bool UseSsl { get; set; } = true;
    }
}
