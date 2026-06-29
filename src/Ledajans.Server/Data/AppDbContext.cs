using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Geofence> Geofences => Set<Geofence>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<NonWorkingDay> NonWorkingDays => Set<NonWorkingDay>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasIndex(a => new { a.UserId, a.LocalDate }).IsUnique();
            e.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NonWorkingDay>(e =>
        {
            e.HasIndex(n => new { n.Date, n.UserId });
            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserDevice>(e =>
        {
            e.HasIndex(d => d.DeviceId).IsUnique();
            e.HasIndex(d => d.UserId);
            e.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
