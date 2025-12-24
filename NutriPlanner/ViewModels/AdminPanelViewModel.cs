using NutriPlanner.Data;
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
using System.Windows.Controls;

namespace NutriPlanner.ViewModels
{
    public class AdminPanelViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private readonly User _currentUser;
        private ObservableCollection<UserDto> _allUsers;
        private string _searchText = "";
        private UserDto _selectedUser;
        private bool _isLoading;

        public ObservableCollection<UserDto> AllUsers
        {
            get => _allUsers;
            set { _allUsers = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = FilterUsersAsync();
            }
        }

        public UserDto SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                // Напрямую обновляем CanExecute команд
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Статистика
        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalDietitians { get; set; }
        public int TotalRegularUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalPlans { get; set; }

        // Команды
        public ICommand RefreshCommand { get; }
        public ICommand ToggleUserStatusCommand { get; }
        public ICommand ChangeUserRoleCommand { get; }

        public AdminPanelViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;
            AllUsers = new ObservableCollection<UserDto>();

            // Создаем команды с правильными условиями
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            ToggleUserStatusCommand = new RelayCommand(
                () => ToggleUserStatus(),
                () => SelectedUser != null && !IsLoading);

            ChangeUserRoleCommand = new RelayCommand(
                () => ChangeUserRole(),
                () => SelectedUser != null && !IsLoading);

            LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                using var context = new DatabaseContext();

