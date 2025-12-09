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

        public bool CanEditClient => SelectedClient != null;

        public ICommand LoadClientsCommand { get; }
        public ICommand EditClientCommand { get; }
        public ICommand CreatePlanForClientCommand { get; }
        public ICommand ViewClientDiaryCommand { get; }
        public ICommand ExportClientDataCommand { get; }

        public ClientManagementViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;
            _context = new DatabaseContext();

            Clients = new ObservableCollection<UserProfileDto>();

            LoadClientsCommand = new RelayCommand(LoadClients);
            EditClientCommand = new RelayCommand(EditClient, () => CanEditClient);
            CreatePlanForClientCommand = new RelayCommand(CreatePlanForClient, () => CanEditClient);
            ViewClientDiaryCommand = new RelayCommand(ViewClientDiary, () => CanEditClient);
            ExportClientDataCommand = new RelayCommand(ExportClientData, () => CanEditClient);

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

        private void EditClient()
        {
            if (SelectedClient == null) return;

            _mainVM.UpdateStatus($"Редактирование клиента: {SelectedClient.Username}");
            MessageBox.Show($"Функция редактирования клиента будет реализована в следующем обновлении\n" +
                          $"Клиент: {SelectedClient.Username}\n" +
                          $"Email: {SelectedClient.Email}",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreatePlanForClient()
        {
            if (SelectedClient == null) return;

            _mainVM.UpdateStatus($"Создание плана для: {SelectedClient.Username}");
            MessageBox.Show($"Создание плана питания для клиента будет реализовано в следующем обновлении\n" +
                          $"Клиент: {SelectedClient.Username}\n" +
                          $"Целевые калории: {SelectedClient.DailyCalorieTarget}",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewClientDiary()
        {
            if (SelectedClient == null) return;

            _mainVM.UpdateStatus($"Просмотр дневника: {SelectedClient.Username}");
            MessageBox.Show($"Просмотр дневника питания клиента будет реализован в следующем обновлении\n" +
                          $"Клиент: {SelectedClient.Username}",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportClientData()
        {
            if (SelectedClient == null) return;

            try
            {
                var report = $"=== ОТЧЕТ ПО КЛИЕНТУ ===\n" +
                            $"Имя: {SelectedClient.Username}\n" +
                            $"Email: {SelectedClient.Email}\n" +
                            $"Возраст: {SelectedClient.Age}\n" +
                            $"Пол: {SelectedClient.Gender}\n" +
                            $"Рост: {SelectedClient.Height} см\n" +
                            $"Вес: {SelectedClient.Weight} кг\n" +
                            $"Активность: {SelectedClient.ActivityLevel}\n" +
                            $"Цель: {SelectedClient.Goal}\n" +
                            $"\nЦелевые показатели:\n" +
                            $"  Калории: {SelectedClient.DailyCalorieTarget} ккал\n" +
                            $"  Белки: {SelectedClient.DailyProteinTarget} г\n" +
                            $"  Жиры: {SelectedClient.DailyFatTarget} г\n" +
                            $"  Углеводы: {SelectedClient.DailyCarbsTarget} г\n" +
                            $"\nОтчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}";

                MessageBox.Show(report, $"Отчет: {SelectedClient.Username}",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _mainVM.UpdateStatus($"Отчет сформирован для: {SelectedClient.Username}");
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка экспорта: {ex.Message}");
                MessageBox.Show($"Ошибка экспорта данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
