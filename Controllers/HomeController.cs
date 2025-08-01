using AttendanceQR.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceQR.Web.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index([FromServices] AppDbContext db)
        {
            var latest = await db.ClassSessions
                .OrderByDescending(s => s.ClassDate)
                .ThenByDescending(s => s.StartTime)
                .Select(s => new { s.Id, s.SessionId })
                .FirstOrDefaultAsync();

            ViewBag.LatestSessionId = latest?.Id;
            ViewBag.LatestSessionLabel = latest?.SessionId; // for display
            return View();
        }
    }

}
