using Ledajans.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledajans.Server.Migrations
{
    /// <summary>
    /// 40000 ile aynı onarım — kısmen uygulanmış ortamlar için idempotent.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260629150000_RepairUserDeviceUniqueIndex")]
    public class RepairUserDeviceUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE t1 FROM UserDevices t1
                INNER JOIN UserDevices t2 ON t1.UserId = t2.UserId AND t1.Id < t2.Id;
                ALTER TABLE UserDevices DROP FOREIGN KEY IF EXISTS FK_UserDevices_AspNetUsers_UserId;
                ALTER TABLE UserDevices DROP INDEX IF EXISTS IX_UserDevices_UserId;
                ALTER TABLE UserDevices ADD UNIQUE INDEX IX_UserDevices_UserId (UserId);
                ALTER TABLE UserDevices ADD CONSTRAINT FK_UserDevices_AspNetUsers_UserId
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
