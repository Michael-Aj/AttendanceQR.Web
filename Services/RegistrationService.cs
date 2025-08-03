using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Linq; // <-- needed for Any/Select
using AttendanceQR.Web.Data;
using AttendanceQR.Web.Domain.Entities;
using AttendanceQR.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AttendanceQR.Web.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly AppDbContext _db;
        private readonly IEmailSender _email;
        private readonly IConfiguration _cfg;

        public RegistrationService(AppDbContext db, IEmailSender email, IConfiguration cfg)
        { _db = db; _email = email; _cfg = cfg; }

        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        public async Task StartRegistrationAsync(string staffEmail, string baseUrl, CancellationToken ct)
        {
            staffEmail = (staffEmail ?? string.Empty).Trim().ToLowerInvariant();

            // domain allowlist (no Binder package needed)
            var allowed = _cfg.GetSection("Auth:AllowedStaffDomains").Get<string[]>() ?? Array.Empty<string>();
            if (allowed.Length > 0 &&
                !allowed.Any(d => staffEmail.EndsWith("@" + d, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Please use your staff email address.");
            }

            // issue token
            var minutes = _cfg.GetValue<int>("Auth:MagicLinkMinutes", 20);
            var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                                   .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var tokenHash = Sha256(rawToken);
            var expires = DateTime.UtcNow.AddMinutes(minutes);

            _db.EmailSignInTokens.Add(new EmailSignInToken
            {
                StaffEmail = staffEmail,
                TokenHash = tokenHash,
                ExpiresAtUtc = expires
            });
            await _db.SaveChangesAsync(ct);

            // link
            var link = $"{baseUrl}/Account/Verify?email={Uri.EscapeDataString(staffEmail)}&token={Uri.EscapeDataString(rawToken)}";
            var html = $@"<p>Click to complete your registration/sign in:</p>
                          <p><a href=""{link}"">Sign in to AttendanceQR</a></p>
                          <p>This link expires in {minutes} minutes.</p>";

            await _email.SendAsync(staffEmail, "AttendanceQR sign-in", html, ct);
        }

        public async Task<ClaimsPrincipal?> CompleteAsync(string email, string token, CancellationToken ct)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            var hash = Sha256(token ?? string.Empty);

            var rec = await _db.EmailSignInTokens
                .Where(t => t.StaffEmail == email && t.TokenHash == hash && t.UsedAtUtc == null)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(ct);

            if (rec == null || rec.ExpiresAtUtc < DateTime.UtcNow) return null;

            rec.UsedAtUtc = DateTime.UtcNow;

            // Use classic FindAsync overload for single PK
            var lec = await _db.Lecturers.FindAsync(new object?[] { email }, ct);
            if (lec == null)
            {
                lec = new Lecturer { StaffEmail = email, DisplayName = email.Split('@')[0] };
                _db.Lecturers.Add(lec);
            }
            lec.LastLoginUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, lec.DisplayName ?? email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "Lecturer")
            };
            var id = new ClaimsIdentity(claims, "Cookies");
            return new ClaimsPrincipal(id);
        }
    }
}
