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
using System.Windows;

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// ViewModel для дневника питания пользователя
    /// </summary>
    public class DailyNutritionViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DatabaseContext _context;
        private readonly User _currentUser;

        private DailyNutritionDto _dailyNutrition;
        private ObservableCollection<ProductDto> _availableProducts;
        private ProductDto _selectedProduct;
        private decimal _productQuantity = 100;
        private DateTime _selectedDate = DateTime.Today;
        private ObservableCollection<MealDto> _meals;

        public DailyNutritionDto DailyNutrition
        {
            get => _dailyNutrition;
            set { _dailyNutrition = value; OnPropertyChanged(); }
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

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                LoadDayData();
            }
        }

        public ObservableCollection<MealDto> Meals
        {
            get => _meals;
            set { _meals = value; OnPropertyChanged(); }
        }

        // Команды
        public ICommand AddProductCommand { get; }
        public ICommand ClearDayCommand { get; }
        public ICommand SaveToDatabaseCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand TodayCommand { get; }

        public DailyNutritionViewModel(MainViewModel mainViewModel, User currentUser)
        {
            _mainViewModel = mainViewModel;
            _context = new DatabaseContext();
            _currentUser = currentUser;

            DailyNutrition = new DailyNutritionDto();
            AvailableProducts = new ObservableCollection<ProductDto>();
            Meals = new ObservableCollection<MealDto>();

            // Инициализация команд
            AddProductCommand = new RelayCommand(AddProduct, CanAddProduct);
            ClearDayCommand = new RelayCommand(ClearDay);
            SaveToDatabaseCommand = new RelayCommand(SaveToDatabase);
            PreviousDayCommand = new RelayCommand(PreviousDay);
            NextDayCommand = new RelayCommand(NextDay);
            TodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);

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
                LoadDayData();
                InitializeTargets();
                _mainViewModel.UpdateStatus("Дневник загружен");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка загрузки: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки дневника: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает продукты из базы данных
        /// </summary>
        private async Task LoadProductsFromDatabase()
        {
            try
            {
                var products = await _context.Products
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продуктов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает данные за выбранный день
        /// </summary>
        private async void LoadDayData()
        {
            try
            {
                Meals.Clear();
                DailyNutrition = new DailyNutritionDto();

                // Загружаем записи за выбранный день
                var dayEntries = await _context.FoodDiaries
                    .Include(fd => fd.Product)
                    .Where(fd => fd.UserId == _currentUser.UserId &&
                                fd.Date.Date == SelectedDate.Date)
                    .OrderBy(fd => fd.Date)
                    .ToListAsync();

                decimal totalCalories = 0;
                decimal totalProtein = 0;
                decimal totalFat = 0;
                decimal totalCarbs = 0;

                foreach (var entry in dayEntries)
                {
                    var meal = new MealDto
                    {
                        MealName = entry.Product?.ProductName ?? "Неизвестный продукт",
                        Calories = entry.Calories,
                        Protein = entry.Protein,
                        Fat = entry.Fat,
                        Carbs = entry.Carbohydrates,
                        MealTime = entry.Date,
                        Quantity = entry.Quantity
                    };

                    Meals.Add(meal);

                    totalCalories += entry.Calories;
                    totalProtein += entry.Protein;
                    totalFat += entry.Fat;
                    totalCarbs += entry.Carbohydrates;
                }

                // Обновляем общие показатели
                DailyNutrition.TotalCalories = totalCalories;
                DailyNutrition.TotalProtein = totalProtein;
                DailyNutrition.TotalFat = totalFat;
                DailyNutrition.TotalCarbs = totalCarbs;

                // Инициализируем цели из профиля пользователя
                InitializeTargets();

                // Рассчитываем прогресс
                UpdateProgress();

                _mainViewModel.UpdateStatus($"Загружено {Meals.Count} записей за {SelectedDate:dd.MM.yyyy}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Инициализирует целевые показатели
        /// </summary>
        private void InitializeTargets()
        {
            DailyNutrition.TargetCalories = _currentUser.DailyCalorieTarget;
            DailyNutrition.TargetProtein = _currentUser.DailyProteinTarget;
            DailyNutrition.TargetFat = _currentUser.DailyFatTarget;
            DailyNutrition.TargetCarbs = _currentUser.DailyCarbsTarget;
        }

        /// <summary>
        /// Обновляет прогресс выполнения целей
        /// </summary>
        private void UpdateProgress()
        {
            if (DailyNutrition.TargetCalories > 0)
                DailyNutrition.CaloriesProgress = Math.Round((DailyNutrition.TotalCalories / DailyNutrition.TargetCalories) * 100, 1);

            if (DailyNutrition.TargetProtein > 0)
                DailyNutrition.ProteinProgress = Math.Round((DailyNutrition.TotalProtein / DailyNutrition.TargetProtein) * 100, 1);

            if (DailyNutrition.TargetFat > 0)
                DailyNutrition.FatProgress = Math.Round((DailyNutrition.TotalFat / DailyNutrition.TargetFat) * 100, 1);

            if (DailyNutrition.TargetCarbs > 0)
                DailyNutrition.CarbsProgress = Math.Round((DailyNutrition.TotalCarbs / DailyNutrition.TargetCarbs) * 100, 1);
        }

        /// <summary>
        /// Добавляет продукт в дневник
        /// </summary>
        private async void AddProduct()
        {
            if (SelectedProduct == null || ProductQuantity <= 0) return;

            try
            {
                var multiplier = ProductQuantity / 100;
                var calories = Math.Round(SelectedProduct.Calories * multiplier, 2);
                var protein = Math.Round(SelectedProduct.Protein * multiplier, 2);
                var fat = Math.Round(SelectedProduct.Fat * multiplier, 2);
                var carbs = Math.Round(SelectedProduct.Carbohydrates * multiplier, 2);

                // Создаем запись в дневнике
                var foodEntry = new FoodDiary
                {
                    UserId = _currentUser.UserId,
                    Date = DateTime.Now,
                    ProductId = SelectedProduct.ProductId,
                    Quantity = ProductQuantity,
                    Calories = calories,
                    Protein = protein,
                    Fat = fat,
                    Carbohydrates = carbs
                };

                await _context.FoodDiaries.AddAsync(foodEntry);
                await _context.SaveChangesAsync();

                // Обновляем отображение
                LoadDayData();

                _mainViewModel.UpdateStatus($"Добавлен: {SelectedProduct.ProductName} ({ProductQuantity}г)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления продукта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сохраняет текущее состояние (уже не нужно, так как сохраняется при добавлении)
        /// </summary>
        private void SaveToDatabase()
        {
            _mainViewModel.UpdateStatus("Данные автоматически сохраняются при добавлении");
        }

        /// <summary>
        /// Очищает все записи за выбранный день
        /// </summary>
        private async void ClearDay()
        {
            var result = MessageBox.Show($"Удалить все записи за {SelectedDate:dd.MM.yyyy}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var entriesToDelete = await _context.FoodDiaries
                        .Where(fd => fd.UserId == _currentUser.UserId &&
                                    fd.Date.Date == SelectedDate.Date)
                        .ToListAsync();

                    _context.FoodDiaries.RemoveRange(entriesToDelete);
                    await _context.SaveChangesAsync();

                    LoadDayData();
                    _mainViewModel.UpdateStatus($"Записи за {SelectedDate:dd.MM.yyyy} удалены");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления записей: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);
        private void NextDay() => SelectedDate = SelectedDate.AddDays(1);

        private bool CanAddProduct() => SelectedProduct != null && ProductQuantity > 0;
    }
}
