using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsSettlePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "UpdatedAt" },
                values: new object[] { new Guid("a1a1a1a1-0002-4000-8000-000000000005"), "payments:settle", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EFT takas / iade (sahte clearing)", null });

            migrationBuilder.InsertData(
                table: "RolePermission",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { new Guid("a1a1a1a1-0002-4000-8000-000000000005"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("a1a1a1a1-0002-4000-8000-000000000005"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") });

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0002-4000-8000-000000000005"));
        }
    }
}
