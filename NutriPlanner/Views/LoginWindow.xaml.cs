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
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            var vm = new LoginViewModel(LoginSuccess, OpenRegister);
            DataContext = vm;
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }

        // Метод, который вызывается после успешного входа
        private void LoginSuccess(User user)
        {
            if (user == null)
                return;

            // Передаём пользователя в MainWindow
            var mainWnd = new MainWindow(user);
            mainWnd.Show();
            this.Close();
        }

        private void OpenRegister()
        {
            var regWnd = new RegisterWindow(() =>
            {
                this.Show(); // Показываем окно логина обратно
            });
            this.Hide(); // Скрываем окно логина
            regWnd.ShowDialog();
        }

    }
}
