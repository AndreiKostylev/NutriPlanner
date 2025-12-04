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

        // Поля
        private string _username;
        private string _email;
        private string _password;
        private int _age;
        private string _gender;
        private decimal _height;
        private decimal _weight;
        private string _activityLevel;
        private string _goal;

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
        public ObservableCollection<string> Genders { get; set; } = new() { "Мужской", "Женский" };
        public ObservableCollection<string> ActivityLevels { get; set; } = new() { "Низкая", "Средняя", "Высокая" };
        public ObservableCollection<string> Goals { get; set; } = new() { "Поддержание", "Похудение", "Набор массы" };

        // Команды
        public ICommand RegisterCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public RegisterViewModel(Action backAction)
        {
            RegisterCommand = new RelayCommand(Register);
            BackToLoginCommand = new RelayCommand(() => backAction?.Invoke());
        }

        private void Register()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show("Логин и пароль обязательны");
                    return;
                }

                if (_db.Users.Any(u => u.Username == Username))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует");
                    return;
                }

                var userRole = _db.Roles.First(r => r.RoleName == "User");

                var newUser = new User
                {
                    Username = Username,
                    Email = Email,
                    PasswordHash = Password,
                    Age = Age,
                    Gender = Gender,
                    Height = Height,
                    Weight = Weight,
                    ActivityLevel = ActivityLevel,
                    Goal = Goal,
                    RoleId = userRole.RoleId,
                    RegistrationDate = DateTime.Now
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                MessageBox.Show("Регистрация успешна");

                // Закрываем окно после регистрации
                Application.Current.Windows
                    .OfType<RegisterWindow>()
                    .FirstOrDefault()?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка регистрации: " + ex.Message);
            }
        }

    }
}
