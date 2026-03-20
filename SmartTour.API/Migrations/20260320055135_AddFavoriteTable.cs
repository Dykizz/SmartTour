using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PoiId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favorites_Pois_PoiId",
                        column: x => x.PoiId,
                        principalTable: "Pois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 5, 51, 32, 852, DateTimeKind.Utc).AddTicks(4443));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 5, 51, 32, 852, DateTimeKind.Utc).AddTicks(4448));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 5, 51, 32, 852, DateTimeKind.Utc).AddTicks(4450));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 5, 51, 32, 852, DateTimeKind.Utc).AddTicks(4451));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 5, 51, 32, 852, DateTimeKind.Utc).AddTicks(4452));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 5, 51, 32, 852, DateTimeKind.Utc).AddTicks(4539));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 5, 51, 32, 852, DateTimeKind.Utc).AddTicks(4544));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 5, 51, 32, 852, DateTimeKind.Utc).AddTicks(4547));

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_PoiId",
                table: "Favorites",
                column: "PoiId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_PoiId",
                table: "Favorites",
                columns: new[] { "UserId", "PoiId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 13, 4, 2, 6, 208, DateTimeKind.Utc).AddTicks(9052));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 13, 4, 2, 6, 208, DateTimeKind.Utc).AddTicks(9056));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 13, 4, 2, 6, 208, DateTimeKind.Utc).AddTicks(9057));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 13, 4, 2, 6, 208, DateTimeKind.Utc).AddTicks(9058));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 13, 4, 2, 6, 208, DateTimeKind.Utc).AddTicks(9058));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 13, 4, 2, 6, 208, DateTimeKind.Utc).AddTicks(9116));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 13, 4, 2, 6, 208, DateTimeKind.Utc).AddTicks(9120));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 13, 4, 2, 6, 208, DateTimeKind.Utc).AddTicks(9122));
        }
    }
}
