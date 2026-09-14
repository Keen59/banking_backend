using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "RefreshToken",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                UPDATE "RefreshToken"
                SET "FamilyId" = "Id"
                WHERE "FamilyId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "FamilyId",
                table: "RefreshToken",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedByTokenId",
                table: "RefreshToken",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_FamilyId",
                table: "RefreshToken",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_ReplacedByTokenId",
                table: "RefreshToken",
                column: "ReplacedByTokenId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshToken_RefreshToken_ReplacedByTokenId",
                table: "RefreshToken",
                column: "ReplacedByTokenId",
                principalTable: "RefreshToken",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshToken_RefreshToken_ReplacedByTokenId",
                table: "RefreshToken");

            migrationBuilder.DropIndex(
                name: "IX_RefreshToken_FamilyId",
                table: "RefreshToken");

            migrationBuilder.DropIndex(
                name: "IX_RefreshToken_ReplacedByTokenId",
                table: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "ReplacedByTokenId",
                table: "RefreshToken");
        }
    }
}
