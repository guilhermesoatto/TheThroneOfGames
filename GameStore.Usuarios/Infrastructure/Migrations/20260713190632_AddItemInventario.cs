using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameStore.Usuarios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItemInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItensInventario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    JogoId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeJogo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AdquiridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensInventario", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensInventario_UsuarioId_JogoId",
                table: "ItensInventario",
                columns: new[] { "UsuarioId", "JogoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensInventario");
        }
    }
}
