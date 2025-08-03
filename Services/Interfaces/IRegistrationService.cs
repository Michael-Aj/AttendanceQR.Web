using System.Security.Claims;

namespace AttendanceQR.Web.Services.Interfaces
{
    public interface IRegistrationService
    {
        Task StartRegistrationAsync(string staffEmail, string baseUrl, CancellationToken ct);
        Task<ClaimsPrincipal?> CompleteAsync(string email, string token, CancellationToken ct);
    }
}
