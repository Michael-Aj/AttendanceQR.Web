using AttendanceQR.Web.ViewModels;

namespace AttendanceQR.Web.Services.Interfaces
{
    public interface IAttendanceService
    {
        Task<bool> AlreadyCapturedAsync(string sessionId, string studentNumber, CancellationToken ct);
        Task<int> CaptureAsync(CapturePostVm vm, HttpContext http, CancellationToken ct);
    }
}
