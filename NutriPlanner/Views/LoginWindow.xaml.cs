using NutriPlanner.Models;
using NutriPlanner.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NutriPlanner.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            var vm = new LoginViewModel(LoginSuccess, OpenRegister, CloseWindow);
            DataContext = vm;
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }

        private void LoginSuccess(User user)
        {
            if (user == null) return;

            // Открываем главное окно
            var mainWindow = new MainWindow(user);
            mainWindow.Show();

            // Закрываем окно входа
            this.Close();
        }

        private void OpenRegister()
        {
            this.Hide();

            var registerWindow = new RegisterWindow(() =>
            {
                // При возврате из регистрации
                this.Show();
                this.Activate();
            });

            registerWindow.Owner = this;
            registerWindow.ShowDialog();
        }

        private void CloseWindow()
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Если это последнее окно, закрываем приложение
            if (Application.Current.Windows.Count == 1)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
