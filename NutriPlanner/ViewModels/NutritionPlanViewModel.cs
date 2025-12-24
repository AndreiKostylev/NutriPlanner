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
        private DatabaseContext _context;

        // Клиенты для выбора
        private ObservableCollection<UserProfileDto> _clients;
        private UserProfileDto _selectedClient;

        // Данные для плана
        private string _planName = "План питания";
        private DateTime _startDate = DateTime.Today;
        private DateTime _endDate = DateTime.Today.AddDays(7);
        private string _status = "Новый";
        private string _description = "";

        // Цели плана
        private decimal _targetCalories;
        private decimal _targetProtein;
        private decimal _targetFat;
        private decimal _targetCarbs;

        // Продукты для плана
        private ObservableCollection<ProductDto> _allProducts;
        private ObservableCollection<MealPlanItem> _breakfastItems;
        private ObservableCollection<MealPlanItem> _lunchItems;
        private ObservableCollection<MealPlanItem> _dinnerItems;
        private ProductDto _selectedProduct;
        private string _selectedMealType = "Завтрак";
        private decimal _selectedQuantity = 100;
        private string _mealNotes = "";

        // Сводка по приемам пищи
        private MealSummary _breakfastSummary;
        private MealSummary _lunchSummary;
        private MealSummary _dinnerSummary;

        // Для отслеживания текущей вкладки
        private string _currentMealTab = "Завтрак";

        // Коллекции для комбобоксов
        public List<string> MealTypes { get; } = new List<string> { "Завтрак", "Обед", "Ужин" };
        public List<string> StatusTypes { get; } = new List<string> { "Новый", "Отправлен", "Активен", "Неактивен" };

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
                    LoadAllProducts();
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

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
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

        public ObservableCollection<ProductDto> AllProducts
        {
            get => _allProducts;
            set { _allProducts = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MealPlanItem> BreakfastItems
        {
            get => _breakfastItems;
            set { _breakfastItems = value; OnPropertyChanged(); UpdateMealSummaries(); }
        }

        public ObservableCollection<MealPlanItem> LunchItems
        {
            get => _lunchItems;
            set { _lunchItems = value; OnPropertyChanged(); UpdateMealSummaries(); }
        }

        public ObservableCollection<MealPlanItem> DinnerItems
        {
            get => _dinnerItems;
            set { _dinnerItems = value; OnPropertyChanged(); UpdateMealSummaries(); }
        }

        public ProductDto SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public string SelectedMealType
        {
            get => _selectedMealType;
            set
            {
                if (_selectedMealType != value)
                {
                    _selectedMealType = value;
                    OnPropertyChanged();
                    // При изменении SelectedMealType обновляем CurrentMealTab
                    CurrentMealTab = value;
                }
            }
        }

        public decimal SelectedQuantity
        {
            get => _selectedQuantity;
            set { _selectedQuantity = value; OnPropertyChanged(); }
        }

        public string MealNotes
        {
            get => _mealNotes;
            set { _mealNotes = value; OnPropertyChanged(); }
        }

        public MealSummary BreakfastSummary
        {
            get => _breakfastSummary;
            set { _breakfastSummary = value; OnPropertyChanged(); }
        }

        public MealSummary LunchSummary
        {
            get => _lunchSummary;
            set { _lunchSummary = value; OnPropertyChanged(); }
        }

        public MealSummary DinnerSummary
        {
            get => _dinnerSummary;
            set { _dinnerSummary = value; OnPropertyChanged(); }
        }

        public string CurrentMealTab
        {
            get => _currentMealTab;
            set
            {
                if (_currentMealTab != value)
                {
                    _currentMealTab = value;
                    OnPropertyChanged();
                    // При изменении вкладки обновляем SelectedMealType
                    SelectedMealType = value;
                }
            }
        }

        public bool HasSelectedClient => SelectedClient != null;

        // Команды (УБРАНА GenerateAutoPlanCommand)
        public ICommand LoadClientsCommand { get; }
        public ICommand CreatePlanCommand { get; }
        public ICommand CalculateTargetsCommand { get; }
        public ICommand AddProductToMealCommand { get; }
        public ICommand RemoveProductFromMealCommand { get; }
        public ICommand ClearMealCommand { get; }
        public ICommand SendPlanToClientCommand { get; }
        public ICommand AutoFillQuantitiesCommand { get; }

        public NutritionPlanViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;
            _context = new DatabaseContext();

            // Инициализация коллекций
            Clients = new ObservableCollection<UserProfileDto>();
            AllProducts = new ObservableCollection<ProductDto>();
            BreakfastItems = new ObservableCollection<MealPlanItem>();
            LunchItems = new ObservableCollection<MealPlanItem>();
            DinnerItems = new ObservableCollection<MealPlanItem>();

            // Инициализация сводок
            BreakfastSummary = new MealSummary { MealType = "Завтрак" };
            LunchSummary = new MealSummary { MealType = "Обед" };
            DinnerSummary = new MealSummary { MealType = "Ужин" };

            LoadClientsCommand = new RelayCommand(LoadClients);
            CreatePlanCommand = new RelayCommand(CreatePlan, () => HasSelectedClient && !string.IsNullOrWhiteSpace(PlanName));
            CalculateTargetsCommand = new RelayCommand(CalculateTargets, () => HasSelectedClient);
            AddProductToMealCommand = new RelayCommand(AddProductToMeal, () => SelectedProduct != null && SelectedQuantity > 0);
            RemoveProductFromMealCommand = new RelayCommand(RemoveProductFromMeal);
            ClearMealCommand = new RelayCommand(ClearMeal);
            SendPlanToClientCommand = new RelayCommand(SendPlanToClient, () => HasSelectedClient && !string.IsNullOrWhiteSpace(PlanName));
            AutoFillQuantitiesCommand = new RelayCommand(AutoFillQuantities);

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

                using (var context = new DatabaseContext())
                {
                    var clients = await context.Users
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
        /// Загружает все продукты из базы данных
        /// </summary>
        private async void LoadAllProducts()
        {
            try
            {
                AllProducts.Clear();

                using (var context = new DatabaseContext())
                {
                    var products = await context.Products
                        .OrderBy(p => p.ProductName)
                        .ToListAsync();

                    foreach (var product in products)
                    {
                        AllProducts.Add(new ProductDto
                        {
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            Category = product.Category,
                            Calories = product.Calories,
                            Protein = product.Protein,
                            Fat = product.Fat,
                            Carbohydrates = product.Carbohydrates,
                            Unit = product.Unit
                        });
                    }
                }

                if (AllProducts.Any())
                {
                    SelectedProduct = AllProducts.First();
                }

                _mainVM.UpdateStatus($"Загружено {AllProducts.Count} продуктов");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продуктов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

            PlanName = $"План для {SelectedClient.Username} от {DateTime.Now:dd.MM.yyyy}";

            // Очищаем предыдущий план
            BreakfastItems.Clear();
            LunchItems.Clear();
            DinnerItems.Clear();
            UpdateMealSummaries();

            _mainVM.UpdateStatus($"Загружены данные клиента: {SelectedClient.Username}");
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
        /// Добавляет продукт в выбранный прием пищи
        /// </summary>
        private void AddProductToMeal()
        {
            if (SelectedProduct == null || SelectedQuantity <= 0)
            {
                MessageBox.Show("Выберите продукт и укажите количество", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedCollection = GetSelectedMealCollection();
            if (selectedCollection == null)
            {
                MessageBox.Show($"Не удалось определить коллекцию для приема пищи: {SelectedMealType}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Рассчитываем нутриенты для указанного количества
            var multiplier = SelectedQuantity / 100;
            var item = new MealPlanItem
            {
                ProductId = SelectedProduct.ProductId,
                ProductName = SelectedProduct.ProductName,
                Quantity = SelectedQuantity,
                Calories = Math.Round(SelectedProduct.Calories * multiplier, 1),
                Protein = Math.Round(SelectedProduct.Protein * multiplier, 1),
                Fat = Math.Round(SelectedProduct.Fat * multiplier, 1),
                Carbs = Math.Round(SelectedProduct.Carbohydrates * multiplier, 1),
                Notes = MealNotes
            };

            selectedCollection.Add(item);
            UpdateMealSummaries();

            _mainVM.UpdateStatus($"Добавлен {SelectedProduct.ProductName} в {SelectedMealType}");

            // Сбрасываем поля
            SelectedQuantity = 100;
            MealNotes = "";
        }

        /// <summary>
        /// Удаляет продукт из приема пищи
        /// </summary>
        private void RemoveProductFromMeal()
        {
            var selectedCollection = GetSelectedMealCollection();
            if (selectedCollection == null || !selectedCollection.Any()) return;

            // Находим последний добавленный элемент
            var lastItem = selectedCollection.Last();
            selectedCollection.Remove(lastItem);
            UpdateMealSummaries();
            _mainVM.UpdateStatus($"Удален {lastItem.ProductName} из {SelectedMealType}");
        }

        /// <summary>
        /// Очищает выбранный прием пищи
        /// </summary>
        private void ClearMeal()
        {
            var selectedCollection = GetSelectedMealCollection();
            if (selectedCollection == null || !selectedCollection.Any()) return;

            var result = MessageBox.Show($"Очистить {SelectedMealType}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                selectedCollection.Clear();
                UpdateMealSummaries();
                _mainVM.UpdateStatus($"{SelectedMealType} очищен");
            }
        }

        /// <summary>
        /// Автоматически заполняет количества продуктов
        /// </summary>
        private void AutoFillQuantities()
        {
            if (!BreakfastItems.Any() && !LunchItems.Any() && !DinnerItems.Any())
            {
                MessageBox.Show("Сначала добавьте продукты в план", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Устанавливаем стандартные количества
            foreach (var item in BreakfastItems)
            {
                item.Quantity = GetAutoQuantity(item.ProductName, "Завтрак");
                RecalculateItemNutrients(item);
            }

            foreach (var item in LunchItems)
            {
                item.Quantity = GetAutoQuantity(item.ProductName, "Обед");
                RecalculateItemNutrients(item);
            }

            foreach (var item in DinnerItems)
            {
                item.Quantity = GetAutoQuantity(item.ProductName, "Ужин");
                RecalculateItemNutrients(item);
            }

            UpdateMealSummaries();
            _mainVM.UpdateStatus("Количества продуктов автоматически заполнены");
        }

        private decimal GetAutoQuantity(string productName, string mealType)
        {
            // Базовая логика для автоматического определения количества
            if (productName.Contains("каша") || productName.Contains("гречка") || productName.Contains("рис"))
                return mealType == "Завтрак" ? 150 : 120;

            if (productName.Contains("кури") || productName.Contains("мясо") || productName.Contains("рыб"))
                return 150;

            if (productName.Contains("овощ") || productName.Contains("салат"))
                return 200;

            if (productName.Contains("фрукт") || productName.Contains("яблоко") || productName.Contains("банан"))
                return 150;

            return mealType == "Завтрак" ? 100 : 120;
        }

        private void RecalculateItemNutrients(MealPlanItem item)
        {
            var product = AllProducts.FirstOrDefault(p => p.ProductId == item.ProductId);
            if (product != null)
            {
                var multiplier = item.Quantity / 100;
                item.Calories = Math.Round(product.Calories * multiplier, 1);
                item.Protein = Math.Round(product.Protein * multiplier, 1);
                item.Fat = Math.Round(product.Fat * multiplier, 1);
                item.Carbs = Math.Round(product.Carbohydrates * multiplier, 1);
            }
        }

        private void AddProductToCollection(ObservableCollection<MealPlanItem> collection, ProductDto product, decimal quantity, string notes)
        {
            if (product == null) return;

            var multiplier = quantity / 100;
            collection.Add(new MealPlanItem
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Quantity = quantity,
                Calories = Math.Round(product.Calories * multiplier, 1),
                Protein = Math.Round(product.Protein * multiplier, 1),
                Fat = Math.Round(product.Fat * multiplier, 1),
                Carbs = Math.Round(product.Carbohydrates * multiplier, 1),
                Notes = notes
            });
        }

        /// <summary>
        /// Обновляет сводки по приемам пищи
        /// </summary>
        private void UpdateMealSummaries()
        {
            BreakfastSummary?.UpdateFromItems(BreakfastItems);
            LunchSummary?.UpdateFromItems(LunchItems);
            DinnerSummary?.UpdateFromItems(DinnerItems);
        }

        /// <summary>
        /// Возвращает коллекцию для выбранного приема пищи
        /// </summary>
        private ObservableCollection<MealPlanItem> GetSelectedMealCollection()
        {
            if (string.IsNullOrEmpty(SelectedMealType))
            {
                // Если SelectedMealType пустой, возвращаем по CurrentMealTab
                switch (CurrentMealTab)
                {
                    case "Завтрак":
                        return BreakfastItems;
                    case "Обед":
                        return LunchItems;
                    case "Ужин":
                        return DinnerItems;
                    default:
                        return BreakfastItems;
                }
            }

            switch (SelectedMealType)
            {
                case "Завтрак":
                    return BreakfastItems;
                case "Обед":
                    return LunchItems;
                case "Ужин":
                    return DinnerItems;
                default:
                    // Если значение неправильное, используем вкладку по умолчанию
                    return BreakfastItems;
            }
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

                // Проверяем, есть ли продукты в плане
                if (!BreakfastItems.Any() && !LunchItems.Any() && !DinnerItems.Any())
                {
                    var result = MessageBox.Show("План не содержит продуктов. Создать пустой план?",
                        "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }

                using (var context = new DatabaseContext())
                {
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
                        Status = "Новый", // Всегда создаем с статусом "Новый"
                        Description = Description,
                        CreatedDate = DateTime.Now
                    };

                    context.NutritionPlans.Add(plan);
                    await context.SaveChangesAsync();

                    // Сохраняем продукты плана
                    await SavePlanProducts(context, plan.PlanId);

                    _mainVM.UpdateStatus($"Создан план '{PlanName}' для клиента {SelectedClient.Username}");

                    MessageBox.Show($"План питания успешно создан!\n\n" +
                                  $"Клиент: {SelectedClient.Username}\n" +
                                  $"Название: {PlanName}\n" +
                                  $"Период: {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}\n" +
                                  $"Калории: {TargetCalories} ккал/день\n" +
                                  $"Статус: НОВЫЙ (требуется отправить клиенту)\n\n" +
                                  $"Завтрак: {BreakfastItems.Count} продуктов\n" +
                                  $"Обед: {LunchItems.Count} продуктов\n" +
                                  $"Ужин: {DinnerItems.Count} продуктов",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очищаем форму
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка создания плана: {ex.Message}");
                MessageBox.Show($"Ошибка создания плана: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сохраняет продукты плана в базу данных
        /// </summary>
        private async Task SavePlanProducts(DatabaseContext context, int planId)
        {
            // Сохраняем продукты завтрака
            foreach (var item in BreakfastItems)
            {
                var planProduct = new PlanProduct
                {
                    PlanId = planId,
                    ProductId = item.ProductId,
                    MealType = "Завтрак",
                    Quantity = item.Quantity,
                    Notes = item.Notes
                };
                context.PlanProducts.Add(planProduct);
            }

            // Сохраняем продукты обеда
            foreach (var item in LunchItems)
            {
                var planProduct = new PlanProduct
                {
                    PlanId = planId,
                    ProductId = item.ProductId,
                    MealType = "Обед",
                    Quantity = item.Quantity,
                    Notes = item.Notes
                };
                context.PlanProducts.Add(planProduct);
            }

            // Сохраняем продукты ужина
            foreach (var item in DinnerItems)
            {
                var planProduct = new PlanProduct
                {
                    PlanId = planId,
                    ProductId = item.ProductId,
                    MealType = "Ужин",
                    Quantity = item.Quantity,
                    Notes = item.Notes
                };
                context.PlanProducts.Add(planProduct);
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Отправляет план клиенту (меняет статус на "Отправлен")
        /// </summary>
        private async void SendPlanToClient()
        {
            try
            {
                var result = MessageBox.Show(
                    $"Отправить план '{PlanName}' клиенту {SelectedClient.Username}?\n\n" +
                    "Клиент получит уведомление о новом плане питания.",
                    "Отправка плана клиенту",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new DatabaseContext())
                    {
                        // Находим последний созданный план для этого клиента с таким же названием
                        var lastPlan = await context.NutritionPlans
                            .Where(p => p.UserId == SelectedClient.UserId)
                            .OrderByDescending(p => p.CreatedDate)
                            .FirstOrDefaultAsync();

                        if (lastPlan != null)
                        {
                            lastPlan.Status = "Отправлен";
                            await context.SaveChangesAsync();

                            _mainVM.UpdateStatus($"План '{lastPlan.PlanName}' отправлен клиенту {SelectedClient.Username}");
                            MessageBox.Show($"План успешно отправлен клиенту!\n\n" +
                                          "Клиент увидит план в своем разделе 'Мои планы питания'.",
                                          "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Сначала создайте план, затем отправьте его клиенту.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки плана: {ex.Message}",
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
            Status = "Новый";
            Description = "";
            BreakfastItems.Clear();
            LunchItems.Clear();
            DinnerItems.Clear();
            MealNotes = "";
            UpdateMealSummaries();

            if (SelectedClient != null)
            {
                LoadClientData(); // Загружаем данные клиента заново
            }
        }
    }

    /// <summary>
    /// Класс для элемента плана питания
    /// </summary>
    public class MealPlanItem : BaseViewModel
    {
        private int _productId;
        private string _productName = string.Empty;
        private decimal _quantity;
        private decimal _calories;
        private decimal _protein;
        private decimal _fat;
        private decimal _carbs;
        private string _notes = string.Empty;

        public int ProductId
        {
            get => _productId;
            set { _productId = value; OnPropertyChanged(); }
        }

        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); }
        }

        public decimal Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); }
        }

        public decimal Calories
        {
            get => _calories;
            set { _calories = value; OnPropertyChanged(); }
        }

        public decimal Protein
        {
            get => _protein;
            set { _protein = value; OnPropertyChanged(); }
        }

        public decimal Fat
        {
            get => _fat;
            set { _fat = value; OnPropertyChanged(); }
        }

        public decimal Carbs
        {
            get => _carbs;
            set { _carbs = value; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }
    }

    /// <summary>
    /// Класс для сводки по приему пищи
    /// </summary>
    public class MealSummary : BaseViewModel
    {
        private string _mealType = string.Empty;
        private decimal _totalCalories;
        private decimal _totalProtein;
        private decimal _totalFat;
        private decimal _totalCarbs;

        public string MealType
        {
            get => _mealType;
            set { _mealType = value; OnPropertyChanged(); }
        }

        public decimal TotalCalories
        {
            get => _totalCalories;
            set { _totalCalories = value; OnPropertyChanged(); }
        }

        public decimal TotalProtein
        {
            get => _totalProtein;
            set { _totalProtein = value; OnPropertyChanged(); }
        }

        public decimal TotalFat
        {
            get => _totalFat;
            set { _totalFat = value; OnPropertyChanged(); }
        }

        public decimal TotalCarbs
        {
            get => _totalCarbs;
            set { _totalCarbs = value; OnPropertyChanged(); }
        }

        public void UpdateFromItems(ObservableCollection<MealPlanItem> items)
        {
            if (items == null) return;

            TotalCalories = items.Sum(i => i.Calories);
            TotalProtein = items.Sum(i => i.Protein);
            TotalFat = items.Sum(i => i.Fat);
            TotalCarbs = items.Sum(i => i.Carbs);
        }
    }
}
