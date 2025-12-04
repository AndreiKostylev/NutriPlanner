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

namespace NutriPlanner.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username;
        private string _password;

        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        public ICommand LoginCommand { get; }
        public ICommand OpenRegisterCommand { get; }

        private readonly Action<User> _loginSuccess;
        private readonly Action _openRegister;

        public LoginViewModel(Action<User> loginSuccess, Action openRegister)
        {
            _loginSuccess = loginSuccess;
            _openRegister = openRegister;

            LoginCommand = new RelayCommand(Login);
            OpenRegisterCommand = new RelayCommand(() => _openRegister?.Invoke());
        }

        private void Login()
        {
            try
            {
                using var db = new DatabaseContext();
                var user = db.Users
                    .Where(u => u.Username == Username && u.PasswordHash == Password)
                    .FirstOrDefault();

                if (user == null)
                {
                    MessageBox.Show("Неверный логин или пароль");
                    return;
                }

                _loginSuccess?.Invoke(user);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка входа: " + ex.Message);
            }
        }
    }
}
