using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToServicePackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SoftDeleteAt",
                table: "ServicePackages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 15, 27, 44, 385, DateTimeKind.Utc).AddTicks(1721));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 15, 27, 44, 385, DateTimeKind.Utc).AddTicks(1724));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 15, 27, 44, 385, DateTimeKind.Utc).AddTicks(1725));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 15, 27, 44, 385, DateTimeKind.Utc).AddTicks(1726));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 15, 27, 44, 385, DateTimeKind.Utc).AddTicks(1727));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "SoftDeleteAt" },
                values: new object[] { new DateTime(2026, 3, 11, 15, 27, 44, 385, DateTimeKind.Utc).AddTicks(1783), null });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "SoftDeleteAt" },
                values: new object[] { new DateTime(2026, 3, 11, 15, 27, 44, 385, DateTimeKind.Utc).AddTicks(1790), null });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "SoftDeleteAt" },
                values: new object[] { new DateTime(2026, 3, 11, 15, 27, 44, 385, DateTimeKind.Utc).AddTicks(1792), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoftDeleteAt",
                table: "ServicePackages");

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

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6374));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6378));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 14, 2, 49, 359, DateTimeKind.Utc).AddTicks(6379));
        }
    }
}
