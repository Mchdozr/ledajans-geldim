using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Geofence> Geofences => Set<Geofence>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<NonWorkingDay> NonWorkingDays => Set<NonWorkingDay>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Location>(e =>
        {
            e.HasIndex(l => l.Code).IsUnique();
        });

        builder.Entity<Geofence>(e =>
        {
            e.HasIndex(g => g.LocationId).IsUnique();
            e.HasOne(g => g.Location)
                .WithMany()
                .HasForeignKey(g => g.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.HasOne(u => u.Location)
                .WithMany()
                .HasForeignKey(u => u.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CompanySettings>(e =>
        {
            e.HasKey(s => s.LocationId);
            e.HasOne(s => s.Location)
                .WithMany()
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasIndex(a => new { a.UserId, a.LocalDate }).IsUnique();
            e.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Location)
                .WithMany()
                .HasForeignKey(a => a.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<NonWorkingDay>(e =>
        {
            e.HasIndex(n => new { n.Date, n.UserId, n.LocationId });
            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.Location)
                .WithMany()
                .HasForeignKey(n => n.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserDevice>(e =>
        {
            e.HasIndex(d => d.DeviceId).IsUnique();
            e.HasIndex(d => d.UserId).IsUnique();
            e.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Department>(e =>
        {
            e.HasIndex(d => new { d.LocationId, d.Name }).IsUnique();
            e.HasOne(d => d.Location)
                .WithMany()
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
