using Microsoft.EntityFrameworkCore;
using NutriPlanner.Data;
using NutriPlanner.Models;
using NutriPlanner.Models.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// ViewModel для создания планов питания диетологом для клиентов
    /// </summary>
    public class NutritionPlanViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private readonly User _currentUser;
        private readonly DatabaseContext _context;

        // Клиенты для выбора
        private ObservableCollection<UserProfileDto> _clients;
        private UserProfileDto _selectedClient;

        // Данные для плана
        private string _planName = "План питания";
        private DateTime _startDate = DateTime.Today;
        private DateTime _endDate = DateTime.Today.AddDays(7);
        private string _status = "Активен";

        // Цели плана
        private decimal _targetCalories;
        private decimal _targetProtein;
        private decimal _targetFat;
        private decimal _targetCarbs;

        // Рекомендуемые блюда
        private ObservableCollection<DishDto> _recommendedDishes;
        private DishDto _selectedDish;

        public ObservableCollection<UserProfileDto> Clients
        {
            get => _clients;
            set { _clients = value; OnPropertyChanged(); }
        }

        public UserProfileDto SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedClient));
                if (value != null)
                {
                    LoadClientData();
                    LoadRecommendedDishes();
                }
            }
        }

        public string PlanName
        {
            get => _planName;
            set { _planName = value; OnPropertyChanged(); }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public decimal TargetCalories
        {
            get => _targetCalories;
            set { _targetCalories = value; OnPropertyChanged(); }
        }

        public decimal TargetProtein
        {
            get => _targetProtein;
            set { _targetProtein = value; OnPropertyChanged(); }
        }

        public decimal TargetFat
        {
            get => _targetFat;
            set { _targetFat = value; OnPropertyChanged(); }
        }

        public decimal TargetCarbs
        {
            get => _targetCarbs;
            set { _targetCarbs = value; OnPropertyChanged(); }
        }

        public ObservableCollection<DishDto> RecommendedDishes
        {
            get => _recommendedDishes;
            set { _recommendedDishes = value; OnPropertyChanged(); }
        }

        public DishDto SelectedDish
        {
            get => _selectedDish;
            set { _selectedDish = value; OnPropertyChanged(); }
        }

        public bool HasSelectedClient => SelectedClient != null;

        // Команды
        public ICommand LoadClientsCommand { get; }
        public ICommand CreatePlanCommand { get; }
        public ICommand CalculateTargetsCommand { get; }
        public ICommand AddDishToPlanCommand { get; }
        public ICommand RemoveDishFromPlanCommand { get; }
        public ICommand GenerateMealPlanCommand { get; }

        public NutritionPlanViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;
            _context = new DatabaseContext();

            Clients = new ObservableCollection<UserProfileDto>();
            RecommendedDishes = new ObservableCollection<DishDto>();

            LoadClientsCommand = new RelayCommand(LoadClients);
            CreatePlanCommand = new RelayCommand(CreatePlan, () => HasSelectedClient && !string.IsNullOrWhiteSpace(PlanName));
            CalculateTargetsCommand = new RelayCommand(CalculateTargets, () => HasSelectedClient);
            AddDishToPlanCommand = new RelayCommand(AddDishToPlan, () => SelectedDish != null);
            RemoveDishFromPlanCommand = new RelayCommand(RemoveDishFromPlan, () => SelectedDish != null);
            GenerateMealPlanCommand = new RelayCommand(GenerateMealPlan, () => HasSelectedClient);

            // Загружаем клиентов только если это диетолог
            if (currentUser.IsDietitianOrAdmin())
            {
                LoadClients();
            }
        }

        /// <summary>
        /// Загружает список клиентов для диетолога
        /// </summary>
        private async void LoadClients()
        {
            try
            {
                if (!_currentUser.IsDietitianOrAdmin())
                {
                    MessageBox.Show("У вас нет прав для создания планов питания",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Clients.Clear();

                var clients = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName == "User" && u.IsActive)
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                foreach (var client in clients)
                {
                    Clients.Add(new UserProfileDto
                    {
                        UserId = client.UserId,
                        Username = client.Username,
                        Email = client.Email,
                        Age = client.Age,
                        Gender = client.Gender,
                        Height = client.Height,
                        Weight = client.Weight,
                        ActivityLevel = client.ActivityLevel,
                        Goal = client.Goal,
                        DailyCalorieTarget = client.DailyCalorieTarget,
                        DailyProteinTarget = client.DailyProteinTarget,
                        DailyFatTarget = client.DailyFatTarget,
                        DailyCarbsTarget = client.DailyCarbsTarget
                    });
                }

                if (Clients.Any())
                {
                    SelectedClient = Clients.First();
                }

                _mainVM.UpdateStatus($"Загружено {Clients.Count} клиентов");
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка загрузки клиентов: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает данные выбранного клиента
        /// </summary>
        private void LoadClientData()
        {
            if (SelectedClient == null) return;

            // Используем цели клиента как базовые
            TargetCalories = SelectedClient.DailyCalorieTarget;
            TargetProtein = SelectedClient.DailyProteinTarget;
            TargetFat = SelectedClient.DailyFatTarget;
            TargetCarbs = SelectedClient.DailyCarbsTarget;

            PlanName = $"План для {SelectedClient.Username}";

            _mainVM.UpdateStatus($"Загружены данные клиента: {SelectedClient.Username}");
        }

        /// <summary>
        /// Загружает рекомендованные блюда для плана
        /// </summary>
        private async void LoadRecommendedDishes()
        {
            try
            {
                RecommendedDishes.Clear();

                if (SelectedClient == null) return;

                // Загружаем блюда диетолога для рекомендаций
                var dishes = await _context.Dishes
                    .Include(d => d.DishProducts)
                    .ThenInclude(dp => dp.Product)
                    .Where(d => d.UserId == _currentUser.UserId) // Блюда созданные диетологом
                    .OrderBy(d => d.TotalCalories)
                    .Take(8) // Ограничиваем количество
                    .ToListAsync();

                foreach (var dish in dishes)
                {
                    var dishDto = new DishDto
                    {
                        DishId = dish.DishId,
                        DishName = dish.DishName,
                        TotalCalories = dish.TotalCalories,
                        TotalProtein = dish.TotalProtein,
                        TotalFat = dish.TotalFat,
                        TotalCarbohydrates = dish.TotalCarbohydrates,
                        Recipe = "Рекомендовано диетологом"
                    };

                    // Добавляем ингредиенты
                    foreach (var ingredient in dish.DishProducts)
                    {
                        dishDto.Ingredients.Add(new ProductDto
                        {
                            ProductId = ingredient.Product.ProductId,
                            ProductName = ingredient.Product.ProductName,
                            Category = ingredient.Product.Category,
                            Calories = ingredient.Product.Calories,
                            Protein = ingredient.Product.Protein,
                            Fat = ingredient.Product.Fat,
                            Carbohydrates = ingredient.Product.Carbohydrates,
                            Unit = ingredient.Product.Unit,
                            Quantity = ingredient.Quantity
                        });
                    }

                    RecommendedDishes.Add(dishDto);
                }

                _mainVM.UpdateStatus($"Загружено {RecommendedDishes.Count} рекомендованных блюд");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки блюд: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Рассчитывает целевые показатели на основе данных клиента
        /// </summary>
        private void CalculateTargets()
        {
            if (SelectedClient == null) return;

            // Используем стандартную формулу расчета
            TargetCalories = SelectedClient.DailyCalorieTarget;
            TargetProtein = SelectedClient.DailyProteinTarget;
            TargetFat = SelectedClient.DailyFatTarget;
            TargetCarbs = SelectedClient.DailyCarbsTarget;

            // Можно добавить корректировку по цели клиента
            if (SelectedClient.Goal == "Похудение")
            {
                TargetCalories = Math.Round(TargetCalories * 0.85m); // -15% для похудения
            }
            else if (SelectedClient.Goal == "Набор массы")
            {
                TargetCalories = Math.Round(TargetCalories * 1.15m); // +15% для набора массы
            }

            // Пересчитываем БЖУ
            TargetProtein = Math.Round(TargetCalories * 0.3m / 4); // 30% от калорий
            TargetFat = Math.Round(TargetCalories * 0.25m / 9); // 25% от калорий
            TargetCarbs = Math.Round(TargetCalories * 0.45m / 4); // 45% от калорий

            _mainVM.UpdateStatus($"Цели рассчитаны для клиента {SelectedClient.Username}");
        }

        /// <summary>
        /// Добавляет блюдо в план
        /// </summary>
        private void AddDishToPlan()
        {
            if (SelectedDish == null) return;

            MessageBox.Show($"Блюдо '{SelectedDish.DishName}' добавлено в план\n" +
                          $"Калории: {SelectedDish.TotalCalories} ккал",
                          "Блюдо добавлено", MessageBoxButton.OK, MessageBoxImage.Information);

            _mainVM.UpdateStatus($"Добавлено блюдо: {SelectedDish.DishName}");
        }

        /// <summary>
        /// Удаляет блюдо из плана
        /// </summary>
        private void RemoveDishFromPlan()
        {
            if (SelectedDish == null) return;

            MessageBox.Show($"Блюдо '{SelectedDish.DishName}' удалено из плана",
                          "Блюдо удалено", MessageBoxButton.OK, MessageBoxImage.Information);

            _mainVM.UpdateStatus($"Удалено блюдо: {SelectedDish.DishName}");
        }

        /// <summary>
        /// Генерирует примерный план питания
        /// </summary>
        private void GenerateMealPlan()
        {
            if (SelectedClient == null) return;

            string mealPlan = $"Примерный план питания для {SelectedClient.Username}:\n\n";
            mealPlan += $"Цели на день:\n";
            mealPlan += $"• Калории: {TargetCalories} ккал\n";
            mealPlan += $"• Белки: {TargetProtein} г\n";
            mealPlan += $"• Жиры: {TargetFat} г\n";
            mealPlan += $"• Углеводы: {TargetCarbs} г\n\n";

            mealPlan += $"Рекомендуемое распределение:\n";
            mealPlan += $"• Завтрак (25%): {Math.Round(TargetCalories * 0.25m)} ккал\n";
            mealPlan += $"• Обед (35%): {Math.Round(TargetCalories * 0.35m)} ккал\n";
            mealPlan += $"• Ужин (30%): {Math.Round(TargetCalories * 0.30m)} ккал\n";
            mealPlan += $"• Перекусы (10%): {Math.Round(TargetCalories * 0.10m)} ккал\n\n";

            mealPlan += $"Рекомендации:\n";
            mealPlan += $"• Пить 2-3 литра воды в день\n";
            mealPlan += $"• Есть каждые 3-4 часа\n";
            mealPlan += $"• Включать овощи в каждый прием пищи\n";

            MessageBox.Show(mealPlan, "Сгенерированный план питания",
                MessageBoxButton.OK, MessageBoxImage.Information);

            _mainVM.UpdateStatus($"Сгенерирован план для {SelectedClient.Username}");
        }

        /// <summary>
        /// Создает план питания для клиента
        /// </summary>
        private async void CreatePlan()
        {
            try
            {
                if (!_currentUser.IsDietitianOrAdmin())
                {
                    MessageBox.Show("У вас нет прав для создания планов питания",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedClient == null)
                {
                    MessageBox.Show("Выберите клиента для создания плана",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (EndDate <= StartDate)
                {
                    MessageBox.Show("Дата окончания должна быть позже даты начала",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (TargetCalories <= 0)
                {
                    MessageBox.Show("Укажите корректное количество калорий",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем план питания
                var plan = new NutritionPlan
                {
                    UserId = SelectedClient.UserId,
                    PlanName = PlanName,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    DailyCalories = TargetCalories,
                    DailyProtein = TargetProtein,
                    DailyFat = TargetFat,
                    DailyCarbohydrates = TargetCarbs,
                    Status = Status
                };

                _context.NutritionPlans.Add(plan);
                await _context.SaveChangesAsync();

                _mainVM.UpdateStatus($"Создан план '{PlanName}' для клиента {SelectedClient.Username}");
                MessageBox.Show($"План питания успешно создан!\n\n" +
                              $"Клиент: {SelectedClient.Username}\n" +
                              $"Название: {PlanName}\n" +
                              $"Период: {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}\n" +
                              $"Калории: {TargetCalories} ккал/день\n" +
                              $"Статус: {Status}",
                              "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Очищаем форму
                ClearForm();
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка создания плана: {ex.Message}");
                MessageBox.Show($"Ошибка создания плана: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Очищает форму создания плана
        /// </summary>
        private void ClearForm()
        {
            PlanName = "Новый план питания";
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(7);
            Status = "Активен";

            if (SelectedClient != null)
            {
                LoadClientData(); // Загружаем данные клиента заново
            }
        }
    }
}
