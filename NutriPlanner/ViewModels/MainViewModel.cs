using NutriPlanner.Models;
using NutriPlanner.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// Главный ViewModel приложения (Фасад для управления всеми модулями)
    /// </summary>

    public class MainViewModel : BaseViewModel
    {
        public User CurrentUser { get; set; }

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "Готов к работе";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand ShowDailyNutritionCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowNutritionPlanCommand { get; }
        public ICommand ShowAboutCommand { get; }

        public DailyNutritionViewModel DailyNutritionVM { get; }
        public ProductsViewModel ProductsVM { get; }
        public NutritionPlanViewModel NutritionPlanVM { get; }

        public MainViewModel(User currentUser)
        {
            CurrentUser = currentUser;

            DailyNutritionVM = new DailyNutritionViewModel(this);
            ProductsVM = new ProductsViewModel(this);
            NutritionPlanVM = new NutritionPlanViewModel(this);

            ShowDailyNutritionCommand = new RelayCommand(ShowDailyNutrition);
            ShowProductsCommand = new RelayCommand(ShowProducts);
            ShowNutritionPlanCommand = new RelayCommand(ShowNutritionPlan);
            ShowAboutCommand = new RelayCommand(ShowAbout);

            ShowDailyNutrition();
        }

        private void ShowDailyNutrition() => CurrentView = new DailyNutritionView { DataContext = DailyNutritionVM };
        private void ShowProducts() => CurrentView = new ProductsView { DataContext = ProductsVM };
        private void ShowNutritionPlan() => CurrentView = new NutritionPlanView { DataContext = NutritionPlanVM };
        private void ShowAbout()
        {
            var wnd = new AboutWindow();
            wnd.ShowDialog();
        }

        public void UpdateStatus(string message) => StatusMessage = message;
    }
}
