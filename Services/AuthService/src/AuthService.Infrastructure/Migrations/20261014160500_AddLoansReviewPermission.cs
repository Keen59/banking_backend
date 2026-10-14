using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoansReviewPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "Permission" ("Id", "Code", "CreatedAt", "Description", "UpdatedAt")
                VALUES ('a1a1a1a1-0002-4000-8000-000000000007', 'loans:review', TIMESTAMPTZ '2026-01-01 00:00:00+00', 'Kredi başvurusu onay / red', NULL);

                INSERT INTO "RolePermission" ("PermissionId", "RoleId")
                VALUES ('a1a1a1a1-0002-4000-8000-000000000007', 'a1a1a1a1-0001-4000-8000-000000000002');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "RolePermission"
                WHERE "PermissionId" = 'a1a1a1a1-0002-4000-8000-000000000007'
                  AND "RoleId" = 'a1a1a1a1-0001-4000-8000-000000000002';

                DELETE FROM "Permission"
                WHERE "Id" = 'a1a1a1a1-0002-4000-8000-000000000007';
                """);
        }
    }
}
