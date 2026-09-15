using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a1a1a1a1-0002-4000-8000-000000000001"), "kyc:review", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "KYC inceleme ve onay", null },
                    { new Guid("a1a1a1a1-0002-4000-8000-000000000002"), "customers:read", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tüm müşteri kayıtlarını görüntüleme", null },
                    { new Guid("a1a1a1a1-0002-4000-8000-000000000003"), "roles:assign", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Rol atama", null }
                });

            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a1a1a1a1-0001-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bireysel müşteri", "Customer", null },
                    { new Guid("a1a1a1a1-0001-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "KYC ve müşteri operasyonu", "Operations", null }
                });

            migrationBuilder.InsertData(
                table: "RolePermission",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("a1a1a1a1-0002-4000-8000-000000000001"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") },
                    { new Guid("a1a1a1a1-0002-4000-8000-000000000002"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") },
                    { new Guid("a1a1a1a1-0002-4000-8000-000000000003"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0001-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("a1a1a1a1-0002-4000-8000-000000000001"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("a1a1a1a1-0002-4000-8000-000000000002"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") });

            migrationBuilder.DeleteData(
                table: "RolePermission",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("a1a1a1a1-0002-4000-8000-000000000003"), new Guid("a1a1a1a1-0001-4000-8000-000000000002") });

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0002-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0002-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0002-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-0001-4000-8000-000000000002"));
        }
    }
}
