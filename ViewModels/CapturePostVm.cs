using System.ComponentModel.DataAnnotations;

namespace AttendanceQR.Web.ViewModels
{
    public class CapturePostVm
    {
        [Required] public string SessionId { get; set; } = default!;
        [Required] public string StudentNumber { get; set; } = default!;
        [Required] public string FirstName { get; set; } = default!;
        [Required] public string LastName { get; set; } = default!;
        [Required] public string Programme { get; set; } = default!;
        [Required] public string ModuleCode { get; set; } = default!;
        [Required] public string Venue { get; set; } = default!;
        [Required] public DateOnly ClassDate { get; set; }
        [Required] public TimeOnly StartTime { get; set; }
        public string? Nonce { get; set; }
    }
}
