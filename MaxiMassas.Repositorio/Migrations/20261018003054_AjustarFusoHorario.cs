using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaxiMassas.Repositorio.Migrations
{
    /// <inheritdoc />
    public partial class AjustarFusoHorario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DataCadastro",
                table: "Clientes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'E. South America Standard Time' AS datetime2)",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            // O SQL Server do RDS roda em UTC; a trigger passa a gravar o horário de Brasília.
            migrationBuilder.Sql(@"
ALTER TRIGGER TR_Clientes_DataAtualizacao
ON Clientes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE c
       SET DataAtualizacao = CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'E. South America Standard Time' AS datetime2)
      FROM Clientes c
      JOIN inserted i ON i.Id = c.Id;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DataCadastro",
                table: "Clientes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'E. South America Standard Time' AS datetime2)");

            migrationBuilder.Sql(@"
ALTER TRIGGER TR_Clientes_DataAtualizacao
ON Clientes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE c
       SET DataAtualizacao = GETDATE()
      FROM Clientes c
      JOIN inserted i ON i.Id = c.Id;
END");
        }
    }
}
