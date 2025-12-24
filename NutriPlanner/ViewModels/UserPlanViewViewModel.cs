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
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// ViewModel для просмотра планов питания пользователем с рекомендациями продуктов
    /// </summary>
    public class UserPlanViewViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private readonly User _currentUser;

        private ObservableCollection<NutritionPlanDto> _plans;
        private NutritionPlanDto _selectedPlan;
        private ObservableCollection<FoodEntryDto> _todayMeals;
        private DailyNutritionDto _dailyProgress;
        private DateTime _selectedDate = DateTime.Today;
        private bool _isLoading;
        private bool _isRefreshing = false;
        private PlanProductDto _selectedPlanProduct;

        // Добавляем private поля для вычисляемых свойств
        private bool _hasNewPlans;
        private int _newPlansCount;

        // Продукты из выбранного плана
        private ObservableCollection<PlanProductDto> _planProducts;
        private ObservableCollection<PlanProductDto> _breakfastProducts;
        private ObservableCollection<PlanProductDto> _lunchProducts;
        private ObservableCollection<PlanProductDto> _dinnerProducts;
        private ObservableCollection<PlanProductDto> _snackProducts;

        public ObservableCollection<NutritionPlanDto> Plans
        {
            get => _plans;
            set
            {
                _plans = value;
                OnPropertyChanged();
                UpdateCalculatedProperties(); // Обновляем вычисляемые свойства
            }
        }

        public NutritionPlanDto SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                if (_selectedPlan != value)
                {
                    _selectedPlan = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedPlan));
                    OnPropertyChanged(nameof(CanAddToDiary));
                    OnPropertyChanged(nameof(CanAcceptPlan));
                    if (value != null)
                    {
                        LoadTodayMeals();
                        LoadPlanProducts();

                        CheckNewPlanNotification();
                    }
                }
            }
        }

        public ObservableCollection<FoodEntryDto> TodayMeals
        {
            get => _todayMeals;
            set { _todayMeals = value; OnPropertyChanged(); }
        }

        public DailyNutritionDto DailyProgress
        {
            get => _dailyProgress;
            set { _dailyProgress = value; OnPropertyChanged(); }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged();
                    LoadTodayMeals();

                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool HasSelectedPlan => SelectedPlan != null;
        public bool CanAddToDiary => HasSelectedPlan && PlanProducts.Any();
        public bool CanAcceptPlan => HasSelectedPlan && (SelectedPlan.Status == "Новый" || SelectedPlan.Status == "Отправлен");

        // Новые свойства с исправленными сеттерами
        public bool HasNewPlans
        {
            get => _hasNewPlans;
            private set
            {
                if (_hasNewPlans != value)
                {
                    _hasNewPlans = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NewPlansCount
        {
            get => _newPlansCount;
            private set
            {
                if (_newPlansCount != value)
                {
                    _newPlansCount = value;
                    OnPropertyChanged();
                }
            }
        }

        // Продукты из выбранного плана
        public ObservableCollection<PlanProductDto> PlanProducts
        {
            get => _planProducts;
            set
            {
                _planProducts = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddToDiary));
            }
        }

        public ObservableCollection<PlanProductDto> BreakfastProducts
        {
            get => _breakfastProducts;
            set { _breakfastProducts = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PlanProductDto> LunchProducts
        {
            get => _lunchProducts;
            set { _lunchProducts = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PlanProductDto> DinnerProducts
        {
            get => _dinnerProducts;
            set { _dinnerProducts = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PlanProductDto> SnackProducts
        {
            get => _snackProducts;
            set { _snackProducts = value; OnPropertyChanged(); }
        }

        public PlanProductDto SelectedPlanProduct
        {
            get => _selectedPlanProduct;
            set
            {
                _selectedPlanProduct = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedPlanProduct));
            }
        }

        public bool HasSelectedPlanProduct => SelectedPlanProduct != null;

        // Команды
        public ICommand LoadPlansCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand ExportPlanCommand { get; }
        public ICommand ViewPlanDetailsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddPlanToDiaryCommand { get; }
        public ICommand AddSelectedToDiaryCommand { get; }
        public ICommand AcceptPlanCommand { get; }
        public ICommand ViewNewPlansCommand { get; }
        public ICommand ClearPlanSelectionCommand { get; }

        public UserPlanViewViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;

            Plans = new ObservableCollection<NutritionPlanDto>();
            TodayMeals = new ObservableCollection<FoodEntryDto>();
            DailyProgress = new DailyNutritionDto();
            PlanProducts = new ObservableCollection<PlanProductDto>();
            BreakfastProducts = new ObservableCollection<PlanProductDto>();
            LunchProducts = new ObservableCollection<PlanProductDto>();
            DinnerProducts = new ObservableCollection<PlanProductDto>();
            SnackProducts = new ObservableCollection<PlanProductDto>();

            LoadPlansCommand = new RelayCommand(LoadPlans);
            PreviousDayCommand = new RelayCommand(PreviousDay);
            NextDayCommand = new RelayCommand(NextDay);
            TodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);
            ExportPlanCommand = new RelayCommand(ExportPlan, () => HasSelectedPlan);
            ViewPlanDetailsCommand = new RelayCommand(ViewPlanDetails, () => HasSelectedPlan);
            RefreshCommand = new RelayCommand(RefreshData, () => !_isRefreshing);
            AddPlanToDiaryCommand = new RelayCommand(AddPlanToDiary, () => CanAddToDiary);
            AddSelectedToDiaryCommand = new RelayCommand(AddSelectedToDiary, () => HasSelectedPlanProduct);
            AcceptPlanCommand = new RelayCommand(AcceptPlan, () => CanAcceptPlan);
            ViewNewPlansCommand = new RelayCommand(ViewNewPlans);
            ClearPlanSelectionCommand = new RelayCommand(ClearPlanSelection);

            LoadPlans();
        }

        /// <summary>
        /// Обновляет вычисляемые свойства при изменении списка планов
        /// </summary>
        private void UpdateCalculatedProperties()
        {
            if (Plans == null)
            {
                HasNewPlans = false;
                NewPlansCount = 0;
                return;
            }

            var newPlans = Plans.Where(p => p.Status == "Новый" || p.Status == "Отправлен").ToList();
            HasNewPlans = newPlans.Any();
            NewPlansCount = newPlans.Count;
        }

        /// <summary>
        /// Обновляет все данные
        /// </summary>
        public async void RefreshData()
        {
            if (_isRefreshing) return;

            try
            {
                _isRefreshing = true;
                await LoadPlansAsync();
                await LoadTodayMealsAsync();
                if (SelectedPlan != null)
                {
                    await LoadPlanProductsAsync();
                }
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// Загружает планы питания пользователя (асинхронная версия)
        /// </summary>
        public async Task LoadPlansAsync()
        {
            try
            {
                IsLoading = true;

                using var context = new DatabaseContext();

                var newPlans = new ObservableCollection<NutritionPlanDto>();

                var plans = await context.NutritionPlans
                    .Where(p => p.UserId == _currentUser.UserId)
                    .OrderByDescending(p => p.StartDate)
                    .ToListAsync();

                foreach (var plan in plans)
                {
                    newPlans.Add(new NutritionPlanDto
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        StartDate = plan.StartDate,
                        EndDate = plan.EndDate,
                        DailyCalories = plan.DailyCalories,
                        DailyProtein = plan.DailyProtein,
                        DailyFat = plan.DailyFat,
                        DailyCarbohydrates = plan.DailyCarbohydrates,
                        Status = plan.Status,

                    });
                }

                Plans = newPlans;

                if (Plans.Any())
                {
                    var active = ActivePlan;
                    if (active != null)
                    {
                        SelectedPlan = active;
                    }
                    else if (HasNewPlans)
                    {
                        SelectedPlan = Plans.First(p => p.Status == "Новый" || p.Status == "Отправлен");
                    }
                    else
                    {
                        SelectedPlan = Plans.First();
                    }
                }
                else
                {
                    SelectedPlan = null;
                    DailyProgress = new DailyNutritionDto();
                    OnPropertyChanged(nameof(DailyProgress));
                }

                _mainVM.UpdateStatus($"Загружено {Plans.Count} планов питания");
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка загрузки планов: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки планов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Загружает продукты из выбранного плана (асинхронная версия)
        /// </summary>
        public async Task LoadPlanProductsAsync()
        {
            try
            {
                if (SelectedPlan == null) return;

                using var context = new DatabaseContext();

                var newPlanProducts = new ObservableCollection<PlanProductDto>();
                var newBreakfastProducts = new ObservableCollection<PlanProductDto>();
                var newLunchProducts = new ObservableCollection<PlanProductDto>();
                var newDinnerProducts = new ObservableCollection<PlanProductDto>();
                var newSnackProducts = new ObservableCollection<PlanProductDto>();

                var planProducts = await context.PlanProducts
                    .Include(pp => pp.Product)
                    .Where(pp => pp.PlanId == SelectedPlan.PlanId)
                    .OrderBy(pp => pp.MealType)
                    .ThenBy(pp => pp.Product.ProductName)
                    .ToListAsync();

                foreach (var planProduct in planProducts)
                {
                    var dto = new PlanProductDto
                    {
                        PlanProductId = planProduct.PlanProductId,
                        PlanId = planProduct.PlanId,
                        ProductId = planProduct.ProductId,
                        ProductName = planProduct.Product.ProductName,
                        Category = planProduct.Product.Category,
                        MealType = planProduct.MealType,
                        Quantity = planProduct.Quantity,
                        Unit = planProduct.Product.Unit,
                        Calories = planProduct.Product.Calories,
                        Protein = planProduct.Product.Protein,
                        Fat = planProduct.Product.Fat,
                        Carbohydrates = planProduct.Product.Carbohydrates,
                        Notes = planProduct.Notes
                    };

                    newPlanProducts.Add(dto);

                    // Распределяем по приемам пищи
                    switch (planProduct.MealType.ToLower())
                    {
                        case "завтрак":
                            newBreakfastProducts.Add(dto);
                            break;
                        case "обед":
                            newLunchProducts.Add(dto);
                            break;
                        case "ужин":
                            newDinnerProducts.Add(dto);
                            break;
                        default:
                            newSnackProducts.Add(dto);
                            break;
                    }
                }

                PlanProducts = newPlanProducts;
                BreakfastProducts = newBreakfastProducts;
                LunchProducts = newLunchProducts;
                DinnerProducts = newDinnerProducts;
                SnackProducts = newSnackProducts;

                _mainVM.UpdateStatus($"Загружено {PlanProducts.Count} продуктов из плана");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продуктов плана: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Проверяет наличие новых планов и показывает уведомление
        /// </summary>
        private void CheckNewPlanNotification()
        {
            if (SelectedPlan != null && (SelectedPlan.Status == "Новый" || SelectedPlan.Status == "Отправлен"))
            {
                var result = MessageBox.Show(
                    $"У вас есть новый план питания от диетолога!\n\n" +
                    $"Название: {SelectedPlan.PlanName}\n" +
                    $"Статус: {SelectedPlan.Status}\n" +
                    $"Продуктов в плане: {PlanProducts.Count}\n\n" +
                    $"Хотите просмотреть детали и принять план?",
                    "Новый план питания",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    ViewPlanDetails();
                }
            }
        }

        /// <summary>
        /// Добавляет все продукты плана в дневник питания на сегодня
        /// </summary>
        private async void AddPlanToDiary()
        {
            if (SelectedPlan == null || !PlanProducts.Any())
            {
                MessageBox.Show("В плане нет продуктов для добавления", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    $"Добавить все продукты из плана '{SelectedPlan.PlanName}' в дневник питания на сегодня?\n\n" +
                    $"Всего продуктов: {PlanProducts.Count}\n" +
                    $"Завтрак: {BreakfastProducts.Count}\n" +
                    $"Обед: {LunchProducts.Count}\n" +
                    $"Ужин: {DinnerProducts.Count}\n" +
                    $"Перекусы: {SnackProducts.Count}",
                    "Добавление в дневник",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                using var context = new DatabaseContext();
                var addedCount = 0;

                foreach (var product in PlanProducts)
                {
                    DateTime entryTime = GetTimeForMealType(product.MealType);

                    var foodDiary = new FoodDiary
                    {
                        UserId = _currentUser.UserId,
                        Date = entryTime,
                        ProductId = product.ProductId,
                        Quantity = product.Quantity,
                        Calories = product.TotalCalories,
                        Protein = product.TotalProtein,
                        Fat = product.TotalFat,
                        Carbohydrates = product.TotalCarbs
                    };

                    context.FoodDiaries.Add(foodDiary);
                    addedCount++;
                }

                await context.SaveChangesAsync();

                MessageBox.Show($"Успешно добавлено {addedCount} продуктов в дневник питания!\n\n" +
                              "Обновите раздел 'Дневник питания' для просмотра изменений.",
                              "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                _mainVM.UpdateStatus($"Добавлено {addedCount} продуктов из плана в дневник");

                LoadTodayMeals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления в дневник: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Добавляет выбранный продукт из плана в дневник
        /// </summary>
        private async void AddSelectedToDiary()
        {
            if (SelectedPlanProduct == null) return;

            try
            {
                using var context = new DatabaseContext();
                DateTime entryTime = GetTimeForMealType(SelectedPlanProduct.MealType);

                var foodDiary = new FoodDiary
                {
                    UserId = _currentUser.UserId,
                    Date = entryTime,
                    ProductId = SelectedPlanProduct.ProductId,
                    Quantity = SelectedPlanProduct.Quantity,
                    Calories = SelectedPlanProduct.TotalCalories,
                    Protein = SelectedPlanProduct.TotalProtein,
                    Fat = SelectedPlanProduct.TotalFat,
                    Carbohydrates = SelectedPlanProduct.TotalCarbs
                };

                context.FoodDiaries.Add(foodDiary);
                await context.SaveChangesAsync();

                MessageBox.Show($"Продукт '{SelectedPlanProduct.ProductName}' добавлен в дневник питания!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                _mainVM.UpdateStatus($"Добавлен {SelectedPlanProduct.ProductName} в дневник");

                LoadTodayMeals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления в дневник: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DateTime GetTimeForMealType(string mealType)
        {
            var today = DateTime.Today;
            var mealTypeLower = mealType.ToLower();

            if (mealTypeLower.Contains("завтрак"))
                return new DateTime(today.Year, today.Month, today.Day, 8, 0, 0);
            if (mealTypeLower.Contains("обед"))
                return new DateTime(today.Year, today.Month, today.Day, 13, 0, 0);
            if (mealTypeLower.Contains("ужин"))
                return new DateTime(today.Year, today.Month, today.Day, 19, 0, 0);

            return new DateTime(today.Year, today.Month, today.Day, 12, 0, 0);
        }

        /// <summary>
        /// Принимает план (меняет статус на "Активен")
        /// </summary>
        private async void AcceptPlan()
        {
            if (SelectedPlan == null) return;

            try
            {
                var result = MessageBox.Show(
                    $"Принять план '{SelectedPlan.PlanName}'?\n\n" +
                    $"Продуктов в плане: {PlanProducts.Count}\n" +
                    "После принятия план станет активным, а все другие планы будут деактивированы.",
                    "Принятие плана",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                using var context = new DatabaseContext();
                var plan = await context.NutritionPlans.FindAsync(SelectedPlan.PlanId);

                if (plan != null)
                {
                    // Деактивируем все другие планы
                    var otherPlans = await context.NutritionPlans
                        .Where(p => p.UserId == _currentUser.UserId && p.PlanId != SelectedPlan.PlanId)
                        .ToListAsync();

                    foreach (var otherPlan in otherPlans)
                    {
                        otherPlan.Status = "Неактивен";
                    }

                    // Активируем выбранный план
                    plan.Status = "Активен";
                    await context.SaveChangesAsync();

                    SelectedPlan.Status = "Активен";
                    OnPropertyChanged(nameof(SelectedPlan));
                    OnPropertyChanged(nameof(CanAcceptPlan));

                    MessageBox.Show($"План '{SelectedPlan.PlanName}' успешно принят и активирован!\n\n" +
                                  $"Теперь это ваш активный план питания.",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    _mainVM.UpdateStatus($"План '{SelectedPlan.PlanName}' принят клиентом");
                    LoadPlans(); // Перезагружаем список планов
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка принятия плана: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Показывает список новых планов
        /// </summary>
        private void ViewNewPlans()
        {
            if (!HasNewPlans)
            {
                MessageBox.Show("Нет новых планов питания", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var newPlans = Plans.Where(p => p.Status == "Новый" || p.Status == "Отправлен").ToList();
            var message = $"У вас есть {newPlans.Count} новых планов:\n\n";

            foreach (var plan in newPlans)
            {
                message += $"• {plan.PlanName} (статус: {plan.Status}, период: {plan.StartDate:dd.MM.yyyy} - {plan.EndDate:dd.MM.yyyy})\n";
            }

            message += "\nВыберите план из списка для просмотра деталей и продуктов.";

            MessageBox.Show(message, "Новые планы питания",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Очищает выбор плана
        /// </summary>
        private void ClearPlanSelection()
        {
            SelectedPlan = null;
            PlanProducts.Clear();
            BreakfastProducts.Clear();
            LunchProducts.Clear();
            DinnerProducts.Clear();
            SnackProducts.Clear();
        }

        /// <summary>
        /// Загружает планы питания пользователя (синхронная обертка)
        /// </summary>
        private async void LoadPlans()
        {
            await LoadPlansAsync();
        }

        /// <summary>
        /// Загружает продукты из выбранного плана (синхронная обертка)
        /// </summary>
        private async void LoadPlanProducts()
        {
            await LoadPlanProductsAsync();
        }

        /// <summary>
        /// Загружает приемы пищи за выбранный день (асинхронная версия)
        /// </summary>
        public async Task LoadTodayMealsAsync()
        {
            try
            {
                IsLoading = true;

                using var context = new DatabaseContext();

                var newTodayMeals = new ObservableCollection<FoodEntryDto>();

                if (_currentUser == null) return;

                var entries = await context.FoodDiaries
                    .Include(fd => fd.Product)
                    .Where(fd => fd.UserId == _currentUser.UserId &&
                                fd.Date.Date == SelectedDate.Date)
                    .OrderBy(fd => fd.Date)
                    .ToListAsync();

                foreach (var entry in entries)
                {
                    newTodayMeals.Add(new FoodEntryDto
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

                TodayMeals = newTodayMeals;
                CalculateProgress();
                OnPropertyChanged(nameof(TodayMeals));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки приемов пищи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Загружает приемы пищи за выбранный день (синхронная обертка)
        /// </summary>
        private async void LoadTodayMeals()
        {
            await LoadTodayMealsAsync();
        }

        /// <summary>
        /// Рассчитывает прогресс выполнения плана
        /// </summary>
        private void CalculateProgress()
        {
            if (SelectedPlan == null)
            {
                DailyProgress = new DailyNutritionDto
                {
                    TotalCalories = TodayMeals.Sum(m => m.Calories),
                    TotalProtein = TodayMeals.Sum(m => m.Protein),
                    TotalFat = TodayMeals.Sum(m => m.Fat),
                    TotalCarbs = TodayMeals.Sum(m => m.Carbohydrates),
                    TargetCalories = _currentUser?.DailyCalorieTarget ?? 0,
                    TargetProtein = _currentUser?.DailyProteinTarget ?? 0,
                    TargetFat = _currentUser?.DailyFatTarget ?? 0,
                    TargetCarbs = _currentUser?.DailyCarbsTarget ?? 0
                };
            }
            else
            {
                decimal totalCalories = TodayMeals.Sum(m => m.Calories);
                decimal totalProtein = TodayMeals.Sum(m => m.Protein);
                decimal totalFat = TodayMeals.Sum(m => m.Fat);
                decimal totalCarbs = TodayMeals.Sum(m => m.Carbohydrates);

                DailyProgress.TotalCalories = totalCalories;
                DailyProgress.TotalProtein = totalProtein;
                DailyProgress.TotalFat = totalFat;
                DailyProgress.TotalCarbs = totalCarbs;

                DailyProgress.TargetCalories = SelectedPlan.DailyCalories;
                DailyProgress.TargetProtein = SelectedPlan.DailyProtein;
                DailyProgress.TargetFat = SelectedPlan.DailyFat;
                DailyProgress.TargetCarbs = SelectedPlan.DailyCarbohydrates;
            }

            // Рассчитываем прогресс
            if (DailyProgress.TargetCalories > 0)
                DailyProgress.CaloriesProgress = Math.Round((DailyProgress.TotalCalories / DailyProgress.TargetCalories) * 100, 1);
            else
                DailyProgress.CaloriesProgress = 0;

            if (DailyProgress.TargetProtein > 0)
                DailyProgress.ProteinProgress = Math.Round((DailyProgress.TotalProtein / DailyProgress.TargetProtein) * 100, 1);
            else
                DailyProgress.ProteinProgress = 0;

            if (DailyProgress.TargetFat > 0)
                DailyProgress.FatProgress = Math.Round((DailyProgress.TotalFat / DailyProgress.TargetFat) * 100, 1);
            else
                DailyProgress.FatProgress = 0;

            if (DailyProgress.TargetCarbs > 0)
                DailyProgress.CarbsProgress = Math.Round((DailyProgress.TotalCarbs / DailyProgress.TargetCarbs) * 100, 1);
            else
                DailyProgress.CarbsProgress = 0;

            OnPropertyChanged(nameof(DailyProgress));
        }

        /// <summary>
        /// Показывает детали плана
        /// </summary>
        private void ViewPlanDetails()
        {
            if (SelectedPlan == null)
            {
                MessageBox.Show("У вас нет активных планов питания.\n\n" +
                              "Обратитесь к диетологу для создания индивидуального плана.",
                              "Нет планов питания",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string details = $"=== ДЕТАЛИ ПЛАНА ПИТАНИЯ ===\n\n" +
                           $"Название: {SelectedPlan.PlanName}\n" +
                           $"Период: {SelectedPlan.StartDate:dd.MM.yyyy} - {SelectedPlan.EndDate:dd.MM.yyyy}\n" +
                           $"Осталось дней: {(SelectedPlan.EndDate - DateTime.Today).Days + 1}\n" +
                           $"Статус: {SelectedPlan.Status}\n\n" +
                           $"ДНЕВНЫЕ НОРМЫ:\n" +
                           $"• Калории: {SelectedPlan.DailyCalories} ккал\n" +
                           $"• Белки: {SelectedPlan.DailyProtein} г\n" +
                           $"• Жиры: {SelectedPlan.DailyFat} г\n" +
                           $"• Углеводы: {SelectedPlan.DailyCarbohydrates} г\n\n" +
                           $"ПРОДУКТЫ В ПЛАНЕ ({PlanProducts.Count}):\n";

            if (PlanProducts.Any())
            {
                details += $"• Завтрак: {BreakfastProducts.Count} продуктов\n";
                details += $"• Обед: {LunchProducts.Count} продуктов\n";
                details += $"• Ужин: {DinnerProducts.Count} продуктов\n";
                if (SnackProducts.Any()) details += $"• Перекусы: {SnackProducts.Count} продуктов\n";
            }
            else
            {
                details += "В плане пока нет добавленных продуктов.\n";
            }

            details += $"\nПРОГРЕСС ЗА {SelectedDate:dd.MM.yyyy}:\n" +
                      $"• Калории: {DailyProgress.TotalCalories:F0} / {SelectedPlan.DailyCalories} " +
                      $"({DailyProgress.CaloriesProgress:F1}%)\n" +
                      $"• Белки: {DailyProgress.TotalProtein:F1} г / {SelectedPlan.DailyProtein} г " +
                      $"({DailyProgress.ProteinProgress:F1}%)\n" +
                      $"• Жиры: {DailyProgress.TotalFat:F1} г / {SelectedPlan.DailyFat} г " +
                      $"({DailyProgress.FatProgress:F1}%)\n" +
                      $"• Углеводы: {DailyProgress.TotalCarbs:F1} г / {SelectedPlan.DailyCarbohydrates} г " +
                      $"({DailyProgress.CarbsProgress:F1}%)\n\n" +
                      $"Приемов пищи за день: {TodayMeals.Count}";

            MessageBox.Show(details, "Детали плана",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportPlan()
        {
            if (SelectedPlan == null) return;

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"План_{SelectedPlan.PlanName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".txt",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = "Экспорт плана питания"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string report = GeneratePlanReport();
                    File.WriteAllText(saveDialog.FileName, report, System.Text.Encoding.UTF8);

                    _mainVM.UpdateStatus($"План экспортирован в {saveDialog.FileName}");
                    MessageBox.Show($"План успешно экспортирован в файл:\n{saveDialog.FileName}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);
        private void NextDay() => SelectedDate = SelectedDate.AddDays(1);

        /// <summary>
        /// Генерирует отчет по плану
        /// </summary>
        private string GeneratePlanReport()
        {
            if (SelectedPlan == null) return "Нет данных для отчета";

            var report = $"=== ПЛАН ПИТАНИЯ ===\n\n" +
                        $"Клиент: {_currentUser.Username}\n" +
                        $"Email: {_currentUser.Email}\n" +
                        $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}\n\n" +
                        $"ОСНОВНАЯ ИНФОРМАЦИЯ:\n" +
                        $"Название плана: {SelectedPlan.PlanName}\n" +
                        $"Период действия: {SelectedPlan.StartDate:dd.MM.yyyy} - {SelectedPlan.EndDate:dd.MM.yyyy}\n" +
                        $"Статус: {SelectedPlan.Status}\n" +
                        $"Осталось дней: {(SelectedPlan.EndDate - DateTime.Today).Days + 1}\n\n" +
                        $"ДНЕВНЫЕ НОРМЫ:\n" +
                        $"• Калории: {SelectedPlan.DailyCalories} ккал\n" +
                        $"• Белки: {SelectedPlan.DailyProtein} г\n" +
                        $"• Жиры: {SelectedPlan.DailyFat} г\n" +
                        $"• Углеводы: {SelectedPlan.DailyCarbohydrates} г\n\n";

            // Продукты плана по приемам пищи
            if (PlanProducts.Any())
            {
                report += $"ПРОДУКТЫ ПЛАНА ПО ПРИЕМАМ ПИЩИ:\n\n";

                if (BreakfastProducts.Any())
                {
                    report += $"ЗАВТРАК:\n";
                    foreach (var product in BreakfastProducts)
                    {
                        report += $"• {product.ProductName}: {product.Quantity}г\n";
                        report += $"  Калории: {product.TotalCalories:F0}, Б: {product.TotalProtein:F1}г, Ж: {product.TotalFat:F1}г, У: {product.TotalCarbs:F1}г\n";
                        if (!string.IsNullOrEmpty(product.Notes)) report += $"  Примечание: {product.Notes}\n";
                    }
                    report += "\n";
                }

                if (LunchProducts.Any())
                {
                    report += $"ОБЕД:\n";
                    foreach (var product in LunchProducts)
                    {
                        report += $"• {product.ProductName}: {product.Quantity}г\n";
                        report += $"  Калории: {product.TotalCalories:F0}, Б: {product.TotalProtein:F1}г, Ж: {product.TotalFat:F1}г, У: {product.TotalCarbs:F1}г\n";
                        if (!string.IsNullOrEmpty(product.Notes)) report += $"  Примечание: {product.Notes}\n";
                    }
                    report += "\n";
                }

                if (DinnerProducts.Any())
                {
                    report += $"УЖИН:\n";
                    foreach (var product in DinnerProducts)
                    {
                        report += $"• {product.ProductName}: {product.Quantity}г\n";
                        report += $"  Калории: {product.TotalCalories:F0}, Б: {product.TotalProtein:F1}г, Ж: {product.TotalFat:F1}г, У: {product.TotalCarbs:F1}г\n";
                        if (!string.IsNullOrEmpty(product.Notes)) report += $"  Примечание: {product.Notes}\n";
                    }
                    report += "\n";
                }

                if (SnackProducts.Any())
                {
                    report += $"ПЕРЕКУСЫ:\n";
                    foreach (var product in SnackProducts)
                    {
                        report += $"• {product.ProductName}: {product.Quantity}г\n";
                        report += $"  Калории: {product.TotalCalories:F0}, Б: {product.TotalProtein:F1}г, Ж: {product.TotalFat:F1}г, У: {product.TotalCarbs:F1}г\n";
                        if (!string.IsNullOrEmpty(product.Notes)) report += $"  Примечание: {product.Notes}\n";
                    }
                    report += "\n";
                }
            }

            report += $"Отчет сформирован автоматически.\n" +
                     $"=== КОНЕЦ ОТЧЕТА ===";

            return report;
        }

        /// <summary>
        /// Метод для обновления при активации вкладки
        /// </summary>
        public void OnActivated()
        {
            RefreshData();
        }

        // Активный план на сегодня
        public NutritionPlanDto ActivePlan
        {
            get
            {
                if (Plans == null || !Plans.Any()) return null;
                return Plans.FirstOrDefault(p =>
                    p.Status == "Активен" &&
                    DateTime.Today >= p.StartDate &&
                    DateTime.Today <= p.EndDate);
            }
        }
    }
}
