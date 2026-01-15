using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameStore.Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jogos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Genero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Desenvolvedora = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataLancamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Disponivel = table.Column<bool>(type: "boolean", nullable: false),
                    ImagemUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Estoque = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jogos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_Disponivel",
                table: "Jogos",
                column: "Disponivel");

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_Genero",
                table: "Jogos",
                column: "Genero");

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_Nome",
                table: "Jogos",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_Preco",
                table: "Jogos",
                column: "Preco");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jogos");
        }
    }
}
