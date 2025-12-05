using NutriPlanner.Data;
using NutriPlanner.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using NutriPlanner.Views;

namespace NutriPlanner.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly DatabaseContext _db = new();
        private readonly Action _registrationSuccess;
        private readonly Action _backToLogin;

        // Поля
        private string _username;
        private string _email;
        private string _password;
        private int _age = 25;
        private string _gender = "Мужской";
        private decimal _height = 175;
        private decimal _weight = 70;
        private string _activityLevel = "Средняя";
        private string _goal = "Поддержание";

        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }
        public int Age { get => _age; set { _age = value; OnPropertyChanged(); } }
        public string Gender { get => _gender; set { _gender = value; OnPropertyChanged(); } }
        public decimal Height { get => _height; set { _height = value; OnPropertyChanged(); } }
        public decimal Weight { get => _weight; set { _weight = value; OnPropertyChanged(); } }
        public string ActivityLevel { get => _activityLevel; set { _activityLevel = value; OnPropertyChanged(); } }
        public string Goal { get => _goal; set { _goal = value; OnPropertyChanged(); } }

        // Списки для ComboBox
        public ObservableCollection<string> Genders { get; } = new() { "Мужской", "Женский" };
        public ObservableCollection<string> ActivityLevels { get; } = new() { "Низкая", "Средняя", "Высокая" };
        public ObservableCollection<string> Goals { get; } = new() { "Поддержание", "Похудение", "Набор массы" };

        // Команды
        public ICommand RegisterCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public RegisterViewModel(Action registrationSuccess, Action backToLogin)
        {
            _registrationSuccess = registrationSuccess;
            _backToLogin = backToLogin;

            RegisterCommand = new RelayCommand(Register, CanRegister);
            BackToLoginCommand = new RelayCommand(() => _backToLogin?.Invoke());
        }

        private bool CanRegister()
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Age > 0 && Height > 0 && Weight > 0;
        }

        private void Register()
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(Username))
                {
                    MessageBox.Show("Логин обязателен для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show("Пароль обязателен для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(Email))
                {
                    MessageBox.Show("Email обязателен для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Age < 1 || Age > 120)
                {
                    MessageBox.Show("Возраст должен быть от 1 до 120 лет", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Height < 50 || Height > 250)
                {
                    MessageBox.Show("Рост должен быть от 50 до 250 см", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Weight < 10 || Weight > 300)
                {
                    MessageBox.Show("Вес должен быть от 10 до 300 кг", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка существующего пользователя
                if (_db.Users.Any(u => u.Username == Username))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_db.Users.Any(u => u.Email == Email))
                {
                    MessageBox.Show("Пользователь с таким email уже существует", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Получаем роль "User"
                var userRole = _db.Roles.FirstOrDefault(r => r.RoleName == "User");
                if (userRole == null)
                {
                    MessageBox.Show("Ошибка: роль 'User' не найдена в системе", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем нового пользователя
                var newUser = new User
                {
                    Username = Username.Trim(),
                    Email = Email.Trim(),
                    PasswordHash = Password, // В реальном приложении нужно хэшировать пароль!
                    Age = Age,
                    Gender = Gender,
                    Height = Height,
                    Weight = Weight,
                    ActivityLevel = ActivityLevel,
                    Goal = Goal,
                    RoleId = userRole.RoleId,
                    RegistrationDate = DateTime.Now,
                    // Рассчитываем целевые показатели
                    DailyCalorieTarget = CalculateDailyCalories(),
                    DailyProteinTarget = CalculateDailyProtein(),
                    DailyFatTarget = CalculateDailyFat(),
                    DailyCarbsTarget = CalculateDailyCarbs()
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                MessageBox.Show("Регистрация успешна!\nТеперь вы можете войти в систему.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Вызываем колбэк успешной регистрации
                _registrationSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal CalculateDailyCalories()
        {
            decimal bmr = Gender == "Мужской"
                ? 10 * Weight + 6.25m * Height - 5 * Age + 5
                : 10 * Weight + 6.25m * Height - 5 * Age - 161;

            decimal activityMultiplier = ActivityLevel switch
            {
                "Низкая" => 1.2m,
                "Средняя" => 1.55m,
                "Высокая" => 1.9m,
                _ => 1.2m
            };

            decimal goalMultiplier = Goal switch
            {
                "Похудение" => 0.8m,
                "Набор массы" => 1.2m,
                _ => 1.0m
            };

            return Math.Round(bmr * activityMultiplier * goalMultiplier, 2);
        }

        private decimal CalculateDailyProtein() => Math.Round(CalculateDailyCalories() * 0.3m / 4, 2);
        private decimal CalculateDailyFat() => Math.Round(CalculateDailyCalories() * 0.25m / 9, 2);
        private decimal CalculateDailyCarbs() => Math.Round(CalculateDailyCalories() * 0.45m / 4, 2);
    }
}
