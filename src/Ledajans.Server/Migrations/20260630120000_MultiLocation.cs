using System;
using Ledajans.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledajans.Server.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260630120000_MultiLocation")]
    public partial class MultiLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Code",
                table: "Locations",
                column: "Code",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO Locations (Name, Code, SortOrder, IsActive) VALUES
                ('Ledajans-Ofis', 'ofis', 1, 1),
                ('Ledajans-Fabrika', 'fabrika', 2, 1);
                """);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Geofences",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE Geofences SET LocationId = 1 WHERE LocationId IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "Geofences",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Geofences_LocationId",
                table: "Geofences",
                column: "LocationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Geofences_Locations_LocationId",
                table: "Geofences",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LocationId",
                table: "AspNetUsers",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Locations_LocationId",
                table: "AspNetUsers",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE AspNetUsers u
                INNER JOIN AspNetUserRoles ur ON ur.UserId = u.Id
                INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
                SET u.LocationId = 1
                WHERE r.Name = 'Employee' AND u.LocationId IS NULL;
                """);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE AttendanceRecords SET LocationId = 1 WHERE LocationId IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "AttendanceRecords",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_LocationId",
                table: "AttendanceRecords",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Locations_LocationId",
                table: "AttendanceRecords",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "NonWorkingDays",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE NonWorkingDays SET LocationId = 1 WHERE LocationId IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "NonWorkingDays",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_NonWorkingDays_Date_UserId",
                table: "NonWorkingDays");

            migrationBuilder.CreateIndex(
                name: "IX_NonWorkingDays_Date_UserId_LocationId",
                table: "NonWorkingDays",
                columns: new[] { "Date", "UserId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NonWorkingDays_LocationId",
                table: "NonWorkingDays",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_NonWorkingDays_Locations_LocationId",
                table: "NonWorkingDays",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "CompanySettings",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE CompanySettings SET LocationId = 1 WHERE LocationId IS NULL;");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CompanySettings",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "CompanySettings");

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "CompanySettings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CompanySettings",
                table: "CompanySettings",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanySettings_Locations_LocationId",
                table: "CompanySettings",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("""
                INSERT INTO CompanySettings (LocationId, LateCheckInHour, LateCheckInMinute)
                SELECT 2, LateCheckInHour, LateCheckInMinute FROM CompanySettings WHERE LocationId = 1;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Multi-location migration geri alınamaz.");
        }
    }
}
