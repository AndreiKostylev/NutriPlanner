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
    public class DietitianDashboardViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private readonly DatabaseContext _context;

        private DashboardStatsDto _stats;
        private ObservableCollection<ClientAlertDto> _alerts;
        private bool _isLoading;

        public DashboardStatsDto Stats
        {
            get => _stats;
            set { _stats = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ClientAlertDto> Alerts
        {
            get => _alerts;
            set { _alerts = value; OnPropertyChanged(); }
        }

        public ClientAlertDto SelectedAlert { get; set; }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ViewClientCommand { get; }
        public ICommand CreatePlanCommand { get; }

        public DietitianDashboardViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _context = new DatabaseContext();

            Stats = new DashboardStatsDto();
            Alerts = new ObservableCollection<ClientAlertDto>();

            RefreshCommand = new RelayCommand(async () => await LoadDashboardDataAsync());
            ViewClientCommand = new RelayCommand(ViewClient, () => SelectedAlert != null);
            CreatePlanCommand = new RelayCommand(CreateNewPlan);

            // Асинхронная загрузка через Task.Run + Dispatcher
            Task.Run(async () => await LoadDashboardDataAsync());
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                IsLoading = true;
                _mainVM.UpdateStatus("Загрузка данных панели диетолога...");

                var clients = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName == "User" && u.IsActive)
                    .ToListAsync();

                _mainVM.UpdateStatus($"Найдено клиентов: {clients.Count}");

                if (!clients.Any())
                {
                    Stats.TotalClients = 0;
                    Stats.ActiveClients = 0;
                    Stats.ClientsNeedingAttention = 0;
                    Alerts.Clear();
                    Alerts.Add(new ClientAlertDto
                    {
                        ClientId = 0,
                        ClientName = "Информация",
                        AlertType = "Info",
                        Message = "В системе пока нет клиентов. Зарегистрируйте новых пользователей.",
                        AlertDate = DateTime.Now,
                        Priority = "Low"
                    });

                    OnPropertyChanged(nameof(Stats));
                    OnPropertyChanged(nameof(Alerts));
                    _mainVM.UpdateStatus("Нет клиентов в системе");
                    return;
                }

                Stats.TotalClients = clients.Count;

                Stats.ActiveClients = 0;
                foreach (var client in clients)
                {
                    var hasActivePlan = await _context.NutritionPlans
                        .AnyAsync(p => p.UserId == client.UserId && p.Status == "Active");
                    if (hasActivePlan) Stats.ActiveClients++;
                }

                Stats.ClientsNeedingAttention = 0;
                foreach (var client in clients)
                {
                    var hasTodayEntry = await _context.FoodDiaries
                        .AnyAsync(fd => fd.UserId == client.UserId && fd.Date.Date == DateTime.Today);
                    if (!hasTodayEntry) Stats.ClientsNeedingAttention++;
                }

                await LoadAlertsAsync(clients);

                _mainVM.UpdateStatus($"Панель обновлена. Клиентов: {Stats.TotalClients}");
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка загрузки: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}\n\n" +
                                "Убедитесь, что:\n" +
                                "1. База данных доступна\n" +
                                "2. В таблице Roles есть роль 'User'\n" +
                                "3. В таблице Users есть активные пользователи с RoleId = 1",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAlertsAsync(System.Collections.Generic.List<User> clients)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Alerts.Clear();
            });

            foreach (var client in clients)
            {
                try
                {
                    var hasTodayEntry = await _context.FoodDiaries
                        .AnyAsync(fd => fd.UserId == client.UserId && fd.Date.Date == DateTime.Today);
                    if (!hasTodayEntry)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Alerts.Add(new ClientAlertDto
                            {
                                ClientId = client.UserId,
                                ClientName = client.Username,
                                AlertType = "NoEntry",
                                Message = "Не заполнил дневник сегодня",
                                AlertDate = DateTime.Now,
                                Priority = "High"
                            });
                        });
                    }

                    var hasActivePlan = await _context.NutritionPlans
                        .AnyAsync(p => p.UserId == client.UserId && p.Status == "Active");
                    if (!hasActivePlan)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Alerts.Add(new ClientAlertDto
                            {
                                ClientId = client.UserId,
                                ClientName = client.Username,
                                AlertType = "NoPlan",
                                Message = "Нет активного плана питания",
                                AlertDate = DateTime.Now,
                                Priority = "Medium"
                            });
                        });
                    }

                    var lastWeekEntries = await _context.FoodDiaries
                        .Where(fd => fd.UserId == client.UserId && fd.Date >= DateTime.Today.AddDays(-7))
                        .ToListAsync();
                    if (lastWeekEntries.Any() && client.DailyCalorieTarget > 0)
                    {
                        var avgCalories = lastWeekEntries.Average(fd => fd.Calories);
                        var deviation = Math.Abs((avgCalories / client.DailyCalorieTarget - 1) * 100);
                        if (deviation > 20)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                Alerts.Add(new ClientAlertDto
                                {
                                    ClientId = client.UserId,
                                    ClientName = client.Username,
                                    AlertType = "Deviation",
                                    Message = $"Отклонение от цели: {deviation:F1}%",
                                    AlertDate = DateTime.Now,
                                    Priority = deviation > 30 ? "High" : "Medium"
                                });
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке клиента {client.Username}: {ex.Message}");
                }
            }

            if (!Alerts.Any())
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Alerts.Add(new ClientAlertDto
                    {
                        ClientId = 0,
                        ClientName = "Отлично!",
                        AlertType = "Success",
                        Message = "Все клиенты активны и выполняют свои планы",
                        AlertDate = DateTime.Now,
                        Priority = "Low"
                    });
                });
            }

            var sortedAlerts = Alerts
                .OrderByDescending(a => a.Priority == "High")
                .ThenByDescending(a => a.Priority == "Medium")
                .ThenBy(a => a.AlertDate)
                .ToList();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Alerts.Clear();
                foreach (var alert in sortedAlerts)
                    Alerts.Add(alert);
            });

            OnPropertyChanged(nameof(Alerts));
        }

        private void ViewClient()
        {
            if (SelectedAlert == null) return;

            if (SelectedAlert.ClientId == 0)
            {
                MessageBox.Show(SelectedAlert.Message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _mainVM.UpdateStatus($"Переход к клиенту: {SelectedAlert.ClientName}");
            MessageBox.Show($"Клиент: {SelectedAlert.ClientName}\nТип предупреждения: {SelectedAlert.AlertType}\nСообщение: {SelectedAlert.Message}", "Информация о клиенте", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateNewPlan()
        {
            _mainVM.UpdateStatus("Создание нового плана питания");
            MessageBox.Show("Функция создания нового плана питания будет реализована в следующем обновлении", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }


}
