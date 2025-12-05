using NutriPlanner.Models;
using NutriPlanner.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// Главный ViewModel приложения (Фасад для управления всеми модулями)
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private User _currentUser;
        private object _currentView;
        private string _statusMessage = "Готов к работе";

        public User CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }

        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand ShowDailyNutritionCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowNutritionPlanCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand ShowProfileCommand { get; }
        public ICommand LogoutCommand { get; }

        public DailyNutritionViewModel DailyNutritionVM { get; }
        public ProductsViewModel ProductsVM { get; }
        public NutritionPlanViewModel NutritionPlanVM { get; }

        public MainViewModel(User currentUser)
        {
            CurrentUser = currentUser;

            DailyNutritionVM = new DailyNutritionViewModel(this, currentUser);
            ProductsVM = new ProductsViewModel(this, currentUser);
            NutritionPlanVM = new NutritionPlanViewModel(this, currentUser);

            ShowDailyNutritionCommand = new RelayCommand(ShowDailyNutrition);
            ShowProductsCommand = new RelayCommand(ShowProducts);
            ShowNutritionPlanCommand = new RelayCommand(ShowNutritionPlan);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            ShowProfileCommand = new RelayCommand(ShowProfile);
            LogoutCommand = new RelayCommand(Logout);

            ShowDailyNutrition();
        }

        private void ShowDailyNutrition()
        {
            CurrentView = DailyNutritionVM;
            StatusMessage = "Режим: Дневник питания";
        }

        private void ShowProducts()
        {
            CurrentView = ProductsVM;
            StatusMessage = "Режим: Управление продуктами";
        }

        private void ShowNutritionPlan()
        {
            CurrentView = NutritionPlanVM;
            StatusMessage = "Режим: План питания";
        }

        private void ShowProfile()
        {
            StatusMessage = "Режим: Профиль пользователя";
            // TODO: Создать UserProfileView
        }

        private void ShowAbout()
        {
            var wnd = new AboutWindow();
            wnd.Owner = Application.Current.MainWindow;
            wnd.ShowDialog();
            StatusMessage = "О программе открыто";
        }

        private void Logout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Закрываем главное окно
                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                if (mainWindow != null)
                {
                    mainWindow.Close();
                }

                // Открываем окно входа
                var loginWindow = new LoginWindow();
                loginWindow.Show();
            }
        }

        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }
    }
}
