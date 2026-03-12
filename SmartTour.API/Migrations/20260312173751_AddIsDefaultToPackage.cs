using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDefaultToPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "ServicePackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
                columns: new[] { "CreatedAt", "IsDefault" },
                values: new object[] { new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(8015), false });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "IsDefault" },
                values: new object[] { new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(8019), false });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "IsDefault" },
                values: new object[] { new DateTime(2026, 3, 12, 17, 37, 50, 147, DateTimeKind.Utc).AddTicks(8022), false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "ServicePackages");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 18, 2, 29, 104, DateTimeKind.Utc).AddTicks(5843));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 18, 2, 29, 104, DateTimeKind.Utc).AddTicks(5845));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 18, 2, 29, 104, DateTimeKind.Utc).AddTicks(5846));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 18, 2, 29, 104, DateTimeKind.Utc).AddTicks(5847));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 18, 2, 29, 104, DateTimeKind.Utc).AddTicks(5848));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 18, 2, 29, 104, DateTimeKind.Utc).AddTicks(5914));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 18, 2, 29, 104, DateTimeKind.Utc).AddTicks(5920));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 11, 18, 2, 29, 104, DateTimeKind.Utc).AddTicks(5921));
        }
    }
}
