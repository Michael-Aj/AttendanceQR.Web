// ViewModels/SessionRowVm.cs
namespace AttendanceQR.Web.ViewModels
{
    public class SessionRowVm
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = default!;
        public string ModuleCode { get; set; } = default!;
        public string VenueCode { get; set; } = default!;
        public DateOnly ClassDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public int Count { get; set; }
    }
}
