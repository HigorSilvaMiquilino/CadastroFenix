using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cadastro.Migrations
{
    /// <inheritdoc />
    public partial class AddLogPerformanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "LogsPerformance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TempoExecucaoSegundos = table.Column<double>(type: "float", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsPerformance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsPerformance_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Funcionarios",
                columns: new[] { "Id", "CPF", "NomeCompleto", "motivo" },
                values: new object[,]
                {
                    { 1, "11400435013", "Ana Pereira", "Funcionário" },
                    { 2, "76698650080", "Bruno Silva", "Funcionário" },
                    { 3, "37452415094", "Carla Souza", "Funcionário" },
                    { 4, "02997489016", "Diego Costa", "Funcionário" },
                    { 5, "69710540084", "Elena Santos", "Funcionário" },
                    { 6, "74916864000", "Fábio Lima", "Funcionário" },
                    { 7, "71483624072", "Gisele Almeida", "Funcionário" },
                    { 8, "79733835064", "Hugo Mendes", "Funcionário" },
                    { 9, "41078925062", "Inês Oliveira", "Funcionário" },
                    { 10, "10919399002", "João Rocha", "Funcionário" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogsPerformance_UsuarioId",
                table: "LogsPerformance",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogsPerformance");

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 10);

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
    }
}
