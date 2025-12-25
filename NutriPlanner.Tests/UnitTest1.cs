using NutriPlanner.Models.DTO;
using System.Data;

namespace NutriPlanner.Tests
{
    public class UnitTest1
    {
       
        // Тесты для User класса
        public class UserTests
        {
            [Fact]
            public void User_IsAdmin_ReturnsTrue_ForAdminRole()
            {
                // Arrange
                var role = new Role { RoleName = "Admin" };
                var user = new User { Role = role };

                // Act
                var result = user.IsAdmin();

                // Assert
                Assert.True(result);
            }

            [Fact]
            public void User_IsAdmin_ReturnsFalse_ForNonAdminRole()
            {
                // Arrange
                var role = new Role { RoleName = "User" };
                var user = new User { Role = role };

                // Act
                var result = user.IsAdmin();

                // Assert
                Assert.False(result);
            }

            [Fact]
            public void User_IsDietitian_ReturnsTrue_ForDietitianRole()
            {
                // Arrange
                var role = new Role { RoleName = "Dietitian" };
                var user = new User { Role = role };

                // Act
                var result = user.IsDietitian();

                // Assert
                Assert.True(result);
            }

            [Fact]
            public void User_IsDietitianOrAdmin_ReturnsTrue_ForAdmin()
            {
                // Arrange
                var role = new Role { RoleName = "Admin" };
                var user = new User { Role = role };

                // Act
                var result = user.IsDietitianOrAdmin();

                // Assert
                Assert.True(result);
            }

            [Fact]
            public void User_GetRoleName_ReturnsRoleName()
            {
                // Arrange
                var role = new Role { RoleName = "Dietitian" };
                var user = new User { Role = role };

                // Act
                var result = user.GetRoleName();

                // Assert
                Assert.Equal("Dietitian", result);
            }

            [Fact]
            public void User_GetRoleName_ReturnsUnknown_WhenRoleIsNull()
            {
                // Arrange
                var user = new User { Role = null };

                // Act
                var result = user.GetRoleName();

                // Assert
                Assert.Equal("Неизвестно", result);
            }
        }

        // Тесты для BaseViewModel
        public class BaseViewModelTests
        {
            [Fact]
            public void BaseViewModel_PropertyChanged_RaisesEvent()
            {
                // Arrange
                var viewModel = new TestViewModel();
                bool eventRaised = false;
                viewModel.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == "TestProperty")
                        eventRaised = true;
                };

                // Act
                viewModel.TestProperty = "New Value";

