using System.Net;
using System.Net.Mail;
using AttendanceQR.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AttendanceQR.Web.Services
{
    public class SmtpOptions
    {
        public bool Enabled { get; set; } = false;
        public string From { get; set; } = "";
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public bool UseStartTls { get; set; } = true;
    }

    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _opt;
        public SmtpEmailSender(IOptions<SmtpOptions> opt) => _opt = opt.Value;

        public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (!_opt.Enabled)
            {
                // Dev fallback: write email to console
                Console.WriteLine("=== DEV EMAIL ===");
                Console.WriteLine($"To: {to}\nSubject: {subject}\n\n{htmlBody}\n");
                return;
            }

            using var msg = new MailMessage(_opt.From, to, subject, htmlBody) { IsBodyHtml = true };
            using var client = new SmtpClient(_opt.Host, _opt.Port)
            {
                EnableSsl = _opt.UseStartTls,
                Credentials = new NetworkCredential(_opt.User, _opt.Password)
            };
            await client.SendMailAsync(msg, ct);
        }
    }
}
