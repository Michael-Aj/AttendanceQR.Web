using System.ComponentModel.DataAnnotations;

namespace AttendanceQR.Web.ViewModels
{
    public class ModuleCompareVm
    {
        [Required] public string ModuleCode { get; set; } = "";
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        public int RosterCount { get; set; }
        public int PresentCount { get; set; }
        public List<RosterRow> Absentees { get; set; } = new();
    }
}
