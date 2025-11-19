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
        private object _currentView;
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

        // Дочерние ViewModels
        public DailyNutritionViewModel DailyNutritionVM { get; }
       

        public MainViewModel()
        {
            // Инициализация дочерних ViewModels
            DailyNutritionVM = new DailyNutritionViewModel(this);
          

            // Инициализация команд
            ShowDailyNutritionCommand = new RelayCommand(ShowDailyNutrition);
          
            

            // Показываем дневник питания по умолчанию
            ShowDailyNutrition();
        }

        private void ShowDailyNutrition()
        {
            CurrentView = DailyNutritionVM;
            StatusMessage = "Режим: Дневник питания";
        }

    

      

        /// <summary>
        /// Обновляет статусное сообщение
        /// </summary>
        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }
    }
}
