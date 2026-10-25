using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaxiMassas.Repositorio.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTelegramEntregadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelegramEntregador1",
                table: "ConfiguracoesEntrega",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramEntregador2",
                table: "ConfiguracoesEntrega",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ConfiguracoesEntrega",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "TelegramEntregador1", "TelegramEntregador2" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramEntregador1",
                table: "ConfiguracoesEntrega");

            migrationBuilder.DropColumn(
                name: "TelegramEntregador2",
                table: "ConfiguracoesEntrega");
        }
    }
}
