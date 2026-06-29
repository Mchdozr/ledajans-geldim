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
                INNER JOIN UserDevices t2 ON t1.UserId = t2.UserId AND t1.Id < t2.Id
                """);

            migrationBuilder.DropIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices",
                column: "UserId");
        }
    }
}
