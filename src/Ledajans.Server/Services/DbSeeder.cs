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
            db.Geofences.Add(new Geofence
            {
                Name = "Ledajans Ofis",
                Latitude = double.TryParse(config["Seed:DefaultLat"], NumberStyles.Float, inv, out var lat) ? lat : 41.0082,
                Longitude = double.TryParse(config["Seed:DefaultLng"], NumberStyles.Float, inv, out var lng) ? lng : 28.9784,
                RadiusMeters = int.TryParse(config["Seed:DefaultRadius"], NumberStyles.Integer, inv, out var r) ? r : 150,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
    }
}
