using System;
using Ledajans.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledajans.Server.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260630140000_Departments")]
    public partial class Departments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_LocationId_Name",
                table: "Departments",
                columns: new[] { "LocationId", "Name" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO Departments (LocationId, Name, SortOrder)
                SELECT l.Id, 'Teknik', 1 FROM Locations l
                UNION ALL SELECT l.Id, 'Depo', 2 FROM Locations l
                UNION ALL SELECT l.Id, 'Satış', 3 FROM Locations l
                UNION ALL SELECT l.Id, 'Muhasebe', 4 FROM Locations l;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Departments");
        }
    }
}
