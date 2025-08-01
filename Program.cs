using AttendanceQR.Web.Data;
using AttendanceQR.Web.Domain.Entities;
using AttendanceQR.Web.Services;
using AttendanceQR.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dbFile = conn.Replace("Data Source=", "").Trim();
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbFile))!);

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(conn));

//// SQLite by default
//var dbPath = Environment.GetEnvironmentVariable("DB_PATH")
//           ?? Path.Combine(builder.Environment.ContentRootPath, "data", "attendance.db");
//Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

//builder.Services.AddDbContext<AppDbContext>(opt =>
//    opt.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddControllersWithViews();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});


// DI
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ISessionService, SessionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!await db.Modules.AnyAsync())
    {
        db.Modules.AddRange(
            new Module { ModuleCode = "ISP152", Name = "Info Security" },
            new Module { ModuleCode = "IDB152", Name = "Intro to Databases" }
        );
    }
    if (!await db.Venues.AnyAsync())
    {
        db.Venues.AddRange(
            new Venue { Code = "CEN-101", Name = "Centurion 101" },
            new Venue { Code = "LAB-01", Name = "Main Lab" }
        );
    }
    await db.SaveChangesAsync();
}

// Migrate at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
