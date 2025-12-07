using NutriPlanner.Data;
using NutriPlanner.Models.DTO;
using NutriPlanner.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// ViewModel для дневника питания пользователя
    /// Только просмотр и добавление СВОИХ записей
    /// </summary>
    public class DailyNutritionViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DatabaseContext _context;
        private readonly User _currentUser;

        private DailyNutritionDto _nutritionData;
        private ObservableCollection<ProductDto> _availableProducts;
        private ProductDto _selectedProduct;
        private decimal _productQuantity = 100;

        public DailyNutritionDto NutritionData
        {
            get => _nutritionData;
            set { _nutritionData = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ProductDto> AvailableProducts
        {
            get => _availableProducts;
            set { _availableProducts = value; OnPropertyChanged(); }
        }

        public ProductDto SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public decimal ProductQuantity
        {
            get => _productQuantity;
            set { _productQuantity = value; OnPropertyChanged(); }
        }

        // Команды
        public ICommand AddProductCommand { get; }
        public ICommand ClearDataCommand { get; }
        public ICommand SaveToDatabaseCommand { get; }
        public ICommand LoadTodayDataCommand { get; }

        public DailyNutritionViewModel(MainViewModel mainViewModel, User currentUser)
        {
            _mainViewModel = mainViewModel;
            _context = new DatabaseContext();
            _currentUser = currentUser;

            _nutritionData = new DailyNutritionDto();
            _availableProducts = new ObservableCollection<ProductDto>();

            // Инициализация команд
            AddProductCommand = new RelayCommand(AddProduct, CanAddProduct);
            ClearDataCommand = new RelayCommand(ClearData);
            SaveToDatabaseCommand = new RelayCommand(SaveToDatabase);
            LoadTodayDataCommand = new RelayCommand(LoadTodayData);

            InitializeData();
        }

        /// <summary>
        /// Инициализирует начальные данные
        /// </summary>
        private async void InitializeData()
        {
            try
            {
                await LoadProductsFromDatabase();
                LoadTodayData();
                InitializeTargets();
                _mainViewModel.UpdateStatus("Дневник загружен");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка загрузки: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает продукты из базы данных
        /// </summary>
        private async Task LoadProductsFromDatabase()
        {
            var products = await _context.Products.ToListAsync();
            AvailableProducts.Clear();

            foreach (var product in products)
            {
                AvailableProducts.Add(new ProductDto
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

            if (AvailableProducts.Any())
                SelectedProduct = AvailableProducts.First();
        }

        /// <summary>
        /// Загружает сегодняшние записи пользователя
        /// </summary>
        private void LoadTodayData()
        {
            try
            {
                var todayEntries = _context.FoodDiaries
                    .Include(fd => fd.Product)
                    .Where(fd => fd.UserId == _currentUser.UserId &&
                                fd.Date.Date == DateTime.Today)
                    .ToList();

                NutritionData = new DailyNutritionDto();
                NutritionData.Meals.Clear();

                foreach (var entry in todayEntries)
                {
                    NutritionData.TotalCalories += entry.Calories;
                    NutritionData.TotalProtein += entry.Protein;
                    NutritionData.TotalFat += entry.Fat;
                    NutritionData.TotalCarbs += entry.Carbohydrates;

                    NutritionData.Meals.Add(new MealDto
                    {
                        MealName = entry.Product?.ProductName ?? "Неизвестный продукт",
                        Calories = entry.Calories,
                        Protein = entry.Protein,
                        Fat = entry.Fat,
                        Carbs = entry.Carbohydrates,
                        MealTime = entry.Date
                    });
                }

                UpdateProgress();
                _mainViewModel.UpdateStatus($"Загружено {todayEntries.Count} записей за сегодня");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка загрузки: {ex.Message}");
            }
        }

        /// <summary>
        /// Инициализирует целевые показатели из профиля пользователя
        /// </summary>
        private void InitializeTargets()
        {
            NutritionData.TargetCalories = _currentUser.DailyCalorieTarget;
            NutritionData.TargetProtein = _currentUser.DailyProteinTarget;
            NutritionData.TargetFat = _currentUser.DailyFatTarget;
            NutritionData.TargetCarbs = _currentUser.DailyCarbsTarget;

            UpdateProgress();
        }

        /// <summary>
        /// Обновляет прогресс выполнения целей
        /// </summary>
        private void UpdateProgress()
        {
            if (NutritionData.TargetCalories > 0)
                NutritionData.CaloriesProgress = Math.Round((NutritionData.TotalCalories / NutritionData.TargetCalories) * 100, 1);

            if (NutritionData.TargetProtein > 0)
                NutritionData.ProteinProgress = Math.Round((NutritionData.TotalProtein / NutritionData.TargetProtein) * 100, 1);

            if (NutritionData.TargetFat > 0)
                NutritionData.FatProgress = Math.Round((NutritionData.TotalFat / NutritionData.TargetFat) * 100, 1);

            if (NutritionData.TargetCarbs > 0)
                NutritionData.CarbsProgress = Math.Round((NutritionData.TotalCarbs / NutritionData.TargetCarbs) * 100, 1);
        }

        /// <summary>
        /// Добавляет продукт в дневник питания
        /// </summary>
        private void AddProduct()
        {
            if (SelectedProduct == null) return;

            try
            {
                var multiplier = ProductQuantity / 100;
                var calories = Math.Round(SelectedProduct.Calories * multiplier, 2);
                var protein = Math.Round(SelectedProduct.Protein * multiplier, 2);
                var fat = Math.Round(SelectedProduct.Fat * multiplier, 2);
                var carbs = Math.Round(SelectedProduct.Carbohydrates * multiplier, 2);

                // Обновляем общие показатели
                NutritionData.TotalCalories += calories;
                NutritionData.TotalProtein += protein;
                NutritionData.TotalFat += fat;
                NutritionData.TotalCarbs += carbs;

                // Добавляем запись в список приемов пищи
                NutritionData.Meals.Add(new MealDto
                {
                    MealName = $"{SelectedProduct.ProductName} ({ProductQuantity}г)",
                    Calories = calories,
                    Protein = protein,
                    Fat = fat,
                    Carbs = carbs,
                    MealTime = DateTime.Now
                });

                UpdateProgress();
                OnPropertyChanged(nameof(NutritionData));

                _mainViewModel.UpdateStatus($"Добавлен: {SelectedProduct.ProductName}");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохраняет данные в базу данных
        /// </summary>
        private async void SaveToDatabase()
        {
            try
            {
                var foodEntry = new FoodDiary
                {
                    Date = DateTime.Now,
                    ProductId = SelectedProduct?.ProductId,
                    Quantity = ProductQuantity,
                    Calories = NutritionData.TotalCalories,
                    Protein = NutritionData.TotalProtein,
                    Fat = NutritionData.TotalFat,
                    Carbohydrates = NutritionData.TotalCarbs,
                    UserId = _currentUser.UserId // Только свой ID!
                };

                await _context.FoodDiaries.AddAsync(foodEntry);
                await _context.SaveChangesAsync();

                _mainViewModel.UpdateStatus("Данные сохранены в дневник");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка сохранения: {ex.Message}");
            }
        }

        /// <summary>
        /// Очищает данные дневника (только на клиенте, не в БД)
        /// </summary>
        private void ClearData()
        {
            NutritionData = new DailyNutritionDto();
            InitializeTargets();
            OnPropertyChanged(nameof(NutritionData));
            _mainViewModel.UpdateStatus("Дневник очищен");
        }

        private bool CanAddProduct() => SelectedProduct != null && ProductQuantity > 0;
    }
}
