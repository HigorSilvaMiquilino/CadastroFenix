using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadastro.Migrations
{
    /// <inheritdoc />
    public partial class Produtos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValorUnitario",
                table: "Cupons");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Produtos",
                newName: "descricao");

            migrationBuilder.RenameColumn(
                name: "QuantidadeProdutos",
                table: "Cupons",
                newName: "quantidadeTotal");

            migrationBuilder.AddColumn<int>(
                name: "quantidade",
                table: "Produtos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "valor",
                table: "Produtos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "quantidade", "valor" },
                values: new object[] { 0, 0m });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "quantidade", "valor" },
                values: new object[] { 0, 0m });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "quantidade", "valor" },
                values: new object[] { 0, 0m });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "quantidade", "valor" },
                values: new object[] { 0, 0m });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "quantidade", "valor" },
                values: new object[] { 0, 0m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quantidade",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "valor",
                table: "Produtos");

            migrationBuilder.RenameColumn(
                name: "descricao",
                table: "Produtos",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "quantidadeTotal",
                table: "Cupons",
                newName: "QuantidadeProdutos");

            migrationBuilder.AddColumn<decimal>(
                name: "ValorUnitario",
                table: "Cupons",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
