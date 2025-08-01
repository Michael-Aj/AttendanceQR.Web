namespace AttendanceQR.Web.Services.Interfaces
{
    public interface IQRCodeService
    {
        byte[] GeneratePng(string url, int pixelsPerModule = 10);
    }
}
