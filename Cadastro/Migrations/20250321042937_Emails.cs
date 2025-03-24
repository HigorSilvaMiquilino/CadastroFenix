using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadastro.Migrations
{
    /// <inheritdoc />
    public partial class Emails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataCreate",
                table: "Usuarios",
                type: "nvarchar(23)",
                maxLength: 23,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DataUpdate",
                table: "Usuarios",
                type: "nvarchar(23)",
                maxLength: 23,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserIp",
                table: "Usuarios",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "DataCreate",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DataUpdate",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UserIp",
                table: "Usuarios");
        }
    }
}
