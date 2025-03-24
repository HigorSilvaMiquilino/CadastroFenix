using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cadastro.Migrations
{
    /// <inheritdoc />
    public partial class Funcionarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Funcionarios",
                columns: new[] { "Id", "CPF", "NomeCompleto", "motivo" },
                values: new object[,]
                {
                    { -10, "10919399002", "João Rocha", "Funcionário" },
                    { -9, "41078925062", "Inês Oliveira", "Funcionário" },
                    { -8, "79733835064", "Hugo Mendes", "Funcionário" },
                    { -7, "71483624072", "Gisele Almeida", "Funcionário" },
                    { -6, "74916864000", "Fábio Lima", "Funcionário" },
                    { -5, "69710540084", "Elena Santos", "Funcionário" },
                    { -4, "02997489016", "Diego Costa", "Funcionário" },
                    { -3, "37452415094", "Carla Souza", "Funcionário" },
                    { -2, "76698650080", "Bruno Silva", "Funcionário" },
                    { -1, "11400435013", "Ana Pereira", "Funcionário" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -10);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -9);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -8);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -7);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -6);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -5);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -4);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -3);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -2);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: -1);
        }
    }
}
