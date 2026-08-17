using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Estoque.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Balance", "Code", "CreatedAt", "Description", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 100, "P001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Caneta Esferográfica Azul", null },
                    { 2, 50, "P002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Caderno Universitário 96 Folhas", null },
                    { 3, 200, "P003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lápis HB2", null },
                    { 4, 75, "P004", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Borracha Branca", null },
                    { 5, 30, "P005", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Apontador Metálico", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
