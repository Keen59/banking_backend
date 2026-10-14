using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerService.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFxClearingAccounts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "LedgerAccount",
            columns: new[] { "Id", "CreatedAt", "Currency", "CustomerId", "Kind", "SourceAccountId", "Status", "UpdatedAt" },
            values: new object[,]
            {
                { new Guid("b1b1b1b1-0001-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", null, 2, null, 1, null },
                { new Guid("b1b1b1b1-0001-4000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EUR", null, 2, null, 1, null }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "LedgerAccount",
            keyColumn: "Id",
            keyValue: new Guid("b1b1b1b1-0001-4000-8000-000000000002"));

        migrationBuilder.DeleteData(
            table: "LedgerAccount",
            keyColumn: "Id",
            keyValue: new Guid("b1b1b1b1-0001-4000-8000-000000000003"));
    }
}
