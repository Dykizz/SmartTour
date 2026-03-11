using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxPoiAllowed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6309));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6312));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6314));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6315));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6315));

            migrationBuilder.InsertData(
                table: "ServicePackages",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "DurationDays", "IsActive", "MaxPoiAllowed", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "FREE", new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6374), "Dành cho người dùng cá nhân phổ thông", 365, true, 1, "Gói miễn phí", 0m },
                    { 2, "PRO_MONTH", new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6378), "Phù hợp cho các quán kinh doanh nhỏ", 30, true, 5, "Vĩnh Khánh Pro (Tháng)", 150000m },
                    { 3, "VIP_YEAR", new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6379), "Đầy đủ tính năng cao cấp", 365, true, 20, "VIP Toàn Năng (Năm)", 1200000m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServicePackages");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 18, 46, 26, 625, DateTimeKind.Utc).AddTicks(3903));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 18, 46, 26, 625, DateTimeKind.Utc).AddTicks(3906));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 18, 46, 26, 625, DateTimeKind.Utc).AddTicks(3907));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 18, 46, 26, 625, DateTimeKind.Utc).AddTicks(3908));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 18, 46, 26, 625, DateTimeKind.Utc).AddTicks(3909));
        }
    }
}
