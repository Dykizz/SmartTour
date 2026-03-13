using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoiRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    POIId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoiRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoiRequests_Pois_POIId",
                        column: x => x.POIId,
                        principalTable: "Pois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PoiRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_PoiRequests_POIId",
                table: "PoiRequests",
                column: "POIId");

            migrationBuilder.CreateIndex(
                name: "IX_PoiRequests_UserId",
                table: "PoiRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoiRequests");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(7954));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(7956));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(7957));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(7958));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(7959));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(8015));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(8019));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(8022));
        }
    }
}
