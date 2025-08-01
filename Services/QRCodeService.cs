using QRCoder;
using AttendanceQR.Web.Services.Interfaces;

namespace AttendanceQR.Web.Services
{
    public class QRCodeService : IQRCodeService
    {
        public byte[] GeneratePng(string url, int pixelsPerModule = 10)
        {
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(pixelsPerModule);
        }
    }
}
