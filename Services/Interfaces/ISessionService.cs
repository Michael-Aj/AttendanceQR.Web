using AttendanceQR.Web.Domain.Entities;
using AttendanceQR.Web.ViewModels;

namespace AttendanceQR.Web.Services.Interfaces
{
    public interface ISessionService
    {
        Task<ClassSession> CreateAsync(CreateSessionVm vm, CancellationToken ct);
        Task<string> IssueNonceAsync(int classSessionId, TimeSpan ttl, CancellationToken ct);
        Task<ClassSession?> FindBySessionIdAsync(string sessionId, CancellationToken ct);
    }
}
