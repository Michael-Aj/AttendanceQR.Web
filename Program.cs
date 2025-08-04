using AttendanceQR.Web.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string from env (Render -> Env Vars) or fallback
var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
          ?? builder.Configuration.GetConnectionString("DefaultConnection")
          ?? "Data Source=/var/attendance/attendance.db";

// Ensure DB directory exists (works for SQLite file paths)
string dataSource;
try { dataSource = new SqliteConnectionStringBuilder(conn).DataSource; }
catch { dataSource = conn.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim(); }
var dir = Path.GetDirectoryName(Path.GetFullPath(dataSource));
if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(conn)  // keep .EnableSensitiveDataLogging() OFF in prod
);

// Auth (cookie)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", o =>
    {
        o.LoginPath = "/Account/Register";
        o.AccessDeniedPath = "/Account/Register";
        o.SlidingExpiration = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization(o =>
    o.AddPolicy("LecturerOnly", p => p.RequireAuthenticatedUser().RequireRole("Lecturer")));

builder.Services.AddControllersWithViews();

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
app.MapDefaultControllerRoute();

app.MapGet("/healthz", () => Results.Ok("ok")).AllowAnonymous();

app.Run();
