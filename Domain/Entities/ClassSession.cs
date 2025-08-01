using System.Collections.Generic;

namespace AttendanceQR.Web.Domain.Entities
{
    public class ClassSession
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = default!; // e.g., 2025-07-28_ISP152_CEN101_08h00 (unique)
        public string ModuleCode { get; set; } = default!;
        public string VenueCode { get; set; } = default!;
        public DateOnly ClassDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        // QR / Nonce
        public string? Nonce { get; set; }
        public DateTime? NonceExpiresAtUtc { get; set; }
        public string? QrUrl { get; set; }

        public Module? Module { get; set; }
        public Venue? Venue { get; set; }
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}
