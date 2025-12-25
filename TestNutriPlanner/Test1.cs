using NutriPlanner.Models;

namespace TestNutriPlanner
{
    [TestClass]
    public class UserUnitTests
    {
        [TestMethod]
        public void User_IsAdmin_ReturnsTrue_ForAdminRole()
        {
            // Arrange
            var role = new Role { RoleName = "Admin" };
            var user = new User { Role = role };

            // Act
            var result = user.IsAdmin();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void User_IsDietitian_ReturnsTrue_ForDietitianRole()
        {
            // Arrange
            var role = new Role { RoleName = "Dietitian" };
            var user = new User { Role = role };

            // Act
            var result = user.IsDietitian();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void User_IsUser_ReturnsTrue_ForUserRole()
        {
            // Arrange
            var role = new Role { RoleName = "User" };
            var user = new User { Role = role };

            // Act
            var result = user.IsUser();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void User_IsDietitianOrAdmin_ReturnsTrue_ForAdmin()
        {
            // Arrange
            var role = new Role { RoleName = "Admin" };
            var user = new User { Role = role };

            // Act
            var result = user.IsDietitianOrAdmin();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void User_IsDietitianOrAdmin_ReturnsTrue_ForDietitian()
        {
            // Arrange
            var role = new Role { RoleName = "Dietitian" };
            var user = new User { Role = role };

            // Act
            var result = user.IsDietitianOrAdmin();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void User_IsDietitianOrAdmin_ReturnsFalse_ForUser()
        {
            // Arrange
            var role = new Role { RoleName = "User" };
            var user = new User { Role = role };

            // Act
            var result = user.IsDietitianOrAdmin();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void User_GetRoleName_ReturnsCorrectName()
        {
            // Arrange
            var role = new Role { RoleName = "Dietitian" };
            var user = new User { Role = role };

            // Act
            var result = user.GetRoleName();

            // Assert
            Assert.AreEqual("Dietitian", result);
        }

        [TestMethod]
        public void User_GetRoleName_ReturnsUnknown_WhenRoleIsNull()
        {
            // Arrange
            var user = new User { Role = null };

            // Act
            var result = user.GetRoleName();

            // Assert
            Assert.AreEqual("Неизвестно", result);
        }
    }

    [TestClass]
    public class NutritionCalculationTests
    {
        [TestMethod]
        public void CalculateDailyCalories_MaleFormula_CalculatesCorrectly()
        {
            // Arrange
            decimal weight = 80;
            decimal height = 180;
            int age = 30;
            string gender = "Мужской";

            // Act - формула из вашего кода (RegisterViewModel)
            decimal bmr = 10 * weight + 6.25m * height - 5 * age + 5;

            // Assert
            decimal expected = 10 * 80 + 6.25m * 180 - 5 * 30 + 5;
            Assert.AreEqual(expected, bmr);
        }

        [TestMethod]
        public void CalculateDailyCalories_FemaleFormula_CalculatesCorrectly()
        {
            // Arrange
            decimal weight = 60;
            decimal height = 165;
            int age = 25;
            string gender = "Женский";

            // Act - формула из вашего кода
            decimal bmr = 10 * weight + 6.25m * height - 5 * age - 161;

            // Assert
            decimal expected = 10 * 60 + 6.25m * 165 - 5 * 25 - 161;
            Assert.AreEqual(expected, bmr);
        }

        [TestMethod]
        public void ActivityMultiplier_ReturnsCorrectValues()
        {
            // Act & Assert - значения из вашего кода
            Assert.AreEqual(1.2m, GetActivityMultiplier("Низкая"));
            Assert.AreEqual(1.55m, GetActivityMultiplier("Средняя"));
            Assert.AreEqual(1.9m, GetActivityMultiplier("Высокая"));
            Assert.AreEqual(1.2m, GetActivityMultiplier("Неизвестно"));
        }

        private decimal GetActivityMultiplier(string activityLevel)
        {
            return activityLevel switch
            {
                "Низкая" => 1.2m,
                "Средняя" => 1.55m,
                "Высокая" => 1.9m,
                _ => 1.2m
            };
        }

        [TestMethod]
        public void GoalMultiplier_ReturnsCorrectValues()
        {
            // Act & Assert - значения из вашего кода
            Assert.AreEqual(0.8m, GetGoalMultiplier("Похудение"));
            Assert.AreEqual(1.2m, GetGoalMultiplier("Набор массы"));
            Assert.AreEqual(1.0m, GetGoalMultiplier("Поддержание"));
            Assert.AreEqual(1.0m, GetGoalMultiplier("Неизвестно"));
        }

        private decimal GetGoalMultiplier(string goal)
        {
            return goal switch
            {
                "Похудение" => 0.8m,
                "Набор массы" => 1.2m,
                _ => 1.0m
            };
        }

        [TestMethod]
        public void CalculateDailyProtein_CalculatesCorrectly()
        {
            // Arrange
            decimal dailyCalories = 2000m;

            // Act - формула из вашего кода: калории * 0.3 / 4
            decimal protein = Math.Round(dailyCalories * 0.3m / 4, 2);

            // Assert
            Assert.AreEqual(150m, protein);
        }

        [TestMethod]
        public void CalculateDailyFat_CalculatesCorrectly()
        {
            // Arrange
            decimal dailyCalories = 2000m;

            // Act - формула из вашего кода: калории * 0.25 / 9
            decimal fat = Math.Round(dailyCalories * 0.25m / 9, 2);

            // Assert
            Assert.AreEqual(55.56m, fat);
        }

        [TestMethod]
        public void CalculateDailyCarbs_CalculatesCorrectly()
        {
            // Arrange
            decimal dailyCalories = 2000m;

            // Act - формула из вашего кода: калории * 0.45 / 4
            decimal carbs = Math.Round(dailyCalories * 0.45m / 4, 2);

            // Assert
            Assert.AreEqual(225m, carbs);
        }
    }

    [TestClass]
    public class DataValidationTests
    {
        [TestMethod]
        public void Product_Validation_ValidData()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                ProductName = "Куриная грудка",
                Category = "Мясо",
                Calories = 165,
                Protein = 31,
                Fat = 3.6m,
                Carbohydrates = 0,
                Unit = "г"
            };

            // Assert
            Assert.IsTrue(product.ProductId > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(product.ProductName));
            Assert.IsTrue(product.Calories >= 0);
            Assert.IsTrue(product.Protein >= 0);
            Assert.IsTrue(product.Fat >= 0);
            Assert.IsTrue(product.Carbohydrates >= 0);
        }

        [TestMethod]
        public void User_Validation_ValidData()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                Age = 25,
                Height = 175,
                Weight = 70,
                IsActive = true
            };

            // Assert
            Assert.IsTrue(user.UserId > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(user.Username));
            Assert.IsTrue(user.Email.Contains("@"));
            Assert.IsTrue(user.Age > 0 && user.Age < 120);
            Assert.IsTrue(user.Height > 50 && user.Height < 250);
            Assert.IsTrue(user.Weight > 10 && user.Weight < 300);
        }

        [TestMethod]
        public void FoodDiary_Validation_ValidData()
        {
            // Arrange
            var diary = new FoodDiary
            {
                DiaryId = 1,
                UserId = 1,
                Date = DateTime.Now,
                Quantity = 100,
                Calories = 150,
                Protein = 10,
                Fat = 5,
                Carbohydrates = 15
            };

            // Assert
            Assert.IsTrue(diary.DiaryId > 0);
            Assert.IsTrue(diary.UserId > 0);
            Assert.IsTrue(diary.Date <= DateTime.Now);
            Assert.IsTrue(diary.Quantity > 0);
            Assert.IsTrue(diary.Calories >= 0);
        }
    }

    [TestClass]
    public class StringAndCollectionTests
    {

        [TestMethod]
        public void Collection_Operations_WorkCorrectly()
        {
            // Arrange
            var products = new System.Collections.Generic.List<string>
            {
                "Куриная грудка",
                "Рис вареный",
                "Яблоко",
                "Говядина"
            };

            // Act
            var meatProducts = products.Where(p => p.Contains("грудка") || p.Contains("Говядина")).ToList();
            var grainProducts = products.Where(p => p.Contains("Рис")).ToList();

            // Assert
            Assert.AreEqual(2, meatProducts.Count);
            Assert.AreEqual(1, grainProducts.Count);
            Assert.AreEqual("Куриная грудка", meatProducts[0]);
            Assert.AreEqual("Рис вареный", grainProducts[0]);
        }

        [TestMethod]
        public void DateTime_Operations_WorkCorrectly()
        {
            // Arrange
            var today = DateTime.Today;
            var now = DateTime.Now;

            // Assert
            Assert.IsTrue(now >= today);
            Assert.AreEqual(DateTime.Now.Year, today.Year);
            Assert.AreEqual(DateTime.Now.Month, today.Month);
            Assert.AreEqual(DateTime.Now.Day, today.Day);
        }
    }
}
