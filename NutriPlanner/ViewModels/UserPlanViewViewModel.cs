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
        private readonly DatabaseContext _context;

        private ObservableCollection<NutritionPlanDto> _plans;
        private NutritionPlanDto _selectedPlan;
        private ObservableCollection<MealDto> _todayMeals;
        private DailyNutritionDto _dailyProgress;
        private DateTime _selectedDate = DateTime.Today;

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
                    CalculateProgress();
                }
            }
        }

        public ObservableCollection<MealDto> TodayMeals
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
                CalculateProgress();
            }
        }

        public bool HasSelectedPlan => SelectedPlan != null;

        // Активный план на сегодня
        public NutritionPlanDto ActivePlan
        {
            get
            {
                if (Plans == null) return null;
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

        public UserPlanViewViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;
            _context = new DatabaseContext();

            Plans = new ObservableCollection<NutritionPlanDto>();
            TodayMeals = new ObservableCollection<MealDto>();
            DailyProgress = new DailyNutritionDto();

            LoadPlansCommand = new RelayCommand(LoadPlans);
            PreviousDayCommand = new RelayCommand(PreviousDay);
            NextDayCommand = new RelayCommand(NextDay);
            TodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);
            ExportPlanCommand = new RelayCommand(ExportPlan, () => HasSelectedPlan);
            ViewPlanDetailsCommand = new RelayCommand(ViewPlanDetails, () => HasSelectedPlan);

            LoadPlans();
        }

        /// <summary>
        /// Загружает планы питания пользователя
        /// </summary>
        private async void LoadPlans()
        {
            try
            {
                Plans.Clear();

                var plans = await _context.NutritionPlans
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
                        // CreatedBy уже имеет значение по умолчанию "Диетолог"
                    };

                    Plans.Add(planDto);
                }

                if (Plans.Any())
                {
                    var active = ActivePlan;
                    SelectedPlan = active ?? Plans.First();
                }

                _mainVM.UpdateStatus($"Загружено {Plans.Count} планов питания");
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка загрузки планов: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки планов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает приемы пищи за выбранный день
        /// </summary>
        private async void LoadTodayMeals()
        {
            try
            {
                TodayMeals.Clear();

                if (_currentUser == null) return;

                var entries = await _context.FoodDiaries
                    .Include(fd => fd.Product)
                    .Where(fd => fd.UserId == _currentUser.UserId &&
                                fd.Date.Date == SelectedDate.Date)
                    .OrderBy(fd => fd.Date)
                    .ToListAsync();

                foreach (var entry in entries)
                {
                    TodayMeals.Add(new MealDto
                    {
                        MealName = entry.Product?.ProductName ?? "Неизвестный продукт",
                        Calories = entry.Calories,
                        Protein = entry.Protein,
                        Fat = entry.Fat,
                        Carbs = entry.Carbohydrates,
                        MealTime = entry.Date,
                        Quantity = entry.Quantity
                    });
                }

                OnPropertyChanged(nameof(TodayMeals));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки приемов пищи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Рассчитывает прогресс выполнения плана
        /// </summary>
        private void CalculateProgress()
        {
            if (SelectedPlan == null) return;

            decimal totalCalories = TodayMeals.Sum(m => m.Calories);
            decimal totalProtein = TodayMeals.Sum(m => m.Protein);
            decimal totalFat = TodayMeals.Sum(m => m.Fat);
            decimal totalCarbs = TodayMeals.Sum(m => m.Carbs);

            DailyProgress.TotalCalories = totalCalories;
            DailyProgress.TotalProtein = totalProtein;
            DailyProgress.TotalFat = totalFat;
            DailyProgress.TotalCarbs = totalCarbs;

            // Устанавливаем цели из выбранного плана
            DailyProgress.TargetCalories = SelectedPlan.DailyCalories;
            DailyProgress.TargetProtein = SelectedPlan.DailyProtein;
            DailyProgress.TargetFat = SelectedPlan.DailyFat;
            DailyProgress.TargetCarbs = SelectedPlan.DailyCarbohydrates;

            // Рассчитываем прогресс
            if (DailyProgress.TargetCalories > 0)
                DailyProgress.CaloriesProgress = Math.Round((totalCalories / DailyProgress.TargetCalories) * 100, 1);
            if (DailyProgress.TargetProtein > 0)
                DailyProgress.ProteinProgress = Math.Round((totalProtein / DailyProgress.TargetProtein) * 100, 1);
            if (DailyProgress.TargetFat > 0)
                DailyProgress.FatProgress = Math.Round((totalFat / DailyProgress.TargetFat) * 100, 1);
            if (DailyProgress.TargetCarbs > 0)
                DailyProgress.CarbsProgress = Math.Round((totalCarbs / DailyProgress.TargetCarbs) * 100, 1);

            OnPropertyChanged(nameof(DailyProgress));
        }

        /// <summary>
        /// Показывает детали плана
        /// </summary>
        private void ViewPlanDetails()
        {
            if (SelectedPlan == null) return;

            string details = $"=== ДЕТАЛИ ПЛАНА ПИТАНИЯ ===\n\n" +
                           $"Название: {SelectedPlan.PlanName}\n" +
                           $"Создатель: {SelectedPlan.CreatedBy}\n" +
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
            if (SelectedPlan == null) return "";

            var report = $"=== ПЛАН ПИТАНИЯ ===\n\n" +
                        $"Клиент: {_currentUser.Username}\n" +
                        $"Email: {_currentUser.Email}\n" +
                        $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}\n\n" +
                        $"ОСНОВНАЯ ИНФОРМАЦИЯ:\n" +
                        $"Название плана: {SelectedPlan.PlanName}\n" +
                        $"Создатель: {SelectedPlan.CreatedBy}\n" +
                        $"Период действия: {SelectedPlan.StartDate:dd.MM.yyyy} - {SelectedPlan.EndDate:dd.MM.yyyy}\n" +
                        $"Статус: {SelectedPlan.Status}\n" +
                        $"Осталось дней: {(SelectedPlan.EndDate - DateTime.Today).Days + 1}\n\n" +
                        $"ДНЕВНЫЕ НОРМЫ:\n" +
                        $"• Калории: {SelectedPlan.DailyCalories} ккал\n" +
                        $"• Белки: {SelectedPlan.DailyProtein} г\n" +
                        $"• Жиры: {SelectedPlan.DailyFat} г\n" +
                        $"• Углеводы: {SelectedPlan.DailyCarbohydrates} г\n\n" +
                        $"СТАТИСТИКА ПОТРЕБЛЕНИЯ ({SelectedDate:dd.MM.yyyy}):\n" +
                        $"• Калории: {DailyProgress.TotalCalories:F0} ккал ({DailyProgress.CaloriesProgress:F1}%)\n" +
                        $"• Белки: {DailyProgress.TotalProtein:F1} г ({DailyProgress.ProteinProgress:F1}%)\n" +
                        $"• Жиры: {DailyProgress.TotalFat:F1} г ({DailyProgress.FatProgress:F1}%)\n" +
                        $"• Углеводы: {DailyProgress.TotalCarbs:F1} г ({DailyProgress.CarbsProgress:F1}%)\n\n";

            if (TodayMeals.Any())
            {
                report += $"ПРИЕМЫ ПИЩИ ЗА ДЕНЬ ({TodayMeals.Count}):\n";
                foreach (var meal in TodayMeals)
                {
                    report += $"• {meal.MealTime:HH:mm} - {meal.MealName}\n" +
                             $"  Калории: {meal.Calories:F0}, Белки: {meal.Protein:F1}г, " +
                             $"Жиры: {meal.Fat:F1}г, Углеводы: {meal.Carbs:F1}г\n";
                }
            }
            else
            {
                report += "Приемов пищи за день не зафиксировано.\n";
            }

            report += $"\nОтчет сформирован автоматически.\n" +
                     $"=== КОНЕЦ ОТЧЕТА ===";

            return report;
        }
    }
}
