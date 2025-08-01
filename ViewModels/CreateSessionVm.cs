using System.ComponentModel.DataAnnotations;

namespace AttendanceQR.Web.ViewModels
{
    public class CreateSessionVm
    {
        [Required] public string ModuleCode { get; set; } = default!;
        [Required] public string VenueCode { get; set; } = default!;
        [Required] public DateOnly ClassDate { get; set; }
        [Required] public TimeOnly StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
    }
}
