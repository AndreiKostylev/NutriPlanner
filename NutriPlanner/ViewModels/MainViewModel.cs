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
    /// Главный ViewModel приложения
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private User _currentUser;
        private object _currentView;
        private string _statusMessage = "Готов к работе";

        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUser));
                OnPropertyChanged(nameof(IsDietitian));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(IsDietitianOrAdmin));
                OnPropertyChanged(nameof(UserDisplayName));

                // ОБЯЗАТЕЛЬНО инициализируем ViewModels ПОСЛЕ установки пользователя
                InitializeViewModels();
            }
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

        // Свойства для проверки ролей
        public bool IsUser => CurrentUser?.IsUser() ?? false;
        public bool IsDietitian => CurrentUser?.IsDietitian() ?? false;
        public bool IsAdmin => CurrentUser?.IsAdmin() ?? false;
        public bool IsDietitianOrAdmin => IsDietitian || IsAdmin;
        public string UserDisplayName => CurrentUser != null ?
            $"{CurrentUser.Username} ({CurrentUser.GetRoleName()})" : "Не авторизован";

        // ViewModels
        public DailyNutritionViewModel DailyNutritionVM { get; private set; }
        public ProductsViewModel ProductsVM { get; private set; }
        public NutritionPlanViewModel NutritionPlanVM { get; private set; }
        public DietitianDashboardViewModel DietitianDashboardVM { get; private set; }

        // Команды
        public ICommand ShowDailyNutritionCommand { get; private set; }
        public ICommand ShowProductsCommand { get; private set; }
        public ICommand ShowNutritionPlanCommand { get; private set; }
        public ICommand ShowProfileCommand { get; private set; }
        public ICommand ShowAboutCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
        public ICommand ShowDietitianDashboardCommand { get; private set; }

        public MainViewModel(User currentUser)
        {
            // ВАЖНО: Сначала устанавливаем пользователя
            CurrentUser = currentUser;

            // ПОТОМ инициализируем команды
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            ShowDailyNutritionCommand = new RelayCommand(ShowDailyNutrition);
            ShowProductsCommand = new RelayCommand(ShowProducts);
            ShowNutritionPlanCommand = new RelayCommand(ShowNutritionPlan);
            ShowProfileCommand = new RelayCommand(ShowProfile);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            LogoutCommand = new RelayCommand(Logout);
            ShowDietitianDashboardCommand = new RelayCommand(ShowDietitianDashboard);
        }

        private void InitializeViewModels()
        {
            try
            {
                if (CurrentUser != null)
                {
                    // Всегда создаем базовые ViewModels
                    DailyNutritionVM = new DailyNutritionViewModel(this, CurrentUser);
                    ProductsVM = new ProductsViewModel(this, CurrentUser);
                    NutritionPlanVM = new NutritionPlanViewModel(this, CurrentUser);

                  
                    if (IsDietitianOrAdmin)
                    {
                        DietitianDashboardVM = new DietitianDashboardViewModel(this);

                        // Автоматически показываем Dashboard при входе диетолога/админа
                        ShowDietitianDashboard();
                    }
                    else
                    {
                        // Для обычных пользователей показываем дневник
                        ShowDailyNutrition();
                    }

                    StatusMessage = $"Добро пожаловать, {CurrentUser.Username}!";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Ошибка загрузки модулей";
            }
        }

        private void ShowDailyNutrition()
        {
            if (DailyNutritionVM != null)
            {
                CurrentView = DailyNutritionVM;
                StatusMessage = "Режим: Дневник питания";
            }
        }

        private void ShowProducts()
        {
            if (ProductsVM != null)
            {
                CurrentView = ProductsVM;
                StatusMessage = "Режим: Управление продуктами";
            }
        }

        private void ShowNutritionPlan()
        {
            if (NutritionPlanVM != null)
            {
                CurrentView = NutritionPlanVM;
                StatusMessage = "Режим: План питания";
            }
        }

        private void ShowDietitianDashboard()
        {
            // ВАЖНО: Проверяем права перед показом
            if (!IsDietitianOrAdmin)
            {
                MessageBox.Show("У вас нет прав для доступа к панели диетолога!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DietitianDashboardVM != null)
            {
                CurrentView = DietitianDashboardVM;
                StatusMessage = "Режим: Панель диетолога";
            }
            else
            {
                MessageBox.Show("Панель диетолога не инициализирована",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowProfile()
        {
            StatusMessage = "Режим: Профиль пользователя";
            MessageBox.Show($"Профиль: {CurrentUser?.Username}\nРоль: {CurrentUser?.GetRoleName()}",
                "Профиль", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowAbout()
        {
            try
            {
                var wnd = new AboutWindow();
                wnd.Owner = Application.Current.MainWindow;
                wnd.ShowDialog();
                StatusMessage = "О программе открыто";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Logout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Application.Current.MainWindow?.Close();
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при выходе: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }
    }
}
