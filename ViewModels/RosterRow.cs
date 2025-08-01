namespace AttendanceQR.Web.ViewModels
{
    public class RosterRow
    {
        public string StudentNumber { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Programme { get; set; } = "";   // keep for reporting
        public string ModuleCode { get; set; } = "";   // optional: map if present
    }
}
