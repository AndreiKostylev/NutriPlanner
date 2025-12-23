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
        public NutritionPlanViewModel NutritionPlanVM { get; private set; } // Только для диетологов
        public DietitianDashboardViewModel DietitianDashboardVM { get; private set; }
        public ClientManagementViewModel ClientManagementVM { get; private set; }
        public UserPlanViewViewModel UserPlanViewVM { get; private set; } // Для просмотра планов

        // Команды
        public ICommand ShowDailyNutritionCommand { get; private set; }
        public ICommand ShowProductsCommand { get; private set; }
        public ICommand ShowNutritionPlanCommand { get; private set; }
        public ICommand ShowDietitianDashboardCommand { get; private set; }
        public ICommand ShowClientManagementCommand { get; private set; }
        public ICommand ShowMealTemplatesCommand { get; private set; } // УДАЛИТЬ ЭТУ КОМАНДУ
        public ICommand ShowUserPlanViewCommand { get; private set; }
        public ICommand ShowProfileCommand { get; private set; }
        public ICommand ShowAboutCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }

        public MainViewModel(User currentUser)
        {
            CurrentUser = currentUser;
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            ShowDailyNutritionCommand = new RelayCommand(ShowDailyNutrition);
            ShowProductsCommand = new RelayCommand(ShowProducts);
            ShowNutritionPlanCommand = new RelayCommand(ShowNutritionPlan);
            ShowDietitianDashboardCommand = new RelayCommand(ShowDietitianDashboard);
            ShowClientManagementCommand = new RelayCommand(ShowClientManagement);
            // УДАЛИТЬ: ShowMealTemplatesCommand = new RelayCommand(ShowMealTemplates);
            ShowUserPlanViewCommand = new RelayCommand(ShowUserPlanView);
            ShowProfileCommand = new RelayCommand(ShowProfile);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            LogoutCommand = new RelayCommand(Logout);
        }

        private void InitializeViewModels()
        {
            try
            {
                if (CurrentUser != null)
                {
                    // Всегда создаем базовые ViewModels для всех пользователей
                    DailyNutritionVM = new DailyNutritionViewModel(this, CurrentUser);
                    ProductsVM = new ProductsViewModel(this, CurrentUser);

                    // UserPlanViewViewModel для просмотра планов (доступен всем)
                    UserPlanViewVM = new UserPlanViewViewModel(this, CurrentUser);

                    // NutritionPlanVM создаем ТОЛЬКО для диетологов и админов
                    if (IsDietitianOrAdmin)
                    {
                        NutritionPlanVM = new NutritionPlanViewModel(this, CurrentUser);
                        DietitianDashboardVM = new DietitianDashboardViewModel(this);
                        ClientManagementVM = new ClientManagementViewModel(this, CurrentUser);
                        // MealTemplatesVM больше не создаем
                    }

                    // По умолчанию показываем дневник питания
                    ShowDailyNutrition();

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
            // Проверяем права доступа
            if (!IsDietitianOrAdmin)
            {
                MessageBox.Show("Создание планов питания доступно только диетологам и администраторам.\n\n" +
                              "Для просмотра своих планов используйте раздел 'Мои планы питания'.",
                              "Ограничение доступа",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            if (NutritionPlanVM != null)
            {
                CurrentView = NutritionPlanVM;
                StatusMessage = "Режим: Создание планов питания (для диетологов)";
            }
            else
            {
                MessageBox.Show("Модуль создания планов не инициализирован",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowUserPlanView()
        {
            if (UserPlanViewVM != null)
            {
                CurrentView = UserPlanViewVM;
                StatusMessage = "Режим: Мои планы питания";
            }
            else
            {
                MessageBox.Show("Модуль просмотра планов не инициализирован",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDietitianDashboard()
        {
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

        private void ShowClientManagement()
        {
            if (!IsDietitianOrAdmin)
            {
                MessageBox.Show("У вас нет прав для управления клиентами!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ClientManagementVM != null)
            {
                CurrentView = ClientManagementVM;
                StatusMessage = "Режим: Управление клиентами";
            }
        }


        private void ShowProfile()
        {
            StatusMessage = "Режим: Профиль пользователя";
            MessageBox.Show($"Профиль: {CurrentUser?.Username}\n" +
                          $"Роль: {CurrentUser?.GetRoleName()}\n" +
                          $"Email: {CurrentUser?.Email}\n" +
                          $"Возраст: {CurrentUser?.Age}\n" +
                          $"Рост: {CurrentUser?.Height} см\n" +
                          $"Вес: {CurrentUser?.Weight} кг\n" +
                          $"Цель: {CurrentUser?.Goal}\n" +
                          $"Активность: {CurrentUser?.ActivityLevel}\n" +
                          $"Цели: {CurrentUser?.DailyCalorieTarget} ккал/день",
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
