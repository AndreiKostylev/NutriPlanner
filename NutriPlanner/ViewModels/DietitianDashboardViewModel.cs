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
    /// ViewModel для панели управления диетолога
    /// </summary>
    public class DietitianDashboardViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private readonly DatabaseContext _context;

        private DashboardStatsDto _stats;
        private ObservableCollection<ClientAlertDto> _alerts;
        private ObservableCollection<DailyNutritionDto> _recentClientDiaries;
        private bool _isLoading;

        /// <summary>
        /// Статистика панели управления
        /// </summary>
        public DashboardStatsDto Stats
        {
            get => _stats;
            set { _stats = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Список предупреждений по клиентам
        /// </summary>
        public ObservableCollection<ClientAlertDto> Alerts
        {
            get => _alerts;
            set { _alerts = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Недавние записи дневников клиентов
        /// </summary>
        public ObservableCollection<DailyNutritionDto> RecentClientDiaries
        {
            get => _recentClientDiaries;
            set { _recentClientDiaries = value; OnPropertyChanged(); }
        }

        public ClientAlertDto SelectedAlert { get; set; }

        /// <summary>
        /// Флаг загрузки данных
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Команда обновления данных
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Команда просмотра клиента
        /// </summary>
        public ICommand ViewClientCommand { get; }

        /// <summary>
        /// Команда создания нового плана
        /// </summary>
        public ICommand CreatePlanCommand { get; }

        /// <summary>
        /// Команда просмотра дневника клиента
        /// </summary>
        public ICommand ViewClientDiaryCommand { get; }

        /// <summary>
        /// Конструктор ViewModel панели диетолога
        /// </summary>
        /// <param name="mainVM">Главный ViewModel приложения</param>
        public DietitianDashboardViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _context = new DatabaseContext();

            Stats = new DashboardStatsDto();
            Alerts = new ObservableCollection<ClientAlertDto>();
            RecentClientDiaries = new ObservableCollection<DailyNutritionDto>();

            RefreshCommand = new RelayCommand(async () => await LoadDashboardDataAsync());
            ViewClientCommand = new RelayCommand(ViewClient, () => SelectedAlert != null);
            CreatePlanCommand = new RelayCommand(CreateNewPlan);
            ViewClientDiaryCommand = new RelayCommand(ViewClientDiary, () => SelectedAlert != null);

            // Начальная загрузка данных
            LoadDashboardDataAsync();
        }

        /// <summary>
        /// Асинхронная загрузка данных для панели управления
        /// </summary>
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                IsLoading = true;
                _mainVM.UpdateStatus("Загрузка данных панели диетолога...");

                // Загрузка клиентов (пользователей с ролью User)
                var clients = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName == "User" && u.IsActive)
                    .ToListAsync();

                // Обновление статистики
                await UpdateStatistics(clients);

                // Загрузка предупреждений
                await LoadAlertsAsync(clients);

                // Загрузка недавних дневников клиентов
                await LoadRecentClientDiariesAsync();

                _mainVM.UpdateStatus($"Панель обновлена. Клиентов: {Stats.TotalClients}");
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка загрузки: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}\n\n" +
                                "Проверьте подключение к базе данных.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Обновление статистики панели
        /// </summary>
        /// <param name="clients">Список клиентов</param>
        private async Task UpdateStatistics(List<User> clients)
        {
            Stats.TotalClients = clients.Count;

            if (clients.Any())
            {
                // Активные клиенты (с активными планами)
                var activeClientIds = await _context.NutritionPlans
                    .Where(p => p.Status == "Активен" &&
                               p.StartDate <= DateTime.Today &&
                               p.EndDate >= DateTime.Today)
                    .Select(p => p.UserId)
                    .Distinct()
                    .ToListAsync();

                Stats.ActiveClients = clients.Count(c => activeClientIds.Contains(c.UserId));

                // Клиенты, требующие внимания (не заполнили дневник сегодня)
                var todayEntries = await _context.FoodDiaries
                    .Where(fd => fd.Date.Date == DateTime.Today.Date)
                    .Select(fd => fd.UserId)
                    .Distinct()
                    .ToListAsync();

                Stats.ClientsNeedingAttention = clients.Count(c => !todayEntries.Contains(c.UserId));

                // Планы, созданные на этой неделе
                var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                Stats.PlansCreatedThisWeek = await _context.NutritionPlans
                    .Where(p => p.StartDate >= startOfWeek)
                    .CountAsync();

                // Непрочитанные сообщения (заглушка)
                Stats.MessagesUnread = 0;
            }
            else
            {
                Stats.ActiveClients = 0;
                Stats.ClientsNeedingAttention = 0;
                Stats.PlansCreatedThisWeek = 0;
                Stats.MessagesUnread = 0;
            }
        }

        /// <summary>
        /// Загрузка предупреждений по клиентам
        /// </summary>
        /// <param name="clients">Список клиентов</param>
        private async Task LoadAlertsAsync(List<User> clients)
        {
            Alerts.Clear();

            if (!clients.Any())
            {
                Alerts.Add(new ClientAlertDto
                {
                    ClientId = 0,
                    ClientName = "Информация",
                    AlertType = "Info",
                    Message = "В системе пока нет клиентов. Пригласите новых пользователей.",
                    AlertDate = DateTime.Now,
                    Priority = "Low"
                });
                return;
            }

            var alerts = new List<ClientAlertDto>();

            foreach (var client in clients)
            {
                try
                {
                    // Проверка: не заполнил дневник сегодня
                    var hasTodayEntry = await _context.FoodDiaries
                        .AnyAsync(fd => fd.UserId == client.UserId &&
                                       fd.Date.Date == DateTime.Today.Date);

                    if (!hasTodayEntry)
                    {
                        alerts.Add(new ClientAlertDto
                        {
                            ClientId = client.UserId,
                            ClientName = client.Username,
                            AlertType = "NoEntry",
                            Message = "Не заполнил дневник сегодня",
                            AlertDate = DateTime.Now,
                            Priority = "High"
                        });
                    }

                    // Проверка: нет активного плана питания
                    var hasActivePlan = await _context.NutritionPlans
                        .AnyAsync(p => p.UserId == client.UserId &&
                                      p.Status == "Активен" &&
                                      p.StartDate <= DateTime.Today &&
                                      p.EndDate >= DateTime.Today);

                    if (!hasActivePlan)
                    {
                        alerts.Add(new ClientAlertDto
                        {
                            ClientId = client.UserId,
                            ClientName = client.Username,
                            AlertType = "NoPlan",
                            Message = "Нет активного плана питания",
                            AlertDate = DateTime.Now,
                            Priority = "Medium"
                        });
                    }

                    // Проверка: отклонение от целей (за последние 3 дня)
                    var recentEntries = await _context.FoodDiaries
                        .Where(fd => fd.UserId == client.UserId &&
                                    fd.Date >= DateTime.Today.AddDays(-3))
                        .ToListAsync();

                    if (recentEntries.Any() && client.DailyCalorieTarget > 0)
                    {
                        var avgCalories = recentEntries.Average(fd => fd.Calories);
                        var deviation = Math.Abs((avgCalories / client.DailyCalorieTarget - 1) * 100);

                        if (deviation > 25) // Более 25% отклонение
                        {
                            alerts.Add(new ClientAlertDto
                            {
                                ClientId = client.UserId,
                                ClientName = client.Username,
                                AlertType = "Deviation",
                                Message = $"Отклонение от цели: {deviation:F1}%",
                                AlertDate = DateTime.Now,
                                Priority = deviation > 40 ? "High" : "Medium"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но продолжаем обработку других клиентов
                    Console.WriteLine($"Ошибка при обработке клиента {client.Username}: {ex.Message}");
                }
            }

            // Если нет алертов - добавляем сообщение об успехе
            if (!alerts.Any())
            {
                alerts.Add(new ClientAlertDto
                {
                    ClientId = 0,
                    ClientName = "Отлично!",
                    AlertType = "Success",
                    Message = "Все клиенты активны и выполняют планы питания",
                    AlertDate = DateTime.Now,
                    Priority = "Low"
                });
            }

            // Сортируем по приоритету
            var sortedAlerts = alerts
                .OrderByDescending(a => a.Priority == "High")
                .ThenByDescending(a => a.Priority == "Medium")
                .ThenBy(a => a.AlertDate)
                .ToList();

            foreach (var alert in sortedAlerts)
                Alerts.Add(alert);
        }

        /// <summary>
        /// Загрузка недавних записей дневников клиентов
        /// </summary>
        private async Task LoadRecentClientDiariesAsync()
        {
            RecentClientDiaries.Clear();

            try
            {
                // Получаем последние записи дневников за последние 3 дня
                var recentEntries = await _context.FoodDiaries
                    .Include(fd => fd.User)
                    .Include(fd => fd.Product)
                    .Where(fd => fd.Date >= DateTime.Today.AddDays(-3))
                    .OrderByDescending(fd => fd.Date)
                    .Take(20) // Ограничиваем количество
                    .ToListAsync();

                // Группируем по пользователю и дате
                var groupedEntries = recentEntries
                    .GroupBy(fd => new { fd.UserId, fd.Date.Date, fd.User.Username })
                    .Select(g => new
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.Username,
                        Date = g.Key.Date,
                        TotalCalories = g.Sum(fd => fd.Calories),
                        TotalProtein = g.Sum(fd => fd.Protein),
                        TotalFat = g.Sum(fd => fd.Fat),
                        TotalCarbs = g.Sum(fd => fd.Carbohydrates),
                        EntryCount = g.Count(),
                        LastEntryTime = g.Max(fd => fd.Date)
                    })
                    .OrderByDescending(x => x.LastEntryTime)
                    .ToList();

                foreach (var entry in groupedEntries)
                {
                    // Получаем целевые показатели пользователя
                    var user = await _context.Users.FindAsync(entry.UserId);
                    if (user == null) continue;

                    var dailyNutrition = new DailyNutritionDto
                    {
                        TotalCalories = entry.TotalCalories,
                        TotalProtein = entry.TotalProtein,
                        TotalFat = entry.TotalFat,
                        TotalCarbs = entry.TotalCarbs,
                        TargetCalories = user.DailyCalorieTarget,
                        TargetProtein = user.DailyProteinTarget,
                        TargetFat = user.DailyFatTarget,
                        TargetCarbs = user.DailyCarbsTarget
                    };

                    // Рассчитываем прогресс
                    if (dailyNutrition.TargetCalories > 0)
                        dailyNutrition.CaloriesProgress = Math.Round((dailyNutrition.TotalCalories / dailyNutrition.TargetCalories) * 100, 1);
                    if (dailyNutrition.TargetProtein > 0)
                        dailyNutrition.ProteinProgress = Math.Round((dailyNutrition.TotalProtein / dailyNutrition.TargetProtein) * 100, 1);
                    if (dailyNutrition.TargetFat > 0)
                        dailyNutrition.FatProgress = Math.Round((dailyNutrition.TotalFat / dailyNutrition.TargetFat) * 100, 1);
                    if (dailyNutrition.TargetCarbs > 0)
                        dailyNutrition.CarbsProgress = Math.Round((dailyNutrition.TotalCarbs / dailyNutrition.TargetCarbs) * 100, 1);

                    RecentClientDiaries.Add(dailyNutrition);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки дневников: {ex.Message}");
            }
        }

        /// <summary>
        /// Переход к просмотру клиента
        /// </summary>
        private void ViewClient()
        {
            if (SelectedAlert == null) return;

            if (SelectedAlert.ClientId == 0)
            {
                MessageBox.Show(SelectedAlert.Message, "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _mainVM.UpdateStatus($"Переход к клиенту: {SelectedAlert.ClientName}");

            // Открываем окно с информацией о клиенте
            var clientInfo = $"Клиент: {SelectedAlert.ClientName}\n" +
                            $"Тип предупреждения: {SelectedAlert.AlertType}\n" +
                            $"Сообщение: {SelectedAlert.Message}\n" +
                            $"Приоритет: {SelectedAlert.Priority}\n" +
                            $"Дата: {SelectedAlert.AlertDate:dd.MM.yyyy HH:mm}";

            MessageBox.Show(clientInfo, "Информация о клиенте",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Просмотр дневника клиента
        /// </summary>
        private void ViewClientDiary()
        {
            if (SelectedAlert == null || SelectedAlert.ClientId == 0) return;

            try
            {
                // Загружаем данные клиента
                var client = _context.Users
                    .Include(u => u.FoodDiaries.Where(fd => fd.Date.Date == DateTime.Today.Date))
                    .ThenInclude(fd => fd.Product)
                    .FirstOrDefault(u => u.UserId == SelectedAlert.ClientId);

                if (client == null)
                {
                    MessageBox.Show("Клиент не найден", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string diaryInfo = $"Дневник питания клиента: {client.Username}\n" +
                                  $"Дата: {DateTime.Today:dd.MM.yyyy}\n\n";

                if (client.FoodDiaries.Any())
                {
                    diaryInfo += "Сегодняшние записи:\n";
                    foreach (var entry in client.FoodDiaries.OrderBy(fd => fd.Date))
                    {
                        diaryInfo += $"{entry.Date:HH:mm} - {entry.Product?.ProductName ?? "Неизвестно"}\n" +
                                    $"  {entry.Quantity}г, {entry.Calories} ккал, " +
                                    $"Б: {entry.Protein}г, Ж: {entry.Fat}г, У: {entry.Carbohydrates}г\n";
                    }

                    var totalCalories = client.FoodDiaries.Sum(fd => fd.Calories);
                    var totalProtein = client.FoodDiaries.Sum(fd => fd.Protein);
                    var totalFat = client.FoodDiaries.Sum(fd => fd.Fat);
                    var totalCarbs = client.FoodDiaries.Sum(fd => fd.Carbohydrates);

                    diaryInfo += $"\nИтого за сегодня:\n" +
                                $"Калории: {totalCalories:F0} / {client.DailyCalorieTarget} ({totalCalories / client.DailyCalorieTarget * 100:F1}%)\n" +
                                $"Белки: {totalProtein:F1}г / {client.DailyProteinTarget}г ({totalProtein / client.DailyProteinTarget * 100:F1}%)\n" +
                                $"Жиры: {totalFat:F1}г / {client.DailyFatTarget}г ({totalFat / client.DailyFatTarget * 100:F1}%)\n" +
                                $"Углеводы: {totalCarbs:F1}г / {client.DailyCarbsTarget}г ({totalCarbs / client.DailyCarbsTarget * 100:F1}%)";
                }
                else
                {
                    diaryInfo += "Записей за сегодня нет.";
                }

                MessageBox.Show(diaryInfo, "Дневник клиента",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки дневника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Создание нового плана питания
        /// </summary>
        private void CreateNewPlan()
        {
            _mainVM.UpdateStatus("Создание нового плана питания");

            // Открываем окно с возможностью создания плана
            var result = MessageBox.Show("Вы хотите создать новый план питания?\n\n" +
                                       "1. Выберите клиента в разделе 'Управление клиентами'\n" +
                                       "2. Используйте кнопку 'Создать план'\n" +
                                       "3. Заполните параметры плана",
                                       "Создание плана питания",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Information);
        }

        /// <summary>
        /// Обновление данных панели
        /// </summary>
        public void RefreshDashboard()
        {
            LoadDashboardDataAsync();
        }
    }


}
