using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaxiMassas.Repositorio.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarLogSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogsSistema",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Excecao = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsSistema", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogsSistema_DataHora",
                table: "LogsSistema",
                column: "DataHora");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogsSistema");
        }
    }
}