                // Загружаем всех пользователей с ролями
                var users = await context.Users
                    .Include(u => u.Role)
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                AllUsers.Clear();
                foreach (var user in users)
                {
                    AllUsers.Add(new UserDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        RoleName = user.Role?.RoleName ?? "Неизвестно",
                        RoleId = user.RoleId,
                        IsActive = user.IsActive,
                        RegistrationDate = user.RegistrationDate
                    });
                }

                // Загружаем статистику
                var products = await context.Products.CountAsync();
                var plans = await context.NutritionPlans.CountAsync();

                TotalUsers = users.Count;
                TotalAdmins = users.Count(u => u.Role?.RoleName == "Admin");
                TotalDietitians = users.Count(u => u.Role?.RoleName == "Dietitian");
                TotalRegularUsers = users.Count(u => u.Role?.RoleName == "User");
                TotalProducts = products;
                TotalPlans = plans;

                OnPropertyChanged(nameof(TotalUsers));
                OnPropertyChanged(nameof(TotalAdmins));
                OnPropertyChanged(nameof(TotalDietitians));
                OnPropertyChanged(nameof(TotalRegularUsers));
                OnPropertyChanged(nameof(TotalProducts));
                OnPropertyChanged(nameof(TotalPlans));

                _mainVM.UpdateStatus("Данные админ-панели загружены");
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task FilterUsersAsync()
        {
            try
            {
                IsLoading = true;

                using var context = new DatabaseContext();

                var usersQuery = context.Users
                    .Include(u => u.Role)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    usersQuery = usersQuery.Where(u =>
                        u.Username.Contains(SearchText) ||
                        u.Email.Contains(SearchText) ||
                        (u.Role != null && u.Role.RoleName.Contains(SearchText)));
                }

                var users = await usersQuery
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                AllUsers.Clear();
                foreach (var user in users)
                {
                    AllUsers.Add(new UserDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        RoleName = user.Role?.RoleName ?? "Неизвестно",
                        RoleId = user.RoleId,
                        IsActive = user.IsActive,
                        RegistrationDate = user.RegistrationDate
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ToggleUserStatus()
        {
            try
            {
                if (SelectedUser == null)
                {
                    MessageBox.Show("Сначала выберите пользователя из списка", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newStatus = !SelectedUser.IsActive;
                var statusText = newStatus ? "активен" : "неактивен";

                var result = MessageBox.Show(
                    $"Изменить статус пользователя '{SelectedUser.Username}' на '{statusText}'?",
                    "Изменение статуса",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Безопасно обновляем статус
                    var userId = SelectedUser.UserId;
                    var username = SelectedUser.Username;

                    _ = UpdateUserStatusAsync(userId, username, newStatus);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateUserStatusAsync(int userId, string username, bool newStatus)
        {
            try
            {
                using var context = new DatabaseContext();
                var user = await context.Users.FindAsync(userId);

                if (user != null)
                {
                    user.IsActive = newStatus;
                    await context.SaveChangesAsync();

                    // Находим и обновляем в списке
                    var userInList = AllUsers.FirstOrDefault(u => u.UserId == userId);
                    if (userInList != null)
                    {
                        userInList.IsActive = newStatus;

                        // Обновляем отображение
                        var index = AllUsers.IndexOf(userInList);
                        AllUsers.RemoveAt(index);
                        AllUsers.Insert(index, userInList);

                        // Если это выбранный пользователь, обновляем SelectedUser
                        if (SelectedUser != null && SelectedUser.UserId == userId)
                        {
                            SelectedUser.IsActive = newStatus;
                            OnPropertyChanged(nameof(SelectedUser));
                        }
                    }

                    var statusText = newStatus ? "активен" : "неактивен";
                    _mainVM.UpdateStatus($"Статус пользователя '{username}' изменен на '{statusText}'");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения статуса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangeUserRole()
        {
            try
            {
                if (SelectedUser == null)
                {
                    MessageBox.Show("Сначала выберите пользователя из списка", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Простой диалог для выбора роли
                string newRole = ShowRoleSelectionDialog();
                if (!string.IsNullOrEmpty(newRole))
                {
                    var userId = SelectedUser.UserId;
                    var username = SelectedUser.Username;

                    _ = UpdateUserRoleAsync(userId, username, newRole);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ShowRoleSelectionDialog()
        {
            // Простое окно выбора роли
            var dialog = new Window
            {
                Title = "Выбор роли",
                Width = 300,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var promptText = new TextBlock
            {
                Text = $"Выберите роль для пользователя:\n{SelectedUser.Username}",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            var comboBox = new ComboBox
            {
                ItemsSource = new[] { "User", "Dietitian", "Admin" },
                SelectedItem = SelectedUser.RoleName,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 80,
                IsCancel = true
            };

            string selectedRole = null;

            okButton.Click += (s, e) =>
            {
                selectedRole = comboBox.SelectedItem as string;
                dialog.DialogResult = true;
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.DialogResult = false;
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(promptText);
            stackPanel.Children.Add(comboBox);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            return dialog.ShowDialog() == true ? selectedRole : null;
        }

        private async Task UpdateUserRoleAsync(int userId, string username, string newRoleName)
        {
            try
            {
                using var context = new DatabaseContext();
                var user = await context.Users.FindAsync(userId);
                var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == newRoleName);

                if (user != null && role != null)
                {
                    user.RoleId = role.RoleId;
                    await context.SaveChangesAsync();

                    // Находим и обновляем в списке
                    var userInList = AllUsers.FirstOrDefault(u => u.UserId == userId);
                    if (userInList != null)
                    {
                        userInList.RoleName = newRoleName;
                        userInList.RoleId = role.RoleId;

                        // Обновляем отображение
                        var index = AllUsers.IndexOf(userInList);
                        AllUsers.RemoveAt(index);
                        AllUsers.Insert(index, userInList);

                        // Если это выбранный пользователь, обновляем SelectedUser
                        if (SelectedUser != null && SelectedUser.UserId == userId)
                        {
                            SelectedUser.RoleName = newRoleName;
                            SelectedUser.RoleId = role.RoleId;
                            OnPropertyChanged(nameof(SelectedUser));
                        }
                    }

                    // Обновляем статистику
                    await UpdateStatistics();

                    _mainVM.UpdateStatus($"Роль пользователя '{username}' изменена на '{newRoleName}'");
                    MessageBox.Show($"Роль успешно изменена на '{newRoleName}'", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка изменения роли: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateStatistics()
        {
            try
            {
                using var context = new DatabaseContext();

                var users = await context.Users.Include(u => u.Role).ToListAsync();

                TotalUsers = users.Count;
                TotalAdmins = users.Count(u => u.Role?.RoleName == "Admin");
                TotalDietitians = users.Count(u => u.Role?.RoleName == "Dietitian");
                TotalRegularUsers = users.Count(u => u.Role?.RoleName == "User");

                OnPropertyChanged(nameof(TotalUsers));
                OnPropertyChanged(nameof(TotalAdmins));
                OnPropertyChanged(nameof(TotalDietitians));
                OnPropertyChanged(nameof(TotalRegularUsers));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления статистики: {ex.Message}");
            }
        }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
