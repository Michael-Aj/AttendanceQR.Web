using AttendanceQR.Web.Services.Interfaces;
using AttendanceQR.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceQR.Web.Controllers
{
    public class CaptureController : Controller
    {
        private readonly IAttendanceService _attendance;
        private readonly ISessionService _sessions;

        public CaptureController(IAttendanceService attendance, ISessionService sessions)
        { _attendance = attendance; _sessions = sessions; }

        // GET /Capture?session_id=...&module_code=...&venue=...&nonce=...
        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Index(string session_id, string module_code, string venue, string nonce, CancellationToken ct)
        {
            var s = await _sessions.FindBySessionIdAsync(session_id, ct);
            if (s == null || s.Nonce != nonce || s.NonceExpiresAtUtc < DateTime.UtcNow)
                return BadRequest("This QR code has expired. Please request a fresh code.");

            var vm = new CaptureVm {
                SessionId = session_id,
                ModuleCode = module_code,
                Venue = venue,
                ClassDate = s.ClassDate,
                StartTime = s.StartTime,
                Nonce = nonce
            };
            return View("Capture", vm);
        }

        [ValidateAntiForgeryToken, HttpPost]
        public async Task<IActionResult> Submit(CapturePostVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View("Capture", vm);

            var id = await _attendance.CaptureAsync(vm, HttpContext, ct);
            if (id == 0)
            {
                ModelState.AddModelError("", "Already captured or invalid QR.");
                return View("Capture", vm);
            }
            return RedirectToAction("Thanks");
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public IActionResult Thanks() => View();
    }
}
