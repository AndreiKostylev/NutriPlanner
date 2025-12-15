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

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// ViewModel для просмотра планов питания пользователем
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

        public ObservableCollection<NutritionPlanDto> Plans
        {
            get => _plans;
            set { _plans = value; OnPropertyChanged(); }
        }

        public NutritionPlanDto SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                _selectedPlan = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedPlan));
                if (value != null)
                {
                    LoadTodayMeals();
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
                _selectedDate = value;
                OnPropertyChanged();
                LoadTodayMeals();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool HasSelectedPlan => SelectedPlan != null;

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

        // Команды
        public ICommand LoadPlansCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand ExportPlanCommand { get; }
        public ICommand ViewPlanDetailsCommand { get; }
        public ICommand RefreshCommand { get; }

        public UserPlanViewViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;

            Plans = new ObservableCollection<NutritionPlanDto>();
            TodayMeals = new ObservableCollection<FoodEntryDto>();
            DailyProgress = new DailyNutritionDto();

            LoadPlansCommand = new RelayCommand(LoadPlans);
            PreviousDayCommand = new RelayCommand(PreviousDay);
            NextDayCommand = new RelayCommand(NextDay);
            TodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);
            ExportPlanCommand = new RelayCommand(ExportPlan, () => HasSelectedPlan);
            ViewPlanDetailsCommand = new RelayCommand(ViewPlanDetails, () => HasSelectedPlan);
            RefreshCommand = new RelayCommand(RefreshData);

            LoadPlans();
        }

        /// <summary>
        /// Обновляет все данные
        /// </summary>
        public async void RefreshData()
        {
            await LoadPlansAsync();
            LoadTodayMeals();
        }

        /// <summary>
        /// Загружает планы питания пользователя (асинхронная версия)
        /// </summary>
        public async Task LoadPlansAsync()
        {
            try
            {
                IsLoading = true;

                // Используем новый контекст
                using var context = new DatabaseContext();

                Plans.Clear();

                var plans = await context.NutritionPlans
                    .Where(p => p.UserId == _currentUser.UserId)
                    .OrderByDescending(p => p.StartDate)
                    .ToListAsync();

                foreach (var plan in plans)
                {
                    var planDto = new NutritionPlanDto
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        StartDate = plan.StartDate,
                        EndDate = plan.EndDate,
                        DailyCalories = plan.DailyCalories,
                        DailyProtein = plan.DailyProtein,
                        DailyFat = plan.DailyFat,
                        DailyCarbohydrates = plan.DailyCarbohydrates,
                        Status = plan.Status
                    };

                    Plans.Add(planDto);
                }

                if (Plans.Any())
                {
                    var active = ActivePlan;
                    SelectedPlan = active ?? Plans.First();
                }
                else
                {
                    SelectedPlan = null;
                    DailyProgress = new DailyNutritionDto();
                    OnPropertyChanged(nameof(DailyProgress));
                }

                _mainVM.UpdateStatus($"Загружено {Plans.Count} планов питания");
                OnPropertyChanged(nameof(ActivePlan));
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
        /// Загружает планы питания пользователя (синхронная обертка)
        /// </summary>
        private async void LoadPlans()
        {
            await LoadPlansAsync();
        }

        /// <summary>
        /// Загружает приемы пищи за выбранный день
        /// </summary>
        private async void LoadTodayMeals()
        {
            try
            {
                IsLoading = true;

                // Используем новый контекст
                using var context = new DatabaseContext();

                TodayMeals.Clear();

                if (_currentUser == null) return;

                var entries = await context.FoodDiaries
                    .Include(fd => fd.Product)
                    .Where(fd => fd.UserId == _currentUser.UserId &&
                                fd.Date.Date == SelectedDate.Date)
                    .OrderBy(fd => fd.Date)
                    .ToListAsync();

                foreach (var entry in entries)
                {
                    TodayMeals.Add(new FoodEntryDto
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

                // Рассчитываем прогресс после загрузки данных
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
        /// Рассчитывает прогресс выполнения плана
        /// </summary>
        private void CalculateProgress()
        {
            if (SelectedPlan == null)
            {
                // Если нет выбранного плана, используем цели из профиля пользователя
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
                // Используем цели из выбранного плана
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
                           $"ПРОГРЕСС ЗА {SelectedDate:dd.MM.yyyy}:\n" +
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

        /// <summary>
        /// Экспортирует план в файл
        /// </summary>
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
                    System.IO.File.WriteAllText(saveDialog.FileName, report, System.Text.Encoding.UTF8);

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

            // Рекомендуемые продукты для плана
            report += $"РЕКОМЕНДУЕМЫЕ ПРОДУКТЫ:\n" +
                     $"1. Источники белка: куриная грудка, рыба, яйца, творог, бобовые\n" +
                     $"2. Источники полезных жиров: авокадо, орехи, оливковое масло\n" +
                     $"3. Источники сложных углеводов: крупы, цельнозерновой хлеб, овощи\n" +
                     $"4. Овощи и фрукты: не менее 500 г в день\n\n";

            report += $"СТАТИСТИКА ПОТРЕБЛЕНИЯ ({SelectedDate:dd.MM.yyyy}):\n" +
                     $"• Калории: {DailyProgress.TotalCalories:F0} ккал ({DailyProgress.CaloriesProgress:F1}%)\n" +
                     $"• Белки: {DailyProgress.TotalProtein:F1} г ({DailyProgress.ProteinProgress:F1}%)\n" +
                     $"• Жиры: {DailyProgress.TotalFat:F1} г ({DailyProgress.FatProgress:F1}%)\n" +
                     $"• Углеводы: {DailyProgress.TotalCarbs:F1} г ({DailyProgress.CarbsProgress:F1}%)\n\n";

            if (TodayMeals.Any())
            {
                report += $"ПРИЕМЫ ПИЩИ ЗА ДЕНЬ ({TodayMeals.Count}):\n";
                foreach (var meal in TodayMeals)
                {
                    report += $"• {meal.Date:HH:mm} - {meal.ProductName} ({meal.Quantity}г)\n" +
                             $"  Калории: {meal.Calories:F0}, Белки: {meal.Protein:F1}г, " +
                             $"Жиры: {meal.Fat:F1}г, Углеводы: {meal.Carbohydrates:F1}г\n";
                }
            }
            else
            {
                report += "Приемов пищи за день не зафиксировано.\n";
            }

            report += $"\nРЕКОМЕНДАЦИИ:\n" +
                     $"1. Распределите приемы пищи на 4-6 раз в день\n" +
                     $"2. Пейте 2-3 литра воды ежедневно\n" +
                     $"3. Соблюдайте баланс белков, жиров и углеводов\n" +
                     $"4. Включайте овощи в каждый прием пищи\n" +
                     $"5. Избегайте быстрых углеводов и сладких напитков\n\n" +
                     $"Отчет сформирован автоматически.\n" +
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
    }
}
