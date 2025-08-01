using System.Collections.Generic;

namespace AttendanceQR.Web.Domain.Entities
{
    public class Student
    {
        public string StudentNumber { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Programme { get; set; } = default!;

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}
