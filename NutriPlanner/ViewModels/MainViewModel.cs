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
        // Текущее отображаемое View
        private object _currentView;

        // Статусное сообщение
        private string _statusMessage = "Готов к работе";

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

        // Команды навигации
        public ICommand ShowDailyNutritionCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowNutritionPlanCommand { get; }
        public ICommand ShowAboutCommand { get; }

        // ViewModels
        public DailyNutritionViewModel DailyNutritionVM { get; }
        public ProductsViewModel ProductsVM { get; }
        public NutritionPlanViewModel NutritionPlanVM { get; }

        public MainViewModel()
        {
            // Инициализация дочерних ViewModel
            DailyNutritionVM = new DailyNutritionViewModel(this);
            ProductsVM = new ProductsViewModel(this);
            NutritionPlanVM = new NutritionPlanViewModel(this);

            // Инициализация команд
            ShowDailyNutritionCommand = new RelayCommand(ShowDailyNutrition);
            ShowProductsCommand = new RelayCommand(ShowProducts);
            ShowNutritionPlanCommand = new RelayCommand(ShowNutritionPlan);
            ShowAboutCommand = new RelayCommand(ShowAbout);

            // Открываем дневник по умолчанию
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

        private void ShowAbout()
        {
            var wnd = new AboutWindow();
            wnd.ShowDialog();
        }

        /// <summary>
        /// Обновляет статусную строку
        /// </summary>
        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }
    }
}