                // Assert
                Assert.True(eventRaised);
            }
        }

        // Вспомогательный класс для тестирования BaseViewModel
        public class TestViewModel : BaseViewModel
        {
            private string _testProperty;
            public string TestProperty
            {
                get => _testProperty;
                set
                {
                    _testProperty = value;
                    OnPropertyChanged();
                }
            }
        }

        // Тесты для DTO классов
        public class DtoTests
        {
            [Fact]
            public void UserDto_Properties_SetAndGetCorrectly()
            {
                // Arrange & Act
                var userDto = new UserDto
                {
                    UserId = 1,
                    Username = "testuser",
                    Email = "test@test.com",
                    RoleName = "User",
                    RoleId = 2,
                    IsActive = true,
                    RegistrationDate = new DateTime(2024, 1, 1)
                };

                // Assert
                Assert.Equal(1, userDto.UserId);
                Assert.Equal("testuser", userDto.Username);
                Assert.Equal("test@test.com", userDto.Email);
                Assert.Equal("User", userDto.RoleName);
                Assert.Equal(2, userDto.RoleId);
                Assert.True(userDto.IsActive);
                Assert.Equal(new DateTime(2024, 1, 1), userDto.RegistrationDate);
            }

            [Fact]
            public void DailyNutritionDto_CalculateProgress_Works()
            {
                // Arrange
                var dto = new DailyNutritionDto
                {
                    TotalCalories = 500,
                    TargetCalories = 1000,
                    TotalProtein = 50,
                    TargetProtein = 100
                };

                // Act
                dto.CaloriesProgress = Math.Round((dto.TotalCalories / dto.TargetCalories) * 100, 1);
                dto.ProteinProgress = Math.Round((dto.TotalProtein / dto.TargetProtein) * 100, 1);

                // Assert
                Assert.Equal(50.0, dto.CaloriesProgress);
                Assert.Equal(50.0, dto.ProteinProgress);
            }
        }

        // Тесты для проверки математических расчетов
        public class CalculationTests
        {
            [Theory]
            [InlineData(70, 175, 30, "Мужской", 1656.75)] // Формула Миффлина-Сан Жеора
            [InlineData(60, 165, 25, "Женский", 1373.25)]
            public void CalculateCalories_ReturnsExpectedResult(decimal weight, decimal height, int age, string gender, decimal expected)
            {
                // Arrange
                decimal bmr;

                // Act
                if (gender == "Мужской")
                {
                    bmr = 10 * weight + 6.25m * height - 5 * age + 5;
                }
                else
                {
                    bmr = 10 * weight + 6.25m * height - 5 * age - 161;
                }

                decimal activityMultiplier = 1.2m; // Низкая активность
                decimal goalMultiplier = 1.0m; // Поддержание

                decimal result = Math.Round(bmr * activityMultiplier * goalMultiplier, 2);

                // Assert
                Assert.Equal(expected, result);
            }

            [Fact]
            public void MathOperations_WorkCorrectly()
            {
                // Arrange
                decimal value1 = 10.5m;
                decimal value2 = 5.5m;

                // Act & Assert
                Assert.Equal(16m, value1 + value2);
                Assert.Equal(5m, value1 - value2);
                Assert.Equal(57.75m, value1 * value2);
                Assert.Equal(1.909m, Math.Round(value1 / value2, 3));
            }
        }

        // Тесты для проверки строковых операций
        public class StringTests
        {
            [Fact]
            public void String_Contains_WorksCorrectly()
            {
                // Arrange
                string text = "НутриПланнер - система планирования питания";

                // Act & Assert
                Assert.Contains("планирования", text);
                Assert.DoesNotContain("отдыха", text);
            }

            [Fact]
            public void String_Format_WorksCorrectly()
            {
                // Arrange
                string username = "testuser";
                string role = "Admin";

                // Act
                string result = $"{username} ({role})";

                // Assert
                Assert.Equal("testuser (Admin)", result);
            }

            [Fact]
            public void String_IsNullOrWhiteSpace_WorksCorrectly()
            {
                // Arrange
                string empty = "";
                string whitespace = "   ";
                string nullString = null;
                string valid = "text";

                // Act & Assert
                Assert.True(string.IsNullOrWhiteSpace(empty));
                Assert.True(string.IsNullOrWhiteSpace(whitespace));
                Assert.True(string.IsNullOrWhiteSpace(nullString));
                Assert.False(string.IsNullOrWhiteSpace(valid));
            }
        }

        // Тесты для проверки коллекций
        public class CollectionTests
        {
            [Fact]
            public void ObservableCollection_AddAndCount_Works()
            {
                // Arrange
                var collection = new System.Collections.ObjectModel.ObservableCollection<string>();

                // Act
                collection.Add("Item1");
                collection.Add("Item2");
                collection.Add("Item3");

                // Assert
                Assert.Equal(3, collection.Count);
                Assert.Contains("Item1", collection);
                Assert.Contains("Item2", collection);
                Assert.Contains("Item3", collection);
            }

            [Fact]
            public void List_Filter_Works()
            {
                // Arrange
                var numbers = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                // Act
                var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();
                var oddNumbers = numbers.Where(n => n % 2 != 0).ToList();

                // Assert
                Assert.Equal(5, evenNumbers.Count);
                Assert.Equal(5, oddNumbers.Count);
                Assert.All(evenNumbers, n => Assert.True(n % 2 == 0));
                Assert.All(oddNumbers, n => Assert.True(n % 2 != 0));
            }
        }
    }
}