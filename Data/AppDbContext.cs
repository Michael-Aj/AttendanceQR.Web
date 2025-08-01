using AttendanceQR.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AttendanceQR.Web.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Primary keys & uniqueness
            b.Entity<Student>().HasKey(s => s.StudentNumber);
            b.Entity<Module>().HasKey(m => m.ModuleCode);
            b.Entity<Venue>().HasKey(v => v.Code);
            b.Entity<ClassSession>().HasIndex(s => s.SessionId).IsUnique();

            // --- Explicit FK: ClassSession.ModuleCode -> Module.ModuleCode
            b.Entity<ClassSession>()
                .Property(s => s.ModuleCode)
                .IsRequired();

            b.Entity<ClassSession>()
                .HasOne(s => s.Module)
                .WithMany() // or .WithMany(m => m.ClassSessions) if you add a collection
                .HasForeignKey(s => s.ModuleCode)
                .HasPrincipalKey(m => m.ModuleCode)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Explicit FK: ClassSession.VenueCode -> Venue.Code
            b.Entity<ClassSession>()
                .Property(s => s.VenueCode)
                .IsRequired();

            b.Entity<ClassSession>()
                .HasOne(s => s.Venue)
                .WithMany()
                .HasForeignKey(s => s.VenueCode)
                .HasPrincipalKey(v => v.Code)
                .OnDelete(DeleteBehavior.Restrict);

            // AttendanceRecord -> ClassSession
            b.Entity<AttendanceRecord>()
                .HasOne(a => a.ClassSession)
                .WithMany(s => s.AttendanceRecords)
                .HasForeignKey(a => a.ClassSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // AttendanceRecord -> Student
            b.Entity<AttendanceRecord>()
                .HasOne(a => a.Student)
                .WithMany(s => s.AttendanceRecords)
                .HasForeignKey(a => a.StudentNumber)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
    
    }
