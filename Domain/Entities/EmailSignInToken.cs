namespace AttendanceQR.Web.Domain.Entities
{
    public class EmailSignInToken
    {
        public int Id { get; set; }
        public string StaffEmail { get; set; } = default!;
        public string TokenHash { get; set; } = default!; // SHA256 of raw token
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? UsedAtUtc { get; set; }
        public string Purpose { get; set; } = "login";
    }
}