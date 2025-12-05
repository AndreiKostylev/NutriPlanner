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
    public partial class RegisterWindow : Window
    {
        private readonly Action _backToLogin;

        public RegisterWindow(Action backToLogin = null)
        {
            InitializeComponent();
            _backToLogin = backToLogin;
            DataContext = new RegisterViewModel(OnRegistrationSuccess, _backToLogin);
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }

        private void OnRegistrationSuccess()
        {
            // Закрываем окно регистрации
            this.Close();

            // Возвращаемся к окну входа
            _backToLogin?.Invoke();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Если пользователь закрывает окно крестиком
            if (_backToLogin != null)
            {
                _backToLogin.Invoke();
            }
        }
    }
}
