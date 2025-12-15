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
using System.Globalization;

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// ViewModel для дневника питания с разделением по приемам пищи
    /// </summary>
    public class DailyNutritionViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly User _currentUser;
        private DailyNutritionDto _dailyNutrition;
        private ObservableCollection<ProductDto> _availableProducts;
        private ProductDto _selectedProduct;
        private string _productQuantityInput = "100";
        private DateTime _selectedDate = DateTime.Today;
        private ObservableCollection<MealDto> _meals;
        private bool _isLoading;

        // Новые поля для управления приемами пищи
        private string _selectedMealType = "Завтрак";
        private ObservableCollection<FoodEntryDto> _currentMealEntries;
        private Dictionary<string, ObservableCollection<FoodEntryDto>> _mealEntries;
        private FoodEntryDto _selectedEntry;

        public ObservableCollection<string> MealTypes { get; } = new ObservableCollection<string>
        {
            "Завтрак",
            "Обед",
            "Ужин"
        };

        // Публичное свойство для доступа из XAML
        public Dictionary<string, ObservableCollection<FoodEntryDto>> MealEntries
        {
            get => _mealEntries;
            private set
            {
                _mealEntries = value;
                OnPropertyChanged();
            }
        }

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

        public string ProductQuantityInput
        {
            get => _productQuantityInput;
            set
            {
                _productQuantityInput = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProductQuantity));
                OnPropertyChanged(nameof(IsValidQuantity));
            }
        }

        public decimal ProductQuantity
        {
            get
            {
                if (decimal.TryParse(ProductQuantityInput, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                    return result;
                return 0;
            }
        }

        public bool IsValidQuantity => ProductQuantity > 0 && ProductQuantity <= 10000;

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

        public string SelectedMealType
        {
            get => _selectedMealType;
            set
            {
                _selectedMealType = value;
                OnPropertyChanged();
                if (MealEntries.ContainsKey(value))
                {
                    CurrentMealEntries = MealEntries[value];
                }
            }
        }

        public ObservableCollection<FoodEntryDto> CurrentMealEntries
        {
            get => _currentMealEntries;
            set
            {
                _currentMealEntries = value;
                OnPropertyChanged();
            }
        }

        public FoodEntryDto SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                _selectedEntry = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedEntry));
            }
        }

        public bool HasSelectedEntry => SelectedEntry != null;

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Вспомогательные свойства для безопасных привязок
        public MealDto BreakfastMeal => GetMealDto("Завтрак");
        public MealDto LunchMeal => GetMealDto("Обед");
        public MealDto DinnerMeal => GetMealDto("Ужин");

        // Команды
        public ICommand AddProductToMealCommand { get; }
        public ICommand ClearMealCommand { get; }
        public ICommand ClearDayCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand RemoveEntryCommand { get; }

        public DailyNutritionViewModel(MainViewModel mainViewModel, User currentUser)
        {
            _mainViewModel = mainViewModel;
            _currentUser = currentUser;

            DailyNutrition = new DailyNutritionDto();
            AvailableProducts = new ObservableCollection<ProductDto>();
            Meals = new ObservableCollection<MealDto>();
            MealEntries = new Dictionary<string, ObservableCollection<FoodEntryDto>>();

            foreach (var mealType in MealTypes)
            {
                MealEntries[mealType] = new ObservableCollection<FoodEntryDto>();
            }

            CurrentMealEntries = MealEntries["Завтрак"];

            AddProductToMealCommand = new RelayCommand(AddProductToMeal, CanAddProduct);
            ClearMealCommand = new RelayCommand(ClearCurrentMeal, () => CurrentMealEntries != null && CurrentMealEntries.Any());
            ClearDayCommand = new RelayCommand(ClearDay);
            PreviousDayCommand = new RelayCommand(PreviousDay);
            NextDayCommand = new RelayCommand(NextDay);
            TodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);
            RemoveEntryCommand = new RelayCommand(RemoveSelectedEntry, () => HasSelectedEntry);

            InitializeData();
        }

        private async void InitializeData()
        {
            try
            {
                IsLoading = true;
                await LoadProductsFromDatabase();
                await LoadDayDataAsync();
                InitializeTargets();
                _mainViewModel.UpdateStatus("Дневник загружен");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка загрузки: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки дневника: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadProductsFromDatabase()
        {
            try
            {
                using var context = new DatabaseContext();
                AvailableProducts.Clear();

                var products = await context.Products
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

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

        public async Task LoadDayDataAsync()
        {
            try
            {
                IsLoading = true;
                using var context = new DatabaseContext();

                // Очищаем данные
                foreach (var mealType in MealTypes)
                {
                    MealEntries[mealType].Clear();
                }
                Meals.Clear();

                // Загружаем записи за день
                var dayEntries = await context.FoodDiaries
                    .Include(fd => fd.Product)
                    .Where(fd => fd.UserId == _currentUser.UserId &&
                                fd.Date.Date == SelectedDate.Date)
                    .OrderBy(fd => fd.Date)
                    .ToListAsync();

                // Группируем по приемам пищи
                foreach (var entry in dayEntries)
                {
                    var mealType = GetMealTypeForEntry(entry);
                    if (MealEntries.ContainsKey(mealType))
                    {
                        MealEntries[mealType].Add(new FoodEntryDto
                        {
                            EntryId = entry.DiaryId,
                            Date = entry.Date,
                            ProductName = entry.Product?.ProductName ?? "Неизвестный продукт",
                            Quantity = entry.Quantity,
                            Calories = entry.Calories,
                            Protein = entry.Protein,
                            Fat = entry.Fat,
                            Carbohydrates = entry.Carbohydrates
                        });
                    }
                }

                // Создаем сводку по приемам пищи
                CalculateDailySummary();
                UpdateProgress();

                _mainViewModel.UpdateStatus($"Загружено {dayEntries.Count} записей за {SelectedDate:dd.MM.yyyy}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetMealTypeForEntry(FoodDiary entry)
        {
            return GetMealTypeByTime(entry.Date);
        }

        private string GetMealTypeByTime(DateTime time)
        {
            var hour = time.Hour;

            // Распределение по времени
            if (hour >= 6 && hour < 11) return "Завтрак";
            if (hour >= 11 && hour < 17) return "Обед";
            return "Ужин"; // Все остальное время - ужин
        }

        private void CalculateDailySummary()
        {
            decimal totalCalories = 0;
            decimal totalProtein = 0;
            decimal totalFat = 0;
            decimal totalCarbs = 0;

            // Пересчитываем все приемы пищи
            foreach (var mealType in MealTypes)
            {
                var entries = MealEntries[mealType];
                if (entries.Any())
                {
                    var meal = new MealDto
                    {
                        MealName = mealType,
                        Calories = entries.Sum(e => e.Calories),
                        Protein = entries.Sum(e => e.Protein),
                        Fat = entries.Sum(e => e.Fat),
                        Carbs = entries.Sum(e => e.Carbohydrates)
                    };

                    totalCalories += meal.Calories;
                    totalProtein += meal.Protein;
                    totalFat += meal.Fat;
                    totalCarbs += meal.Carbs;
                }
            }

            DailyNutrition.TotalCalories = totalCalories;
            DailyNutrition.TotalProtein = totalProtein;
            DailyNutrition.TotalFat = totalFat;
            DailyNutrition.TotalCarbs = totalCarbs;
        }

        private void InitializeTargets()
        {
            if (_currentUser == null) return;

            DailyNutrition.TargetCalories = _currentUser.DailyCalorieTarget;
            DailyNutrition.TargetProtein = _currentUser.DailyProteinTarget;
            DailyNutrition.TargetFat = _currentUser.DailyFatTarget;
            DailyNutrition.TargetCarbs = _currentUser.DailyCarbsTarget;
        }

        private void UpdateProgress()
        {
            if (DailyNutrition.TargetCalories > 0)
                DailyNutrition.CaloriesProgress = Math.Round((DailyNutrition.TotalCalories / DailyNutrition.TargetCalories) * 100, 1);
            else
                DailyNutrition.CaloriesProgress = 0;

            if (DailyNutrition.TargetProtein > 0)
                DailyNutrition.ProteinProgress = Math.Round((DailyNutrition.TotalProtein / DailyNutrition.TargetProtein) * 100, 1);
            else
                DailyNutrition.ProteinProgress = 0;

            if (DailyNutrition.TargetFat > 0)
                DailyNutrition.FatProgress = Math.Round((DailyNutrition.TotalFat / DailyNutrition.TargetFat) * 100, 1);
            else
                DailyNutrition.FatProgress = 0;

            if (DailyNutrition.TargetCarbs > 0)
                DailyNutrition.CarbsProgress = Math.Round((DailyNutrition.TotalCarbs / DailyNutrition.TargetCarbs) * 100, 1);
            else
                DailyNutrition.CarbsProgress = 0;

            OnPropertyChanged(nameof(DailyNutrition));
            OnPropertyChanged(nameof(BreakfastMeal));
            OnPropertyChanged(nameof(LunchMeal));
            OnPropertyChanged(nameof(DinnerMeal));
        }

        private MealDto GetMealDto(string mealType)
        {
            var entries = MealEntries.ContainsKey(mealType) ? MealEntries[mealType] : new ObservableCollection<FoodEntryDto>();

            return new MealDto
            {
                MealName = mealType,
                Calories = entries.Sum(e => e.Calories),
                Protein = entries.Sum(e => e.Protein),
                Fat = entries.Sum(e => e.Fat),
                Carbs = entries.Sum(e => e.Carbohydrates)
            };
        }

        private async void AddProductToMeal()
        {
            if (SelectedProduct == null || !IsValidQuantity) return;

            try
            {
                IsLoading = true;
                using var context = new DatabaseContext();

                var product = await context.Products.FindAsync(SelectedProduct.ProductId);
                if (product == null)
                {
                    MessageBox.Show("Продукт не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var multiplier = ProductQuantity / 100;
                var calories = Math.Round(product.Calories * multiplier, 2);
                var protein = Math.Round(product.Protein * multiplier, 2);
                var fat = Math.Round(product.Fat * multiplier, 2);
                var carbs = Math.Round(product.Carbohydrates * multiplier, 2);

                // Создаем время в зависимости от выбранного приема пищи
                var entryDate = CreateTimeForMealType(SelectedMealType);

                var foodEntry = new FoodDiary
                {
                    UserId = _currentUser.UserId,
                    Date = entryDate,
                    ProductId = SelectedProduct.ProductId,
                    Quantity = ProductQuantity,
                    Calories = calories,
                    Protein = protein,
                    Fat = fat,
                    Carbohydrates = carbs
                };

                await context.FoodDiaries.AddAsync(foodEntry);
                await context.SaveChangesAsync();

                // Обновляем отображение
                var newEntry = new FoodEntryDto
                {
                    EntryId = foodEntry.DiaryId,
                    Date = foodEntry.Date,
                    ProductName = product.ProductName,
                    Quantity = ProductQuantity,
                    Calories = calories,
                    Protein = protein,
                    Fat = fat,
                    Carbohydrates = carbs
                };

                CurrentMealEntries.Add(newEntry);
                CalculateDailySummary();
                UpdateProgress();

                _mainViewModel.UpdateStatus($"Добавлен: {product.ProductName} ({ProductQuantity:F1}г) в {SelectedMealType}");

                // Сбрасываем количество
                ProductQuantityInput = "100";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления продукта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private DateTime CreateTimeForMealType(string mealType)
        {
            var today = SelectedDate;
            int hour;

            switch (mealType)
            {
                case "Завтрак":
                    hour = 8; // 8:00 утра
                    break;
                case "Обед":
                    hour = 13; // 13:00 дня
                    break;
                case "Ужин":
                    hour = 19; // 19:00 вечера
                    break;
                default:
                    hour = 12; // Полдень по умолчанию
                    break;
            }

            return new DateTime(today.Year, today.Month, today.Day, hour, 0, 0);
        }

        private async void RemoveSelectedEntry()
        {
            if (SelectedEntry == null) return;

            try
            {
                // Сохраняем имя продукта перед удалением
                var productName = SelectedEntry.ProductName;

                using var context = new DatabaseContext();
                var dbEntry = await context.FoodDiaries.FindAsync(SelectedEntry.EntryId);
                if (dbEntry != null)
                {
                    context.FoodDiaries.Remove(dbEntry);
                    await context.SaveChangesAsync();

                    // Находим и удаляем запись из соответствующего приема пищи
                    foreach (var mealType in MealTypes)
                    {
                        var entries = MealEntries[mealType];
                        var entryToRemove = entries.FirstOrDefault(e => e.EntryId == SelectedEntry.EntryId);
                        if (entryToRemove != null)
                        {
                            entries.Remove(entryToRemove);
                            break;
                        }
                    }

                    CalculateDailySummary();
                    UpdateProgress();

                    _mainViewModel.UpdateStatus($"Удалена запись: {productName}");
                    // Сбрасываем выделение
                    SelectedEntry = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ClearCurrentMeal()
        {
            if (!CurrentMealEntries.Any()) return;

            var result = MessageBox.Show($"Очистить {SelectedMealType}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var context = new DatabaseContext();
                    var entryIds = CurrentMealEntries.Select(e => e.EntryId).ToList();
                    var entriesToDelete = await context.FoodDiaries
                        .Where(fd => entryIds.Contains(fd.DiaryId))
                        .ToListAsync();

                    context.FoodDiaries.RemoveRange(entriesToDelete);
                    await context.SaveChangesAsync();

                    CurrentMealEntries.Clear();
                    CalculateDailySummary();
                    UpdateProgress();

                    _mainViewModel.UpdateStatus($"{SelectedMealType} очищен");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка очистки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ClearDay()
        {
            var result = MessageBox.Show($"Удалить все записи за {SelectedDate:dd.MM.yyyy}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var context = new DatabaseContext();
                    var entriesToDelete = await context.FoodDiaries
                        .Where(fd => fd.UserId == _currentUser.UserId &&
                                    fd.Date.Date == SelectedDate.Date)
                        .ToListAsync();

                    context.FoodDiaries.RemoveRange(entriesToDelete);
                    await context.SaveChangesAsync();

                    foreach (var mealType in MealTypes)
                    {
                        MealEntries[mealType].Clear();
                    }
                    CalculateDailySummary();
                    UpdateProgress();

                    _mainViewModel.UpdateStatus($"Записи за {SelectedDate:dd.MM.yyyy} удалены");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);
        private void NextDay() => SelectedDate = SelectedDate.AddDays(1);

        private bool CanAddProduct() => SelectedProduct != null && IsValidQuantity;

        private async void LoadDayData() => await LoadDayDataAsync();
    }
}
