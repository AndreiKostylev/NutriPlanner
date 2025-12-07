using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "IsActive", "RegistrationDate" },
                values: new object[] { true, new DateTime(2025, 12, 7, 23, 11, 47, 890, DateTimeKind.Local).AddTicks(4260) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "IsActive", "RegistrationDate" },
                values: new object[] { true, new DateTime(2025, 12, 7, 23, 11, 47, 890, DateTimeKind.Local).AddTicks(4266) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "RegistrationDate",
                value: new DateTime(2025, 12, 6, 0, 41, 22, 153, DateTimeKind.Local).AddTicks(9041));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                column: "RegistrationDate",
                value: new DateTime(2025, 12, 6, 0, 41, 22, 153, DateTimeKind.Local).AddTicks(9046));
        }
    }
}
