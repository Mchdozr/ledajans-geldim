using Ledajans.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledajans.Server.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260629150000_RepairUserDeviceUniqueIndex")]
    public class RepairUserDeviceUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE t1 FROM UserDevices t1
                INNER JOIN UserDevices t2 ON t1.UserId = t2.UserId AND t1.Id < t2.Id;
                ALTER TABLE UserDevices DROP INDEX IF EXISTS IX_UserDevices_UserId;
                CREATE UNIQUE INDEX IX_UserDevices_UserId ON UserDevices (UserId);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
