using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheThroneOfGames.Infrastructure.Migrations
{
    public partial class UpdateMappingsAndSeedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename tables
            migrationBuilder.RenameTable(name: "Users", newName: "Usuario");
            migrationBuilder.RenameTable(name: "Games", newName: "GameEntity");
            migrationBuilder.RenameTable(name: "Promotions", newName: "Promotion");
            migrationBuilder.RenameTable(name: "Purchases", newName: "Purchase");

            // Add column size constraints and setup foreign keys
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Usuario",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "GameEntity",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "Discount",
                table: "Promotion",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Purchase_GameId",
                table: "Purchase",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchase_UserId",
                table: "Purchase",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchase_GameEntity_GameId",
                table: "Purchase",
                column: "GameId",
                principalTable: "GameEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchase_Usuario_UserId",
                table: "Purchase",
                column: "UserId",
                principalTable: "Usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Seed Admin User
            migrationBuilder.InsertData(
                table: "Usuario",
                columns: new[] { "Id", "Name", "Email", "PasswordHash", "Role", "IsActive", "ActiveToken" },
                values: new object[,] { 
                    { 
                        Guid.NewGuid(), 
                        "Admin Primeiro", 
                        "admin1@throne.com",
                        BCrypt.Net.BCrypt.HashPassword("Admin@123"), // Senha forte padrão
                        "Admin",
                        true,
                        "" // Usuário já ativo
                    },
                    {
                        Guid.NewGuid(),
                        "Fulano Silva",
                        "fulano@email.com",
                        BCrypt.Net.BCrypt.HashPassword("User@123"), // Senha forte padrão
                        "User",
                        true,
                        ""
                    },
                    {
                        Guid.NewGuid(),
                        "Beltrano Santos",
                        "beltrano@email.com",
                        BCrypt.Net.BCrypt.HashPassword("User@123"), // Senha forte padrão
                        "User",
                        true,
                        ""
                    },
                    {
                        Guid.NewGuid(),
                        "Ciclano Oliveira",
                        "ciclano@email.com",
                        BCrypt.Net.BCrypt.HashPassword("User@123"), // Senha forte padrão
                        "User",
                        true,
                        ""
                    }
                });

            // Seed Games
            migrationBuilder.InsertData(
                table: "GameEntity",
                columns: new[] { "Id", "Name", "Genre", "Price" },
                values: new object[,] {
                    { Guid.NewGuid(), "Counter-Strike 2", "FPS", 0.0M }, // Free to play
                    { Guid.NewGuid(), "Baldur's Gate 3", "RPG", 299.90M },
                    { Guid.NewGuid(), "EA FC 24", "Sports", 299.90M },
                    { Guid.NewGuid(), "Red Dead Redemption 2", "Action-Adventure", 299.90M },
                    { Guid.NewGuid(), "Cyberpunk 2077", "RPG", 199.90M },
                    { Guid.NewGuid(), "The Witcher 3: Wild Hunt", "RPG", 79.90M },
                    { Guid.NewGuid(), "Grand Theft Auto V", "Action-Adventure", 82.41M },
                    { Guid.NewGuid(), "Elden Ring", "Action RPG", 249.90M },
                    { Guid.NewGuid(), "Resident Evil 4", "Survival Horror", 249.00M },
                    { Guid.NewGuid(), "Starfield", "RPG", 299.00M }
                });

            // Seed Promotions
            migrationBuilder.InsertData(
                table: "Promotion",
                columns: new[] { "Id", "Discount", "ValidUntil" },
                values: new object[,] {
                    { Guid.NewGuid(), 0.2M, DateTime.Now.AddMonths(1) }, // 20% off
                    { Guid.NewGuid(), 0.3M, DateTime.Now.AddMonths(1) }, // 30% off
                    { Guid.NewGuid(), 0.5M, DateTime.Now.AddDays(7) }    // 50% off
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys first
            migrationBuilder.DropForeignKey(
                name: "FK_Purchase_GameEntity_GameId",
                table: "Purchase");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchase_Usuario_UserId",
                table: "Purchase");

            migrationBuilder.DropIndex(
                name: "IX_Purchase_GameId",
                table: "Purchase");

            migrationBuilder.DropIndex(
                name: "IX_Purchase_UserId",
                table: "Purchase");

            // Remove seed data
            migrationBuilder.DeleteData("Promotion", "Id", "");
            migrationBuilder.DeleteData("GameEntity", "Id", "");
            migrationBuilder.DeleteData("Usuario", "Id", "");

            // Restore original table names
            migrationBuilder.RenameTable(name: "Usuario", newName: "Users");
            migrationBuilder.RenameTable(name: "GameEntity", newName: "Games");
            migrationBuilder.RenameTable(name: "Promotion", newName: "Promotions");
            migrationBuilder.RenameTable(name: "Purchase", newName: "Purchases");

            // Restore original column definitions
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Games",
                type: "decimal(18,2)",
                nullable: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "Discount",
                table: "Promotions",
                type: "decimal(18,2)",
                nullable: false);
        }
    }
}