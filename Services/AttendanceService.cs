using AttendanceQR.Web.Data;
using AttendanceQR.Web.Domain.Entities;
using AttendanceQR.Web.Domain.Enums;
using AttendanceQR.Web.Services.Interfaces;
using AttendanceQR.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AttendanceQR.Web.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _db;
        public AttendanceService(AppDbContext db) => _db = db;

        public Task<bool> AlreadyCapturedAsync(string sessionId, string studentNumber, CancellationToken ct) =>
            _db.AttendanceRecords
               .Include(a => a.ClassSession)
               .AnyAsync(a => a.ClassSession!.SessionId == sessionId && a.StudentNumber == studentNumber, ct);

        public async Task<int> CaptureAsync(CapturePostVm vm, HttpContext http, CancellationToken ct)
        {
            var s = await _db.ClassSessions.FirstOrDefaultAsync(x => x.SessionId == vm.SessionId, ct);
            if (s == null || s.Nonce != vm.Nonce || s.NonceExpiresAtUtc < DateTime.UtcNow)
                return 0;

            var exists = await AlreadyCapturedAsync(vm.SessionId, vm.StudentNumber, ct);
            if (exists) return 0;

            var student = await _db.Students.FindAsync([vm.StudentNumber], ct);
            if (student == null)
            {
                student = new Student {
                    StudentNumber = vm.StudentNumber.Trim(),
                    FirstName = vm.FirstName.Trim(),
                    LastName  = vm.LastName.Trim(),
                    Programme = vm.Programme.Trim()
                };
                _db.Students.Add(student);
            }

            var rec = new AttendanceRecord {
                ClassSessionId = s.Id,
                StudentNumber = vm.StudentNumber.Trim(),
                Status = AttendanceStatus.Present,
                CapturedAtUtc = DateTime.UtcNow,
                SourceIp = http.Connection.RemoteIpAddress?.ToString(),
                UserAgent = http.Request.Headers.UserAgent.ToString(),
                NonceUsed = vm.Nonce
            };

            _db.AttendanceRecords.Add(rec);
            await _db.SaveChangesAsync(ct);
            return rec.Id;
        }
    }
}
