using NutriPlanner.Data;
using NutriPlanner.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using NutriPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace NutriPlanner.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username;
        private string _password;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand OpenRegisterCommand { get; }
        public ICommand ExitCommand { get; }

        private readonly Action<User> _loginSuccess;
        private readonly Action _openRegister;
        private readonly Action _closeWindow;

        public LoginViewModel(Action<User> loginSuccess, Action openRegister, Action closeWindow)
        {
            _loginSuccess = loginSuccess;
            _openRegister = openRegister;
            _closeWindow = closeWindow;

            LoginCommand = new RelayCommand(Login, CanLogin);
            OpenRegisterCommand = new RelayCommand(() => _openRegister?.Invoke());
            ExitCommand = new RelayCommand(() => _closeWindow?.Invoke());
        }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private void Login()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show("Введите логин и пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var db = new DatabaseContext();

                // Ищем пользователя по логину с загрузкой роли
                var user = db.Users
                    .Include(u => u.Role) // ВАЖНО: загружаем связанную роль
                    .FirstOrDefault(u => u.Username == Username);

                if (user == null)
                {
                    MessageBox.Show("Пользователь с таким логином не найден", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем пароль (в реальном приложении нужно хэшировать!)
                if (user.PasswordHash != Password)
                {
                    MessageBox.Show("Неверный пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Успешный вход
                _loginSuccess?.Invoke(user);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
