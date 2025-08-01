using AttendanceQR.Web.Data;
using AttendanceQR.Web.Services.Interfaces;
using AttendanceQR.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;
using AttendanceQR.Web.Services;

namespace AttendanceQR.Web.Controllers
{
    //[Authorize] // add auth later; for MVP you can remove this line
    public class SessionsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ISessionService _sessionService;
        private readonly IQRCodeService _qr;

        public SessionsController(AppDbContext db, ISessionService sessionService, IQRCodeService qr)
        {
            _db = db; _sessionService = sessionService; _qr = qr;
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateSessionVm());

        [ValidateAntiForgeryToken, HttpPost]
        public async Task<IActionResult> Create(CreateSessionVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var session = await _sessionService.CreateAsync(vm, ct);
            await _sessionService.IssueNonceAsync(session.Id, TimeSpan.FromMinutes(20), ct);

            var url = Url.Action("Index", "Capture",
                new { session_id = session.SessionId, module_code = session.ModuleCode, venue = session.VenueCode, nonce = session.Nonce },
                protocol: Request.Scheme)!;

            session.QrUrl = url;
            await _db.SaveChangesAsync(ct);

            var png = _qr.GeneratePng(url);
            return File(png, "image/png", $"{session.SessionId}.png");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var s = await _db.ClassSessions
                .Include(x => x.AttendanceRecords)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s == null) return NotFound();
            return View(s);
        }

        // GET: /Sessions/Export/5
        [HttpGet]
        public async Task<IActionResult> Export(int id, CancellationToken ct)
        {
            var session = await _db.ClassSessions
                .Include(s => s.AttendanceRecords)
                .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(s => s.Id == id, ct);

            if (session is null) return NotFound();

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Attendance");

            // Header
            var headers = new[] {
        "SessionId","ModuleCode","Venue","ClassDate","StartTime",
        "StudentNumber","FirstName","LastName","Programme","Status","CapturedAtUtc"
    };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // Rows
            int r = 2;
            foreach (var a in session.AttendanceRecords.OrderBy(x => x.StudentNumber))
            {
                ws.Cell(r, 1).Value = session.SessionId;
                ws.Cell(r, 2).Value = session.ModuleCode;
                ws.Cell(r, 3).Value = session.VenueCode;

                // DateOnly/TimeOnly -> Excel
                ws.Cell(r, 4).Value = session.ClassDate.ToDateTime(TimeOnly.MinValue);
                ws.Cell(r, 4).Style.DateFormat.Format = "yyyy-mm-dd";

                ws.Cell(r, 5).Value = session.StartTime.ToTimeSpan();
                ws.Cell(r, 5).Style.NumberFormat.Format = "hh:mm";

                ws.Cell(r, 6).Value = a.StudentNumber;
                ws.Cell(r, 7).Value = a.Student?.FirstName;
                ws.Cell(r, 8).Value = a.Student?.LastName;
                ws.Cell(r, 9).Value = a.Student?.Programme;
                ws.Cell(r, 10).Value = a.Status.ToString();

                ws.Cell(r, 11).Value = a.CapturedAtUtc;
                ws.Cell(r, 11).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                r++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var bytes = stream.ToArray();

            var fileName = $"{session.SessionId}_attendance.xlsx";
            const string contentType =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(bytes, contentType, fileName);
        }

        [HttpGet]
        [AllowAnonymous] // or [Authorize(Roles="Admin,Lecturer")] if you want it protected
        public IActionResult DownloadTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Template");

            var cols = new[] {
            "session_id","student_number","first_name","last_name",
            "programme","module_code","venue","class_date","start_time","status"
        };
            for (int i = 0; i < cols.Length; i++)
                ws.Cell(1, i + 1).Value = cols[i];

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var bytes = stream.ToArray();
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, "attendance_template.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var sessions = await _db.ClassSessions
                .Include(s => s.AttendanceRecords)
                .OrderByDescending(s => s.ClassDate)
                .ThenByDescending(s => s.StartTime)
                .Select(s => new SessionRowVm
                {
                    Id = s.Id,
                    SessionId = s.SessionId,
                    ModuleCode = s.ModuleCode,
                    VenueCode = s.VenueCode,
                    ClassDate = s.ClassDate,
                    StartTime = s.StartTime,
                    Count = s.AttendanceRecords.Count
                })
                .ToListAsync(ct);

            return View(sessions);
        }

        // GET: /Sessions/CompareModule
        [HttpGet]
        public IActionResult CompareModule() => View(new ModuleCompareVm());

        // POST: /Sessions/CompareModule
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(50_000_000)] // 50 MB server-side cap
        public async Task<IActionResult> CompareModule(ModuleCompareVm vm, IFormFile rosterFile, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            if (rosterFile is null || rosterFile.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a .xlsx or .csv file.");
                return View(vm);
            }

            var ext = Path.GetExtension(rosterFile.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".csv")
            {
                ModelState.AddModelError("", "Unsupported file type. Upload .xlsx or .csv.");
                return View(vm);
            }

            if (rosterFile.Length > 50 * 1024 * 1024)
            {
                ModelState.AddModelError("", "File is too large (limit 50 MB).");
                return View(vm);
            }

            List<RosterRow> roster;
            try
            {
                using var ms = new MemoryStream((int)rosterFile.Length);
                await rosterFile.CopyToAsync(ms, ct);
                ms.Position = 0;
                roster = RosterReader.ReadRoster(ms, rosterFile.FileName); // can throw
            }
            catch (Exception ex)
            {
                // Optional: _logger.LogError(ex, "Roster upload failed");
                ModelState.AddModelError("", $"Could not read the roster: {ex.Message}");
                return View(vm);
            }

            var module = vm.ModuleCode.Trim().ToUpperInvariant();

            // 2) Build present set across all sessions for this module (date range optional)
            var sessionsQ = _db.ClassSessions.Where(s => s.ModuleCode == module);
            if (vm.FromDate.HasValue) sessionsQ = sessionsQ.Where(s => s.ClassDate >= vm.FromDate.Value);
            if (vm.ToDate.HasValue) sessionsQ = sessionsQ.Where(s => s.ClassDate <= vm.ToDate.Value);

            var sessionIds = await sessionsQ.Select(s => s.Id).ToListAsync(ct);

            var present = await _db.AttendanceRecords
                .Where(a => sessionIds.Contains(a.ClassSessionId))
                .Select(a => a.StudentNumber)
                .Distinct()
                .ToListAsync(ct);

            var presentSet = present
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim().ToUpperInvariant())
                .ToHashSet();

            // 3) Absentees
            var absentees = roster
                .Where(r => !presentSet.Contains((r.StudentNumber ?? "").Trim().ToUpperInvariant()))
                .OrderBy(r => r.StudentNumber)
                .ToList();

            vm.Absentees = absentees;
            vm.RosterCount = roster.Count;
            vm.PresentCount = presentSet.Count;

            return View("CompareModuleResults", vm);
        }

        // POST: /Sessions/ExportModuleAbsentees
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> ExportModuleAbsentees(string moduleCode, DateOnly? fromDate, DateOnly? toDate, IFormFile rosterFile, CancellationToken ct)
        {
            if (rosterFile is null || rosterFile.Length == 0) return BadRequest("Upload roster.");

            // read roster
            List<RosterRow> roster;
            using (var ms = new MemoryStream())
            {
                await rosterFile.CopyToAsync(ms, ct);
                ms.Position = 0;
                roster = RosterReader.ReadRoster(ms, rosterFile.FileName);
            }

            var module = (moduleCode ?? "").Trim().ToUpperInvariant();

            var sessionsQ = _db.ClassSessions.Where(s => s.ModuleCode == module);
            if (fromDate.HasValue) sessionsQ = sessionsQ.Where(s => s.ClassDate >= fromDate.Value);
            if (toDate.HasValue) sessionsQ = sessionsQ.Where(s => s.ClassDate <= toDate.Value);

            var sessionIds = await sessionsQ.Select(s => s.Id).ToListAsync(ct);

            var present = await _db.AttendanceRecords
                .Where(a => sessionIds.Contains(a.ClassSessionId))
                .Select(a => a.StudentNumber)
                .Distinct()
                .ToListAsync(ct);

            var presentSet = present
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim().ToUpperInvariant())
                .ToHashSet();

            var absentees = roster
                .Where(r => !presentSet.Contains((r.StudentNumber ?? "").Trim().ToUpperInvariant()))
                .OrderBy(r => r.StudentNumber)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Absentees");
            var headers = new[] { "StudentNumber", "FirstName", "LastName", "Programme", "ModuleCode", "FromDate", "ToDate" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

            int row = 2;
            foreach (var a in absentees)
            {
                ws.Cell(row, 1).Value = a.StudentNumber;
                ws.Cell(row, 2).Value = a.FirstName;
                ws.Cell(row, 3).Value = a.LastName;
                ws.Cell(row, 4).Value = a.Programme;
                ws.Cell(row, 5).Value = module;
                ws.Cell(row, 6).Value = fromDate?.ToDateTime(TimeOnly.MinValue);
                ws.Cell(row, 6).Style.DateFormat.Format = "yyyy-mm-dd";
                ws.Cell(row, 7).Value = toDate?.ToDateTime(TimeOnly.MinValue);
                ws.Cell(row, 7).Style.DateFormat.Format = "yyyy-mm-dd";
                row++;
            }
            ws.Columns().AdjustToContents();

            using var outStream = new MemoryStream();
            wb.SaveAs(outStream);
            var bytes = outStream.ToArray();
            var name = $"Absentees_{module}_{(fromDate?.ToString("yyyyMMdd") ?? "ALL")}_{(toDate?.ToString("yyyyMMdd") ?? "ALL")}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
        }
    
}


}

