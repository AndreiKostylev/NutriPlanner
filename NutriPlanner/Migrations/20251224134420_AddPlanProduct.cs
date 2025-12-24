using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "NutritionPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "NutritionPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanProducts",
                columns: table => new
                {
                    PlanProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanProducts", x => x.PlanProductId);
                    table.ForeignKey(
                        name: "FK_PlanProducts_NutritionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "NutritionPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId");
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "RegistrationDate",
                value: new DateTime(2025, 12, 24, 18, 44, 19, 428, DateTimeKind.Local).AddTicks(6076));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                column: "RegistrationDate",
                value: new DateTime(2025, 12, 24, 18, 44, 19, 428, DateTimeKind.Local).AddTicks(6080));

            migrationBuilder.CreateIndex(
                name: "IX_PlanProducts_PlanId",
                table: "PlanProducts",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanProducts_ProductId",
                table: "PlanProducts",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanProducts");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "NutritionPlans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "NutritionPlans");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "RegistrationDate",
                value: new DateTime(2025, 12, 22, 22, 39, 16, 789, DateTimeKind.Local).AddTicks(9987));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                column: "RegistrationDate",
                value: new DateTime(2025, 12, 22, 22, 39, 16, 789, DateTimeKind.Local).AddTicks(9999));
        }
    }
}
