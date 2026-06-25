using System.Globalization;
using Ledajans.Server.Data;
using Ledajans.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ledajans.Server.Services;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();
        var pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            throw new InvalidOperationException(
                "Veritabani migration tamamlanamadi: " + string.Join(", ", pending));
        }

        foreach (var role in new[] { Roles.Admin, Roles.Employee })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminUser = config["Seed:AdminUserName"] ?? "admin";
        var adminPass = config["Seed:AdminPassword"] ?? "Admin123!";
        var adminFullName = config["Seed:AdminFullName"] ?? "Yönetici";

        if (await userManager.FindByNameAsync(adminUser) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminUser,
                Email = config["Seed:AdminEmail"] ?? "admin@ledajans.local",
                EmailConfirmed = true,
                FullName = adminFullName,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, adminPass);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        if (!await db.Geofences.AnyAsync())
        {
            var inv = CultureInfo.InvariantCulture;
            var lat = double.TryParse(config["Seed:DefaultLat"], NumberStyles.Float, inv, out var parsedLat)
                ? parsedLat : OfficeDefaults.Latitude;
            var lng = double.TryParse(config["Seed:DefaultLng"], NumberStyles.Float, inv, out var parsedLng)
                ? parsedLng : OfficeDefaults.Longitude;
            var radius = int.TryParse(config["Seed:DefaultRadius"], NumberStyles.Integer, inv, out var r)
                ? r : OfficeDefaults.RadiusMeters;

            db.Geofences.Add(new Geofence
            {
                Name = OfficeDefaults.Name,
                Latitude = lat,
                Longitude = lng,
                RadiusMeters = radius,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
        else
        {
            var geofence = await db.Geofences.OrderBy(g => g.Id).FirstAsync();
            const double oldLat = 41.0082;
            const double oldLng = 28.9784;
            if (Math.Abs(geofence.Latitude - oldLat) < 0.0001 && Math.Abs(geofence.Longitude - oldLng) < 0.0001)
            {
                geofence.Latitude = OfficeDefaults.Latitude;
                geofence.Longitude = OfficeDefaults.Longitude;
                await db.SaveChangesAsync();
            }
        }
    }
}
