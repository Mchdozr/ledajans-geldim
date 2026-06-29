using System;
using Ledajans.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledajans.Server.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260629140000_UserDeviceOnePerUser")]
    /// <inheritdoc />
    public class UserDeviceOnePerUser : Migration
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE UserDevices DROP FOREIGN KEY IF EXISTS FK_UserDevices_AspNetUsers_UserId;
                ALTER TABLE UserDevices DROP INDEX IF EXISTS IX_UserDevices_UserId;
                ALTER TABLE UserDevices ADD INDEX IX_UserDevices_UserId (UserId);
                ALTER TABLE UserDevices ADD CONSTRAINT FK_UserDevices_AspNetUsers_UserId
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE;
                """);
        }
    }
}
