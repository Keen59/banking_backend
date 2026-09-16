using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsCreditPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "UpdatedAt" },
                values: new object[] { new Guid("a1a1a1a1-0002-4000-8000-000000000004"), "payments:credit", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Test kredisi (clearing debit, müşteri credit)", null });

            migrationBuilder.InsertData(
                table: "RolePermission",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { new Guid("a1a1a1a1-0002-4000-8000-000000000004"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("a1a1a1a1-0002-4000-8000-000000000004"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") });

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0002-4000-8000-000000000004"));
        }
    }
}
