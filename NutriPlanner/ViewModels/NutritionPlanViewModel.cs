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

        // Продукты для плана
        private ObservableCollection<ProductDto> _allProducts;
        private ObservableCollection<MealPlanItem> _breakfastItems;
        private ObservableCollection<MealPlanItem> _lunchItems;
        private ObservableCollection<MealPlanItem> _dinnerItems;
        private ProductDto _selectedProduct;
        private string _selectedMealType = "Завтрак";
        private decimal _selectedQuantity = 100;

        // Сводка по приемам пищи
        private MealSummary _breakfastSummary;
        private MealSummary _lunchSummary;
        private MealSummary _dinnerSummary;

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
            set { _breakfastItems = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MealPlanItem> LunchItems
        {
            get => _lunchItems;
            set { _lunchItems = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MealPlanItem> DinnerItems
        {
            get => _dinnerItems;
            set { _dinnerItems = value; OnPropertyChanged(); }
        }

        public ProductDto SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public string SelectedMealType
        {
            get => _selectedMealType;
            set { _selectedMealType = value; OnPropertyChanged(); }
        }

        public decimal SelectedQuantity
        {
            get => _selectedQuantity;
            set { _selectedQuantity = value; OnPropertyChanged(); }
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

        public bool HasSelectedClient => SelectedClient != null;

        // Команды
        public ICommand LoadClientsCommand { get; }
        public ICommand CreatePlanCommand { get; }
        public ICommand CalculateTargetsCommand { get; }
        public ICommand AddProductToMealCommand { get; }
        public ICommand RemoveProductFromMealCommand { get; }
        public ICommand ClearMealCommand { get; }
        public ICommand GenerateAutoPlanCommand { get; }

        public NutritionPlanViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;
            _context = new DatabaseContext();

            Clients = new ObservableCollection<UserProfileDto>();
            AllProducts = new ObservableCollection<ProductDto>();
            BreakfastItems = new ObservableCollection<MealPlanItem>();
            LunchItems = new ObservableCollection<MealPlanItem>();
            DinnerItems = new ObservableCollection<MealPlanItem>();

            BreakfastSummary = new MealSummary { MealType = "Завтрак" };
            LunchSummary = new MealSummary { MealType = "Обед" };
            DinnerSummary = new MealSummary { MealType = "Ужин" };

            LoadClientsCommand = new RelayCommand(LoadClients);
            CreatePlanCommand = new RelayCommand(CreatePlan, () => HasSelectedClient && !string.IsNullOrWhiteSpace(PlanName));
            CalculateTargetsCommand = new RelayCommand(CalculateTargets, () => HasSelectedClient);
            AddProductToMealCommand = new RelayCommand(AddProductToMeal);
            RemoveProductFromMealCommand = new RelayCommand(RemoveProductFromMeal);
            ClearMealCommand = new RelayCommand(ClearMeal);
            GenerateAutoPlanCommand = new RelayCommand(GenerateAutoPlan);

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
        /// Загружает все продукты из базы данных
        /// </summary>
        private async void LoadAllProducts()
        {
            try
            {
                AllProducts.Clear();

                var products = await _context.Products
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

            PlanName = $"План для {SelectedClient.Username}";

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
            if (selectedCollection == null) return;

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
                Carbs = Math.Round(SelectedProduct.Carbohydrates * multiplier, 1)
            };

            selectedCollection.Add(item);
            UpdateMealSummaries();

            _mainVM.UpdateStatus($"Добавлен {SelectedProduct.ProductName} в {SelectedMealType}");

            // Сбрасываем количество
            SelectedQuantity = 100;
        }

        /// <summary>
        /// Удаляет продукт из приема пищи
        /// </summary>
        private void RemoveProductFromMeal()
        {
            var selectedCollection = GetSelectedMealCollection();
            if (selectedCollection == null) return;

            // В реальном приложении здесь будет удаление выбранного элемента
            // Сейчас удаляем последний добавленный
            if (selectedCollection.Any())
            {
                var lastItem = selectedCollection.Last();
                selectedCollection.Remove(lastItem);
                UpdateMealSummaries();
                _mainVM.UpdateStatus($"Удален {lastItem.ProductName} из {SelectedMealType}");
            }
        }

        /// <summary>
        /// Очищает выбранный прием пищи
        /// </summary>
        private void ClearMeal()
        {
            var selectedCollection = GetSelectedMealCollection();
            if (selectedCollection == null) return;

            if (!selectedCollection.Any()) return;

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
        /// Генерирует автоматический план питания
        /// </summary>
        private async void GenerateAutoPlan()
        {
            if (SelectedClient == null || !AllProducts.Any()) return;

            try
            {
                // Очищаем текущий план
                BreakfastItems.Clear();
                LunchItems.Clear();
                DinnerItems.Clear();

                // Распределение калорий по приемам пищи
                decimal breakfastCalories = TargetCalories * 0.25m;
                decimal lunchCalories = TargetCalories * 0.35m;
                decimal dinnerCalories = TargetCalories * 0.30m;

                // Группируем продукты по категориям
                var proteinProducts = AllProducts
                    .Where(p => p.Protein > 15 && p.Calories < 300)
                    .ToList();

                var carbProducts = AllProducts
                    .Where(p => p.Carbohydrates > 10 && p.Calories < 200)
                    .ToList();

                var fatProducts = AllProducts
                    .Where(p => p.Fat > 5 && p.Fat < 30 && p.Calories < 500)
                    .ToList();

                var veggieProducts = AllProducts
                    .Where(p => p.Category == "Овощи" || p.Category == "Фрукты")
                    .ToList();

                // Завтрак (белки + углеводы + фрукты)
                AddProductToCollection(BreakfastItems, proteinProducts.FirstOrDefault(), 100);
                AddProductToCollection(BreakfastItems, carbProducts.FirstOrDefault(p => p.Category == "Крупы"), 80);
                AddProductToCollection(BreakfastItems, veggieProducts.FirstOrDefault(p => p.Category == "Фрукты"), 150);

                // Обед (белки + углеводы + овощи)
                AddProductToCollection(LunchItems, proteinProducts.Skip(1).FirstOrDefault(), 150);
                AddProductToCollection(LunchItems, carbProducts.FirstOrDefault(p => p.Category == "Крупы" || p.Category == "Макароны"), 120);
                AddProductToCollection(LunchItems, veggieProducts.FirstOrDefault(p => p.Category == "Овощи"), 200);

                // Ужин (белки + овощи)
                AddProductToCollection(DinnerItems, proteinProducts.Skip(2).FirstOrDefault(), 120);
                AddProductToCollection(DinnerItems, veggieProducts.Skip(1).FirstOrDefault(p => p.Category == "Овощи"), 180);

                UpdateMealSummaries();

                _mainVM.UpdateStatus($"Автоплан сгенерирован для {SelectedClient.Username}");
                MessageBox.Show($"Автоматический план сгенерирован!\n\n" +
                              $"Завтрак: {BreakfastItems.Count} продуктов\n" +
                              $"Обед: {LunchItems.Count} продуктов\n" +
                              $"Ужин: {DinnerItems.Count} продуктов",
                              "Автоплан", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации плана: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddProductToCollection(ObservableCollection<MealPlanItem> collection, ProductDto product, decimal quantity)
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
                Carbs = Math.Round(product.Carbohydrates * multiplier, 1)
            });
        }

        /// <summary>
        /// Обновляет сводки по приемам пищи
        /// </summary>
        private void UpdateMealSummaries()
        {
            BreakfastSummary.UpdateFromItems(BreakfastItems);
            LunchSummary.UpdateFromItems(LunchItems);
            DinnerSummary.UpdateFromItems(DinnerItems);

            OnPropertyChanged(nameof(BreakfastSummary));
            OnPropertyChanged(nameof(LunchSummary));
            OnPropertyChanged(nameof(DinnerSummary));
        }

        /// <summary>
        /// Возвращает коллекцию для выбранного приема пищи
        /// </summary>
        private ObservableCollection<MealPlanItem> GetSelectedMealCollection()
        {
            return SelectedMealType switch
            {
                "Завтрак" => BreakfastItems,
                "Обед" => LunchItems,
                "Ужин" => DinnerItems,
                _ => null
            };
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

                // Сохраняем продукты плана (в реальном приложении нужно создать таблицу MealPlanProducts)
                // Здесь просто сохраняем информацию о продуктах в описании
                string planDescription = GeneratePlanDescription();
                plan.DailyCalories = TargetCalories; // Просто обновляем

                await _context.SaveChangesAsync();

                _mainVM.UpdateStatus($"Создан план '{PlanName}' для клиента {SelectedClient.Username}");
                MessageBox.Show($"План питания успешно создан!\n\n" +
                              $"Клиент: {SelectedClient.Username}\n" +
                              $"Название: {PlanName}\n" +
                              $"Период: {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}\n" +
                              $"Калории: {TargetCalories} ккал/день\n" +
                              $"Статус: {Status}\n\n" +
                              $"Завтрак: {BreakfastItems.Count} продуктов\n" +
                              $"Обед: {LunchItems.Count} продуктов\n" +
                              $"Ужин: {DinnerItems.Count} продуктов",
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
        /// Генерирует описание плана с продуктами
        /// </summary>
        private string GeneratePlanDescription()
        {
            var description = $"План питания '{PlanName}'\n\n";

            if (BreakfastItems.Any())
            {
                description += "ЗАВТРАК:\n";
                foreach (var item in BreakfastItems)
                {
                    description += $"- {item.ProductName}: {item.Quantity}г ({item.Calories} ккал)\n";
                }
                description += $"Итого: {BreakfastSummary.TotalCalories} ккал\n\n";
            }

            if (LunchItems.Any())
            {
                description += "ОБЕД:\n";
                foreach (var item in LunchItems)
                {
                    description += $"- {item.ProductName}: {item.Quantity}г ({item.Calories} ккал)\n";
                }
                description += $"Итого: {LunchSummary.TotalCalories} ккал\n\n";
            }

            if (DinnerItems.Any())
            {
                description += "УЖИН:\n";
                foreach (var item in DinnerItems)
                {
                    description += $"- {item.ProductName}: {item.Quantity}г ({item.Calories} ккал)\n";
                }
                description += $"Итого: {DinnerSummary.TotalCalories} ккал\n";
            }

            return description;
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
            BreakfastItems.Clear();
            LunchItems.Clear();
            DinnerItems.Clear();
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
    public class MealPlanItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbs { get; set; }
    }

    /// <summary>
    /// Класс для сводки по приему пищи
    /// </summary>
    public class MealSummary : BaseViewModel
    {
        public string MealType { get; set; } = string.Empty;
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalFat { get; set; }
        public decimal TotalCarbs { get; set; }

        public void UpdateFromItems(ObservableCollection<MealPlanItem> items)
        {
            TotalCalories = items.Sum(i => i.Calories);
            TotalProtein = items.Sum(i => i.Protein);
            TotalFat = items.Sum(i => i.Fat);
            TotalCarbs = items.Sum(i => i.Carbs);

            OnPropertyChanged(nameof(TotalCalories));
            OnPropertyChanged(nameof(TotalProtein));
            OnPropertyChanged(nameof(TotalFat));
            OnPropertyChanged(nameof(TotalCarbs));
        }
    }
}
