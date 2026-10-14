using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiskService.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCustomerProfile : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CustomerProfile",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CifNumber = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                NationalId = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerProfile", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerProfile_NationalId",
            table: "CustomerProfile",
            column: "NationalId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CustomerProfile");
    }
}
