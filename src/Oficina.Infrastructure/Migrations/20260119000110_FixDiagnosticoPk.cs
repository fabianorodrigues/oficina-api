using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDiagnosticoPk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Diagnosticos",
                table: "Diagnosticos");

            migrationBuilder.DropIndex(
                name: "IX_Diagnosticos_OrdemServicoId",
                table: "Diagnosticos");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Diagnosticos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Diagnosticos",
                table: "Diagnosticos",
                column: "OrdemServicoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Diagnosticos",
                table: "Diagnosticos");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Diagnosticos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Diagnosticos",
                table: "Diagnosticos",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnosticos_OrdemServicoId",
                table: "Diagnosticos",
                column: "OrdemServicoId",
                unique: true);
        }
    }
}
