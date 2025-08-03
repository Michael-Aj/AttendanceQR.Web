using AttendanceQR.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceQR.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRegistrationService _reg;
        private readonly ILogger<AccountController> _log;
        
        public AccountController(IRegistrationService reg, ILogger<AccountController> log)
        { _reg = reg; _log = log; }

        [HttpGet, AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string staffEmail, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(staffEmail))
            {
                TempData["Error"] = "Enter your staff email address.";
                return View();
            }

            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                await _reg.StartRegistrationAsync(staffEmail, baseUrl, ct);
                return RedirectToAction(nameof(EmailSent), new { email = staffEmail });
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Registration start failed");
                TempData["Error"] = ex.Message;
                return View();
            }
        }

        [HttpGet, AllowAnonymous]
        public IActionResult EmailSent(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Verify(string email, string token, CancellationToken ct)
        {
            var principal = await _reg.CompleteAsync(email, token, ct);
            if (principal == null)
            {
                TempData["Error"] = "Link invalid or expired. Please register again.";
                return RedirectToAction(nameof(Register));
            }
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            TempData["Success"] = "Signed in successfully.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Register));
        }
    }
}
