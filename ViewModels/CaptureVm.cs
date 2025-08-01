namespace AttendanceQR.Web.ViewModels
{
    public class CaptureVm
    {
        public string SessionId { get; set; } = default!;
        public string ModuleCode { get; set; } = default!;
        public string Venue { get; set; } = default!;
        public DateOnly ClassDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public string? Nonce { get; set; }
    }
}
