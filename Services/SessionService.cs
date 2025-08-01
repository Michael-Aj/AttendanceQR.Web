using AttendanceQR.Web.Data;
using AttendanceQR.Web.Domain.Entities;
using AttendanceQR.Web.Services.Interfaces;
using AttendanceQR.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AttendanceQR.Web.Services
{
    public class SessionService : ISessionService
    {
        private readonly AppDbContext _db;
        public SessionService(AppDbContext db) => _db = db;

        //public async Task<ClassSession> CreateAsync(CreateSessionVm vm, CancellationToken ct)
        //{
        //    var sessionId = $"{vm.ClassDate:yyyy-MM-dd}_{vm.ModuleCode}_{vm.VenueCode}_{vm.StartTime:HH'h'mm}";
        //    var s = new ClassSession {
        //        SessionId = sessionId,
        //        ModuleCode = vm.ModuleCode,
        //        VenueCode = vm.VenueCode,
        //        ClassDate = vm.ClassDate,
        //        StartTime = vm.StartTime,
        //        EndTime = vm.EndTime
        //    };
        //    _db.ClassSessions.Add(s);
        //    await _db.SaveChangesAsync(ct);
        //    return s;
        //}

        public async Task<ClassSession> CreateAsync(CreateSessionVm vm, CancellationToken ct)
        {
            var moduleCode = vm.ModuleCode.Trim().ToUpperInvariant();
            var venueCode = vm.VenueCode.Trim().ToUpperInvariant();

            // Try to find; create if missing
            var module = await _db.Modules.FindAsync([moduleCode], ct);
            if (module is null)
            {
                module = new Module { ModuleCode = moduleCode, Name = moduleCode };
                _db.Modules.Add(module);
            }

            var venue = await _db.Venues.FindAsync([venueCode], ct);
            if (venue is null)
            {
                venue = new Venue { Code = venueCode, Name = venueCode };
                _db.Venues.Add(venue);
            }

            // Ensure principals exist before referencing them
            await _db.SaveChangesAsync(ct);

            var sessionId = $"{vm.ClassDate:yyyy-MM-dd}_{moduleCode}_{venueCode}_{vm.StartTime:HH'h'mm}";

            // Optional: prevent duplicates
            var exists = await _db.ClassSessions.AnyAsync(s => s.SessionId == sessionId, ct);
            if (exists) throw new InvalidOperationException($"Session '{sessionId}' already exists.");

            var s = new ClassSession
            {
                SessionId = sessionId,
                ModuleCode = moduleCode,
                VenueCode = venueCode,
                ClassDate = vm.ClassDate,
                StartTime = vm.StartTime,
                EndTime = vm.EndTime
            };

            _db.ClassSessions.Add(s);
            await _db.SaveChangesAsync(ct);
            return s;
        }


        public async Task<string> IssueNonceAsync(int classSessionId, TimeSpan ttl, CancellationToken ct)
        {
            var s = await _db.ClassSessions.FindAsync([classSessionId], ct);
            if (s == null) throw new InvalidOperationException("Session not found");

            s.Nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..12]
                        .Replace("/", "x").Replace("+", "y");
            s.NonceExpiresAtUtc = DateTime.UtcNow.Add(ttl);
            await _db.SaveChangesAsync(ct);
            return s.Nonce!;
        }

        public Task<ClassSession?> FindBySessionIdAsync(string sessionId, CancellationToken ct) =>
            _db.ClassSessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct);
    }
}
