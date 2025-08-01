using AttendanceQR.Web.Domain.Enums;

namespace AttendanceQR.Web.Domain.Entities
{
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int ClassSessionId { get; set; }
        public string StudentNumber { get; set; } = default!;
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
        public string? SourceIp { get; set; }
        public string? UserAgent { get; set; }
        public string? NonceUsed { get; set; }

        public ClassSession? ClassSession { get; set; }
        public Student? Student { get; set; }
    }
}
