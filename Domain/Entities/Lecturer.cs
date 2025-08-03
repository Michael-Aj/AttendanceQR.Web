namespace AttendanceQR.Web.Domain.Entities
{
    public class Lecturer
    {
        public string StaffEmail { get; set; } = default!; // PK
        public string? DisplayName { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginUtc { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
