using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NutriPlanner.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Calories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Protein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Carbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Height = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActivityLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DailyCalorieTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyProteinTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyFatTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyCarbsTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId");
                });

            migrationBuilder.CreateTable(
                name: "Dishes",
                columns: table => new
                {
                    DishId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DishName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalCalories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalProtein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCarbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dishes", x => x.DishId);
                    table.ForeignKey(
                        name: "FK_Dishes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "NutritionPlans",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DailyCalories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyProtein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyFat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyCarbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionPlans", x => x.PlanId);
                    table.ForeignKey(
                        name: "FK_NutritionPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "DishProducts",
                columns: table => new
                {
                    DishId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishProducts", x => new { x.DishId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_DishProducts_Dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "Dishes",
                        principalColumn: "DishId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodDiaries",
                columns: table => new
                {
                    DiaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    DishId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Calories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Protein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Carbohydrates = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodDiaries", x => x.DiaryId);
                    table.ForeignKey(
                        name: "FK_FoodDiaries_Dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "Dishes",
                        principalColumn: "DishId");
                    table.ForeignKey(
                        name: "FK_FoodDiaries_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId");
                    table.ForeignKey(
                        name: "FK_FoodDiaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "Calories", "Carbohydrates", "Category", "Fat", "ProductName", "Protein", "Unit" },
                values: new object[,]
                {
                    { 1, 165m, 0m, "Мясо", 3.6m, "Куриная грудка", 31m, "г" },
                    { 2, 250m, 0m, "Мясо", 15m, "Говядина", 26m, "г" },
                    { 3, 130m, 28m, "Крупы", 0.3m, "Рис вареный", 2.7m, "г" },
                    { 4, 132m, 25m, "Крупы", 2.3m, "Гречка", 4.5m, "г" },
                    { 5, 52m, 14m, "Фрукты", 0.2m, "Яблоко", 0.3m, "г" },
                    { 6, 242m, 0m, "Мясо", 16m, "Свинина", 25m, "г" },
                    { 7, 135m, 0m, "Мясо", 1m, "Индейка", 29m, "г" },
                    { 8, 541m, 1.4m, "Мясо", 42m, "Бекон", 37m, "г" },
                    { 9, 208m, 0m, "Рыба", 13m, "Лосось", 20m, "г" },
                    { 10, 184m, 0m, "Рыба", 6m, "Тунец", 30m, "г" },
                    { 11, 82m, 0m, "Рыба", 0.7m, "Треска", 18m, "г" },
                    { 12, 85m, 0m, "Морепродукты", 0.5m, "Креветки", 20m, "г" },
                    { 13, 155m, 1.1m, "Яйца", 11m, "Яйцо куриное", 13m, "шт" },
                    { 14, 121m, 1.8m, "Молочные", 5m, "Творог 5%", 17m, "г" },
                    { 15, 404m, 1.3m, "Молочные", 34m, "Сыр Чеддер", 23m, "г" },
                    { 16, 52m, 4.7m, "Молочные", 2.5m, "Молоко 2.5%", 2.9m, "мл" },
                    { 17, 59m, 3.6m, "Молочные", 0.4m, "Йогурт греческий", 10m, "г" },
                    { 18, 68m, 12m, "Крупы", 1.4m, "Овсянка", 2.4m, "г" },
                    { 19, 158m, 31m, "Крупы", 0.9m, "Макароны вареные", 5.8m, "г" },
                    { 20, 247m, 41m, "Хлеб", 3.4m, "Хлеб цельнозерновой", 13m, "г" },
                    { 21, 77m, 17m, "Овощи", 0.1m, "Картофель", 2m, "г" },
                    { 22, 41m, 10m, "Овощи", 0.2m, "Морковь", 0.9m, "г" },
                    { 23, 18m, 3.9m, "Овощи", 0.2m, "Помидор", 0.9m, "г" },
                    { 24, 15m, 3.6m, "Овощи", 0.1m, "Огурец", 0.7m, "г" },
                    { 25, 34m, 7m, "Овощи", 0.4m, "Брокколи", 2.8m, "г" },
                    { 26, 23m, 3.6m, "Овощи", 0.4m, "Шпинат", 2.9m, "г" },
                    { 27, 89m, 23m, "Фрукты", 0.3m, "Банан", 1.1m, "г" },
                    { 28, 47m, 12m, "Фрукты", 0.1m, "Апельсин", 0.9m, "г" },
                    { 29, 32m, 7.7m, "Фрукты", 0.3m, "Клубника", 0.7m, "г" },
                    { 30, 160m, 9m, "Фрукты", 15m, "Авокадо", 2m, "г" },
                    { 31, 579m, 22m, "Орехи", 50m, "Миндаль", 21m, "г" },
                    { 32, 654m, 14m, "Орехи", 65m, "Грецкие орехи", 15m, "г" },
                    { 33, 567m, 16m, "Орехи", 49m, "Арахис", 26m, "г" },
                    { 34, 884m, 0m, "Масла", 100m, "Оливковое масло", 0m, "мл" },
                    { 35, 717m, 0.1m, "Масла", 81m, "Сливочное масло", 0.9m, "г" },
                    { 36, 116m, 20m, "Бобовые", 0.4m, "Чечевица вареная", 9m, "г" },
                    { 37, 127m, 23m, "Бобовые", 0.5m, "Фасоль", 9m, "г" },
                    { 38, 0m, 0m, "Напитки", 0m, "Вода", 0m, "мл" },
                    { 39, 1m, 0.3m, "Напитки", 0m, "Чай черный", 0m, "мл" },
                    { 40, 1m, 0m, "Напитки", 0m, "Кофе черный", 0.1m, "мл" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 1, "User" },
                    { 2, "Dietitian" },
                    { 3, "Admin" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "ActivityLevel", "Age", "DailyCalorieTarget", "DailyCarbsTarget", "DailyFatTarget", "DailyProteinTarget", "Email", "Gender", "Goal", "Height", "IsActive", "PasswordHash", "RegistrationDate", "RoleId", "Username", "Weight" },
                values: new object[,]
                {
                    { 1, "Moderate", 30, 2500m, 281m, 83m, 150m, "admin@nutriplanner.com", "Male", "Maintenance", 180m, true, "hashed_password", new DateTime(2025, 12, 22, 22, 39, 16, 789, DateTimeKind.Local).AddTicks(9987), 3, "admin", 75m },
                    { 2, "Moderate", 35, 2200m, 247m, 73m, 130m, "dietitian@nutriplanner.com", "Female", "Maintenance", 165m, true, "hashed_password", new DateTime(2025, 12, 22, 22, 39, 16, 789, DateTimeKind.Local).AddTicks(9999), 2, "dietitian", 60m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_UserId",
                table: "Dishes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DishProducts_ProductId",
                table: "DishProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodDiaries_DishId",
                table: "FoodDiaries",
                column: "DishId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodDiaries_ProductId",
                table: "FoodDiaries",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodDiaries_UserId",
                table: "FoodDiaries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionPlans_UserId",
                table: "NutritionPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductName",
                table: "Products",
                column: "ProductName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DishProducts");

            migrationBuilder.DropTable(
                name: "FoodDiaries");

            migrationBuilder.DropTable(
                name: "NutritionPlans");

            migrationBuilder.DropTable(
                name: "Dishes");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
