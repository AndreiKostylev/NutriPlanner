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
using Microsoft.Win32;
using System.IO;
using System.Windows.Controls;

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// ViewModel для управления клиентами (для диетологов и админов)
    /// </summary>
    public class ClientManagementViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private readonly User _currentUser;
        private readonly DatabaseContext _context;

        private ObservableCollection<UserProfileDto> _clients;
        private UserProfileDto _selectedClient;
        private string _searchText = "";
        private DateTime _selectedDate = DateTime.Today;

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
                OnPropertyChanged(nameof(CanEditClient));
                if (value != null)
                {
                    LoadClientPlans();
                    LoadClientDiary();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                LoadClients();
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                LoadClientDiary();
            }
        }

        public bool CanEditClient => SelectedClient != null;

        // Для планов клиента
        public ObservableCollection<NutritionPlanDto> ClientPlans { get; set; }
        public NutritionPlanDto SelectedPlan { get; set; }

        // Для дневника клиента
        public ObservableCollection<FoodEntryDto> ClientDiaryEntries { get; set; }
        public DailyNutritionDto ClientDailyNutrition { get; set; }

        // Команды
        public ICommand LoadClientsCommand { get; }
        public ICommand EditClientCommand { get; }
        public ICommand CreatePlanForClientCommand { get; }
        public ICommand ViewClientDiaryCommand { get; }
        public ICommand ExportClientDataCommand { get; }
        public ICommand DeletePlanCommand { get; }
        public ICommand ActivatePlanCommand { get; }
        public ICommand ExportPlanCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }

        public ClientManagementViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;
            _context = new DatabaseContext();

            Clients = new ObservableCollection<UserProfileDto>();
            ClientPlans = new ObservableCollection<NutritionPlanDto>();
            ClientDiaryEntries = new ObservableCollection<FoodEntryDto>();
            ClientDailyNutrition = new DailyNutritionDto();

            LoadClientsCommand = new RelayCommand(LoadClients);
            EditClientCommand = new RelayCommand(EditClient, () => CanEditClient);
            CreatePlanForClientCommand = new RelayCommand(CreatePlanForClient, () => CanEditClient);
            ViewClientDiaryCommand = new RelayCommand(ViewClientDiary, () => CanEditClient);
            ExportClientDataCommand = new RelayCommand(ExportClientData, () => CanEditClient);
            DeletePlanCommand = new RelayCommand(DeletePlan, () => SelectedPlan != null);
            ActivatePlanCommand = new RelayCommand(ActivatePlan, () => SelectedPlan != null);
            ExportPlanCommand = new RelayCommand(ExportPlan, () => SelectedPlan != null);
            PreviousDayCommand = new RelayCommand(PreviousDay);
            NextDayCommand = new RelayCommand(NextDay);

            LoadClients();
        }

        private async void LoadClients()
        {
            try
            {
                Clients.Clear();

                var query = _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName == "User" && u.IsActive);

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(u =>
                        u.Username.Contains(SearchText) ||
                        u.Email.Contains(SearchText));
                }

                var users = await query.OrderBy(u => u.Username).ToListAsync();

                foreach (var user in users)
                {
                    Clients.Add(new UserProfileDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        Age = user.Age,
                        Gender = user.Gender,
                        Height = user.Height,
                        Weight = user.Weight,
                        ActivityLevel = user.ActivityLevel,
                        Goal = user.Goal,
                        DailyCalorieTarget = user.DailyCalorieTarget,
                        DailyProteinTarget = user.DailyProteinTarget,
                        DailyFatTarget = user.DailyFatTarget,
                        DailyCarbsTarget = user.DailyCarbsTarget
                    });
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

        private async void LoadClientPlans()
        {
            try
            {
                if (SelectedClient == null) return;

                ClientPlans.Clear();

                var plans = await _context.NutritionPlans
                    .Where(p => p.UserId == SelectedClient.UserId)
                    .OrderByDescending(p => p.StartDate)
                    .ToListAsync();

                foreach (var plan in plans)
                {
                    ClientPlans.Add(new NutritionPlanDto
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        StartDate = plan.StartDate,
                        EndDate = plan.EndDate,
                        DailyCalories = plan.DailyCalories,
                        DailyProtein = plan.DailyProtein,
                        DailyFat = plan.DailyFat,
                        DailyCarbohydrates = plan.DailyCarbohydrates
                    });
                }

                OnPropertyChanged(nameof(ClientPlans));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки планов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadClientDiary()
        {
            try
            {
                if (SelectedClient == null) return;

                ClientDiaryEntries.Clear();
                ClientDailyNutrition = new DailyNutritionDto();

                var entries = await _context.FoodDiaries
                    .Include(fd => fd.Product)
                    .Where(fd => fd.UserId == SelectedClient.UserId &&
                                fd.Date.Date == SelectedDate.Date)
                    .OrderBy(fd => fd.Date)
                    .ToListAsync();

                decimal totalCalories = 0;
                decimal totalProtein = 0;
                decimal totalFat = 0;
                decimal totalCarbs = 0;

                foreach (var entry in entries)
                {
                    ClientDiaryEntries.Add(new FoodEntryDto
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

                    totalCalories += entry.Calories;
                    totalProtein += entry.Protein;
                    totalFat += entry.Fat;
                    totalCarbs += entry.Carbohydrates;
                }

                ClientDailyNutrition.TotalCalories = totalCalories;
                ClientDailyNutrition.TotalProtein = totalProtein;
                ClientDailyNutrition.TotalFat = totalFat;
                ClientDailyNutrition.TotalCarbs = totalCarbs;

                // Рассчитываем прогресс
                if (SelectedClient.DailyCalorieTarget > 0)
                    ClientDailyNutrition.CaloriesProgress = Math.Round((totalCalories / SelectedClient.DailyCalorieTarget) * 100, 1);
                if (SelectedClient.DailyProteinTarget > 0)
                    ClientDailyNutrition.ProteinProgress = Math.Round((totalProtein / SelectedClient.DailyProteinTarget) * 100, 1);
                if (SelectedClient.DailyFatTarget > 0)
                    ClientDailyNutrition.FatProgress = Math.Round((totalFat / SelectedClient.DailyFatTarget) * 100, 1);
                if (SelectedClient.DailyCarbsTarget > 0)
                    ClientDailyNutrition.CarbsProgress = Math.Round((totalCarbs / SelectedClient.DailyCarbsTarget) * 100, 1);

                OnPropertyChanged(nameof(ClientDiaryEntries));
                OnPropertyChanged(nameof(ClientDailyNutrition));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки дневника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditClient()
        {
            if (SelectedClient == null) return;

            try
            {
                // Создаем простое окно редактирования через MessageBox с мультистрочным вводом
                string currentData = $"Текущие данные клиента {SelectedClient.Username}:\n" +
                                   $"Email: {SelectedClient.Email}\n" +
                                   $"Возраст: {SelectedClient.Age}\n" +
                                   $"Рост: {SelectedClient.Height}\n" +
                                   $"Вес: {SelectedClient.Weight}\n\n" +
                                   $"Введите новые данные (каждое значение с новой строки):\n" +
                                   $"Email\n" +
                                   $"Возраст\n" +
                                   $"Рост\n" +
                                   $"Вес";

                var result = MessageBox.Show(currentData, "Редактирование клиента",
                    MessageBoxButton.OKCancel, MessageBoxImage.Information);

                if (result == MessageBoxResult.OK)
                {
                    // Просим ввести новые данные через несколько MessageBox
                    string newEmail = ShowSimpleInputDialog("Введите новый email:", SelectedClient.Email);
                    if (newEmail == null) return;

                    string newAgeStr = ShowSimpleInputDialog("Введите новый возраст:", SelectedClient.Age.ToString());
                    if (newAgeStr == null || !int.TryParse(newAgeStr, out int newAge)) return;

                    string newHeightStr = ShowSimpleInputDialog("Введите новый рост (см):", SelectedClient.Height.ToString());
                    if (newHeightStr == null || !decimal.TryParse(newHeightStr, out decimal newHeight)) return;

                    string newWeightStr = ShowSimpleInputDialog("Введите новый вес (кг):", SelectedClient.Weight.ToString());
                    if (newWeightStr == null || !decimal.TryParse(newWeightStr, out decimal newWeight)) return;

                    // Подтверждение
                    var confirm = MessageBox.Show(
                        $"Подтвердите изменения:\n\n" +
                        $"Email: {SelectedClient.Email} → {newEmail}\n" +
                        $"Возраст: {SelectedClient.Age} → {newAge}\n" +
                        $"Рост: {SelectedClient.Height} → {newHeight}\n" +
                        $"Вес: {SelectedClient.Weight} → {newWeight}",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirm == MessageBoxResult.Yes)
                    {
                        // Находим и обновляем пользователя
                        var user = _context.Users.FirstOrDefault(u => u.UserId == SelectedClient.UserId);
                        if (user != null)
                        {
                            user.Email = newEmail;
                            user.Age = newAge;
                            user.Height = newHeight;
                            user.Weight = newWeight;

                            // Пересчитываем цели
                            user.DailyCalorieTarget = CalculateDailyCalories(user);
                            user.DailyProteinTarget = CalculateDailyProtein(user);
                            user.DailyFatTarget = CalculateDailyFat(user);
                            user.DailyCarbsTarget = CalculateDailyCarbs(user);

                            _context.SaveChanges();

                            // Обновляем список
                            LoadClients();
                            _mainVM.UpdateStatus($"Данные клиента {SelectedClient.Username} обновлены");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка редактирования: {ex.Message}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ShowSimpleInputDialog(string prompt, string defaultValue)
        {
            // Простой диалог ввода через TextBox в новом окне
            var dialog = new Window
            {
                Title = "Ввод данных",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            var promptText = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) };
            var inputBox = new TextBox { Text = defaultValue, Margin = new Thickness(0, 0, 0, 10) };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "Отмена", Width = 80 };

            string result = null;

            okButton.Click += (s, e) =>
            {
                result = inputBox.Text;
                dialog.DialogResult = true;
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.DialogResult = false;
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(promptText);
            stackPanel.Children.Add(inputBox);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            if (dialog.ShowDialog() == true)
            {
                return result;
            }

            return null;
        }

        private decimal CalculateDailyCalories(User user)
        {
            // Формула Миффлина-Сан Жеора
            decimal bmr = user.Gender == "Мужской"
                ? 10 * user.Weight + 6.25m * user.Height - 5 * user.Age + 5
                : 10 * user.Weight + 6.25m * user.Height - 5 * user.Age - 161;

            decimal activityMultiplier = user.ActivityLevel switch
            {
                "Низкая" => 1.2m,
                "Средняя" => 1.55m,
                "Высокая" => 1.9m,
                _ => 1.2m
            };

            decimal goalMultiplier = user.Goal switch
            {
                "Похудение" => 0.8m,
                "Набор массы" => 1.2m,
                _ => 1.0m
            };

            return Math.Round(bmr * activityMultiplier * goalMultiplier, 0);
        }

        private decimal CalculateDailyProtein(User user)
        {
            return Math.Round(CalculateDailyCalories(user) * 0.3m / 4, 0);
        }

        private decimal CalculateDailyFat(User user)
        {
            return Math.Round(CalculateDailyCalories(user) * 0.25m / 9, 0);
        }

        private decimal CalculateDailyCarbs(User user)
        {
            return Math.Round(CalculateDailyCalories(user) * 0.45m / 4, 0);
        }

        private async void CreatePlanForClient()
        {
            if (SelectedClient == null) return;

            try
            {
                // Просим ввести название плана
                string planName = ShowSimpleInputDialog("Введите название плана питания:",
                    $"План для {SelectedClient.Username} от {DateTime.Now:dd.MM.yyyy}");

                if (string.IsNullOrEmpty(planName)) return;

                var confirm = MessageBox.Show(
                    $"Создать план питания для клиента {SelectedClient.Username}?\n" +
                    $"Название: {planName}\n" +
                    $"Калории: {SelectedClient.DailyCalorieTarget} ккал/день\n" +
                    $"Белки: {SelectedClient.DailyProteinTarget} г/день\n" +
                    $"Жиры: {SelectedClient.DailyFatTarget} г/день\n" +
                    $"Углеводы: {SelectedClient.DailyCarbsTarget} г/день",
                    "Создание плана питания",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    var plan = new NutritionPlan
                    {
                        UserId = SelectedClient.UserId,
                        PlanName = planName,
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddDays(7),
                        DailyCalories = SelectedClient.DailyCalorieTarget,
                        DailyProtein = SelectedClient.DailyProteinTarget,
                        DailyFat = SelectedClient.DailyFatTarget,
                        DailyCarbohydrates = SelectedClient.DailyCarbsTarget,
                        Status = "Активен"
                    };

                    _context.NutritionPlans.Add(plan);
                    await _context.SaveChangesAsync();

                    LoadClientPlans();
                    _mainVM.UpdateStatus($"Создан план питания '{planName}' для {SelectedClient.Username}");
                }
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка создания плана: {ex.Message}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewClientDiary()
        {
            if (SelectedClient == null) return;

            string diaryInfo = $"Дневник питания клиента: {SelectedClient.Username}\n" +
                              $"Дата: {SelectedDate:dd.MM.yyyy}\n" +
                              $"Всего записей: {ClientDiaryEntries.Count}\n\n";

            if (ClientDiaryEntries.Any())
            {
                diaryInfo += "Записи за день:\n";
                foreach (var entry in ClientDiaryEntries.Take(10)) // Показываем первые 10 записей
                {
                    diaryInfo += $"{entry.Date:HH:mm} - {entry.ProductName} ({entry.Quantity}г)\n" +
                                $"  Калории: {entry.Calories}, Белки: {entry.Protein}г, Жиры: {entry.Fat}г, Углеводы: {entry.Carbohydrates}г\n";
                }

                if (ClientDiaryEntries.Count > 10)
                {
                    diaryInfo += $"\n... и еще {ClientDiaryEntries.Count - 10} записей\n";
                }
            }
            else
            {
                diaryInfo += "Записей за этот день нет.";
            }

            diaryInfo += $"\nИтого за день:\n" +
                        $"Калории: {ClientDailyNutrition.TotalCalories:F0} / {SelectedClient.DailyCalorieTarget} ({ClientDailyNutrition.CaloriesProgress:F1}%)\n" +
                        $"Белки: {ClientDailyNutrition.TotalProtein:F1} г / {SelectedClient.DailyProteinTarget} г ({ClientDailyNutrition.ProteinProgress:F1}%)\n" +
                        $"Жиры: {ClientDailyNutrition.TotalFat:F1} г / {SelectedClient.DailyFatTarget} г ({ClientDailyNutrition.FatProgress:F1}%)\n" +
                        $"Углеводы: {ClientDailyNutrition.TotalCarbs:F1} г / {SelectedClient.DailyCarbsTarget} г ({ClientDailyNutrition.CarbsProgress:F1}%)";

            MessageBox.Show(diaryInfo, "Дневник питания", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeletePlan()
        {
            if (SelectedPlan == null) return;

            var result = MessageBox.Show($"Удалить план '{SelectedPlan.PlanName}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var plan = await _context.NutritionPlans.FindAsync(SelectedPlan.PlanId);
                    if (plan != null)
                    {
                        _context.NutritionPlans.Remove(plan);
                        await _context.SaveChangesAsync();

                        ClientPlans.Remove(SelectedPlan);
                        _mainVM.UpdateStatus($"План '{SelectedPlan.PlanName}' удален");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления плана: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ActivatePlan()
        {
            if (SelectedPlan == null) return;

            try
            {
                // Деактивируем все планы клиента
                var clientPlans = await _context.NutritionPlans
                    .Where(p => p.UserId == SelectedClient.UserId)
                    .ToListAsync();

                foreach (var plan in clientPlans)
                {
                    plan.Status = "Неактивен";
                }

                // Активируем выбранный план
                var selectedPlan = await _context.NutritionPlans.FindAsync(SelectedPlan.PlanId);
                if (selectedPlan != null)
                {
                    selectedPlan.Status = "Активен";
                    await _context.SaveChangesAsync();

                    _mainVM.UpdateStatus($"План '{selectedPlan.PlanName}' активирован");
                    LoadClientPlans();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка активации плана: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportClientData()
        {
            if (SelectedClient == null) return;

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    FileName = $"Client_{SelectedClient.Username}_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".txt",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = "Экспорт данных клиента"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string report = GenerateClientReport();
                    File.WriteAllText(saveDialog.FileName, report, System.Text.Encoding.UTF8);

                    _mainVM.UpdateStatus($"Данные клиента экспортированы в {saveDialog.FileName}");
                    MessageBox.Show($"Данные успешно экспортированы в файл:\n{saveDialog.FileName}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка экспорта: {ex.Message}");
                MessageBox.Show($"Ошибка экспорта данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPlan()
        {
            if (SelectedPlan == null) return;

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    FileName = $"Plan_{SelectedPlan.PlanName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}",
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
                MessageBox.Show($"Ошибка экспорта плана: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviousDay()
        {
            SelectedDate = SelectedDate.AddDays(-1);
        }

        private void NextDay()
        {
            SelectedDate = SelectedDate.AddDays(1);
        }

        private string GenerateClientReport()
        {
            if (SelectedClient == null) return "";

            var report = $"=== ОТЧЕТ ПО КЛИЕНТУ ===\n\n" +
                        $"ОСНОВНАЯ ИНФОРМАЦИЯ:\n" +
                        $"Имя: {SelectedClient.Username}\n" +
                        $"Email: {SelectedClient.Email}\n" +
                        $"Возраст: {SelectedClient.Age}\n" +
                        $"Пол: {SelectedClient.Gender}\n" +
                        $"Рост: {SelectedClient.Height} см\n" +
                        $"Вес: {SelectedClient.Weight} кг\n" +
                        $"Уровень активности: {SelectedClient.ActivityLevel}\n" +
                        $"Цель: {SelectedClient.Goal}\n\n" +
                        $"ЦЕЛЕВЫЕ ПОКАЗАТЕЛИ:\n" +
                        $"Калории: {SelectedClient.DailyCalorieTarget} ккал/день\n" +
                        $"Белки: {SelectedClient.DailyProteinTarget} г/день\n" +
                        $"Жиры: {SelectedClient.DailyFatTarget} г/день\n" +
                        $"Углеводы: {SelectedClient.DailyCarbsTarget} г/день\n\n";

            // Добавляем информацию о планах
            if (ClientPlans.Any())
            {
                report += $"ПЛАНЫ ПИТАНИЯ ({ClientPlans.Count}):\n";
                foreach (var plan in ClientPlans)
                {
                    report += $"• {plan.PlanName}\n" +
                             $"  Период: {plan.StartDate:dd.MM.yyyy} - {plan.EndDate:dd.MM.yyyy}\n" +
                             $"  Калории: {plan.DailyCalories} ккал/день\n" +
                             $"  Белки: {plan.DailyProtein} г/день\n" +
                             $"  Жиры: {plan.DailyFat} г/день\n" +
                             $"  Углеводы: {plan.DailyCarbohydrates} г/день\n\n";
                }
            }

            // Добавляем информацию о дневнике за сегодня
            report += $"СТАТИСТИКА ЗА {SelectedDate:dd.MM.yyyy}:\n" +
                     $"Всего записей: {ClientDiaryEntries.Count}\n" +
                     $"Потреблено калорий: {ClientDailyNutrition.TotalCalories:F1}\n" +
                     $"Потреблено белков: {ClientDailyNutrition.TotalProtein:F1} г\n" +
                     $"Потреблено жиров: {ClientDailyNutrition.TotalFat:F1} г\n" +
                     $"Потреблено углеводов: {ClientDailyNutrition.TotalCarbs:F1} г\n\n" +
                     $"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n" +
                     $"=== КОНЕЦ ОТЧЕТА ===";

            return report;
        }

        private string GeneratePlanReport()
        {
            if (SelectedPlan == null) return "";

            var report = $"=== ПЛАН ПИТАНИЯ ===\n\n" +
                        $"ОСНОВНАЯ ИНФОРМАЦИЯ:\n" +
                        $"Название: {SelectedPlan.PlanName}\n" +
                        $"Период действия: {SelectedPlan.StartDate:dd.MM.yyyy} - {SelectedPlan.EndDate:dd.MM.yyyy}\n" +
                        $"Длительность: {(SelectedPlan.EndDate - SelectedPlan.StartDate).Days + 1} дней\n\n" +
                        $"ДНЕВНЫЕ НОРМЫ:\n" +
                        $"Калории: {SelectedPlan.DailyCalories} ккал\n" +
                        $"Белки: {SelectedPlan.DailyProtein} г ({SelectedPlan.DailyProtein * 4} ккал)\n" +
                        $"Жиры: {SelectedPlan.DailyFat} г ({SelectedPlan.DailyFat * 9} ккал)\n" +
                        $"Углеводы: {SelectedPlan.DailyCarbohydrates} г ({SelectedPlan.DailyCarbohydrates * 4} ккал)\n\n" +
                        $"РАСПРЕДЕЛЕНИЕ КАЛОРИЙ:\n" +
                        $"Белки: {SelectedPlan.DailyProtein * 4 / SelectedPlan.DailyCalories * 100:F1}%\n" +
                        $"Жиры: {SelectedPlan.DailyFat * 9 / SelectedPlan.DailyCalories * 100:F1}%\n" +
                        $"Углеводы: {SelectedPlan.DailyCarbohydrates * 4 / SelectedPlan.DailyCalories * 100:F1}%\n\n" +
                        $"РЕКОМЕНДАЦИИ:\n" +
                        $"1. Распределите приемы пищи на 4-6 раз в день\n" +
                        $"2. Пейте достаточное количество воды\n" +
                        $"3. Соблюдайте баланс белков, жиров и углеводов\n" +
                        $"4. Включайте в рацион овощи и фрукты\n\n" +
                        $"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n" +
                        $"=== КОНЕЦ ОТЧЕТА ===";

            return report;
        }
    }
}
