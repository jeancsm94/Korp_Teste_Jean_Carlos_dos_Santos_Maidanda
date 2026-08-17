using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Estoque.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Apenas o Id=1 (P001) chegou a ser de fato inserido pela migration SeedProducts —
            // os demais Ids do seed original (2-5) já foram ocupados por produtos reais
            // cadastrados manualmente e NÃO devem ser apagados por esta migration.
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Balance", "Code", "CreatedAt", "Description", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 100, "P001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Caneta Esferográfica Azul", null }
                });
        }
    }
}
