using Ledajans.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledajans.Server.Migrations
{
    /// <summary>
    /// Canlıda kırık kalan UserDevice index durumunu güvenli şekilde onarır.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260629150000_RepairUserDeviceUniqueIndex")]
    public class RepairUserDeviceUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE t1 FROM UserDevices t1
                INNER JOIN UserDevices t2 ON t1.UserId = t2.UserId AND t1.Id < t2.Id
                """);

            migrationBuilder.Sql("""
                SET @idx_exists := (
                    SELECT COUNT(1) FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'UserDevices'
                      AND INDEX_NAME = 'IX_UserDevices_UserId'
                );
                SET @idx_non_unique := (
                    SELECT NON_UNIQUE FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'UserDevices'
                      AND INDEX_NAME = 'IX_UserDevices_UserId'
                    LIMIT 1
                );
                SET @sql := CASE
                    WHEN @idx_exists = 0 THEN
                        'CREATE UNIQUE INDEX IX_UserDevices_UserId ON UserDevices (UserId)'
                    WHEN @idx_non_unique = 1 THEN
                        'ALTER TABLE UserDevices DROP INDEX IX_UserDevices_UserId, ADD UNIQUE INDEX IX_UserDevices_UserId (UserId)'
                    ELSE 'SELECT 1'
                END;
                PREPARE repair_stmt FROM @sql;
                EXECUTE repair_stmt;
                DEALLOCATE PREPARE repair_stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
