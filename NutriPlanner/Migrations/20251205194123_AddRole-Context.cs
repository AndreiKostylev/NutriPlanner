using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "RoleName",
                value: "Dietitian");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[] { 3, "Admin" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "RegistrationDate", "RoleId" },
                values: new object[] { new DateTime(2025, 12, 6, 0, 41, 22, 153, DateTimeKind.Local).AddTicks(9041), 3 });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "ActivityLevel", "Age", "DailyCalorieTarget", "DailyCarbsTarget", "DailyFatTarget", "DailyProteinTarget", "Email", "Gender", "Goal", "Height", "PasswordHash", "RegistrationDate", "RoleId", "Username", "Weight" },
                values: new object[] { 2, "Moderate", 35, 2200m, 247m, 73m, 130m, "dietitian@nutriplanner.com", "Female", "Maintenance", 165m, "hashed_password", new DateTime(2025, 12, 6, 0, 41, 22, 153, DateTimeKind.Local).AddTicks(9046), 2, "dietitian", 60m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "RoleName",
                value: "Admin");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "RegistrationDate", "RoleId" },
                values: new object[] { new DateTime(2025, 11, 19, 1, 18, 22, 923, DateTimeKind.Local).AddTicks(4181), 2 });
        }
    }
}
