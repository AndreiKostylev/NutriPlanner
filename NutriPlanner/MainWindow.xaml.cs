using NutriPlanner.Models;
using NutriPlanner.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NutriPlanner
{
    public partial class MainWindow : Window
    {
        public User CurrentUser { get; private set; }

        public MainWindow(User currentUser)
        {
            InitializeComponent();

            if (currentUser == null)
            {
         
                MessageBox.Show("Ошибка: пользователь не найден", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            CurrentUser = currentUser;

            
            var mainVM = new MainViewModel(currentUser);
            DataContext = mainVM;

            Title = $"NutriPlanner - {currentUser.Username}";
            mainVM.UpdateStatus($"Добро пожаловать, {currentUser.Username}!");
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            
            if (Application.Current.MainWindow == this)
            {
                var result = MessageBox.Show("Закрыть приложение?", "Выход",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
