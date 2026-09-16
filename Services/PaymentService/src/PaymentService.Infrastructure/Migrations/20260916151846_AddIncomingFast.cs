using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomingFast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IncomingFastPayment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountCustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByCustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationIban = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    SourceIban = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingFastPayment", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingFastPayment_AccountCustomerId",
                table: "IncomingFastPayment",
                column: "AccountCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingFastPayment_AccountId",
                table: "IncomingFastPayment",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingFastPayment_IdempotencyKey",
                table: "IncomingFastPayment",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomingFastPayment_Status",
                table: "IncomingFastPayment",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncomingFastPayment");
        }
    }
}
