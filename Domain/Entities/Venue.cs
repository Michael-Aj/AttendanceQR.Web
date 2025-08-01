namespace AttendanceQR.Web.Domain.Entities
{
    public class Venue
    {
        public string Code { get; set; } = default!;
        public string? Name { get; set; }
        public int? Capacity { get; set; }
    }
}
