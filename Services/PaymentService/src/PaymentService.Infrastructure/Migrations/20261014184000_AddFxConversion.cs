using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFxConversion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FxConversion",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                DestinationAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceAmount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                SourceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                DestinationAmount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                DestinationCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Rate = table.Column<decimal>(type: "numeric(19,8)", precision: 19, scale: 8, nullable: false),
                TryEquivalent = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                RejectReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                SellJournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                BuyJournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FxConversion", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FxConversion_CustomerId",
            table: "FxConversion",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_FxConversion_CustomerId_IdempotencyKey",
            table: "FxConversion",
            columns: new[] { "CustomerId", "IdempotencyKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FxConversion_Status",
            table: "FxConversion",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "FxConversion");
    }
}
