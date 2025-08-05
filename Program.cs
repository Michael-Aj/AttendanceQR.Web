using AttendanceQR.Web.Data;
using AttendanceQR.Web.Services;
using AttendanceQR.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//// Connection string from env (Render -> Env Vars) or fallback
//var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
//          ?? builder.Configuration.GetConnectionString("DefaultConnection")
//          ?? "Data Source=/var/data/attendance.db";

//// Ensure DB directory exists (works for SQLite file paths)
//string dataSource;
//try { dataSource = new SqliteConnectionStringBuilder(conn).DataSource; }
//catch { dataSource = conn.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim(); }
//var dir = Path.GetDirectoryName(Path.GetFullPath(dataSource));
//if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

//builder.Services.AddDbContext<AppDbContext>(o =>
//    o.UseSqlite(conn)  // keep .EnableSensitiveDataLogging() OFF in prod
//);
var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
          ?? builder.Configuration.GetConnectionString("DefaultConnection")
          ?? "Data Source=./data/attendance.db";

var csb = new SqliteConnectionStringBuilder(conn);
var dataSource = Path.GetFullPath(csb.DataSource);
var dir = Path.GetDirectoryName(dataSource)!;

// Only try to create if it's under a known-writable location
if (!Directory.Exists(dir))
{
    if (dir.StartsWith("/var/attendance") || !Path.IsPathRooted(dir))
        Directory.CreateDirectory(dir);
    else
        throw new InvalidOperationException(
            $"DB dir '{dir}' is not writable. Use /var/attendance on Render.");
}

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(conn));

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});


// DI
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ISessionService, SessionService>();

//// Auth (cookie)
//builder.Services.AddAuthentication("Cookies")
//    .AddCookie("Cookies", o =>
//    {
//        o.LoginPath = "/Account/Register";
//        o.AccessDeniedPath = "/Account/Register";
//        o.SlidingExpiration = true;
//        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//        o.Cookie.SameSite = SameSiteMode.Lax;
//    });
//builder.Services.AddAuthorization(o =>
//    o.AddPolicy("LecturerOnly", p => p.RequireAuthenticatedUser().RequireRole("Lecturer")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Register";
        o.AccessDeniedPath = "/Account/Register";
        o.SlidingExpiration = true;
    });
//builder.Services.AddAuthorization();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("LecturerOnly",
        p => p.RequireAuthenticatedUser().RequireRole("Lecturer"));
});

//builder.Services.AddControllersWithViews(options =>
//{
//    // Require LecturerOnly everywhere by default
//    options.Filters.Add(new AuthorizeFilter("LecturerOnly"));
//});

builder.Services.AddControllersWithViews();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();     // email sender
builder.Services.AddScoped<IRegistrationService, RegistrationService>(); // magic-link service

var app = builder.Build();

// Respect proxy headers from Render (TLS is terminated at the proxy)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Migrate DB on startup (safe for SQLite)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    // optional: seed minimal master data here
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Enforce auth globally (optional) — or use [Authorize] on controllers
// app.MapControllers().RequireAuthorization("LecturerOnly");
//app.MapControllers().RequireAuthorization("LecturerOnly");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/healthz", () => Results.Ok("ok")).AllowAnonymous();

app.Run();
