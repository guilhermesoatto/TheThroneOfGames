using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheThroneOfGames.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ActiveToken",
                table: "Usuario",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Purchase",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Purchase",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Purchase",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Purchase",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Purchase",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "Purchase",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Promotion",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GameIds",
                table: "Promotion",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Promotion",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "GameEntity",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "GameEntity",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "GameEntity",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "GameEntity",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Purchase");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Purchase");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Purchase");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Purchase");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Purchase");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "Purchase");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Promotion");

            migrationBuilder.DropColumn(
                name: "GameIds",
                table: "Promotion");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Promotion");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "GameEntity");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "GameEntity");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "GameEntity");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "GameEntity");

            migrationBuilder.AlterColumn<string>(
                name: "ActiveToken",
                table: "Usuario",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
