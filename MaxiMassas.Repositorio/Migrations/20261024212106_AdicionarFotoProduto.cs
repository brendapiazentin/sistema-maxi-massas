using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaxiMassas.Repositorio.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarFotoProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FotoAtualizadaEm",
                table: "Produtos",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoAtualizadaEm",
                table: "Produtos");
        }
    }
}
