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

namespace NutriPlanner.ViewModels
{
    /// <summary>
    /// ViewModel для управления дневником питания
    /// </summary>
    public class DailyNutritionViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DatabaseContext _context;
        private DailyNutritionDto _nutritionData;
        private ObservableCollection<ProductDto> _availableProducts;
        private ProductDto _selectedProduct;
        private decimal _productQuantity = 100;

        // Поля для расчета норм
        private int _userAge = 30;
        private string _userGender = "Мужской";
        private decimal _userHeight = 180;
        private decimal _userWeight = 75;
        private string _activityLevel = "Умеренная";
        private string _goal = "Поддержание";

        public DailyNutritionDto NutritionData
        {
            get => _nutritionData;
            set { _nutritionData = value; OnPropertyChanged(); }
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

        // Свойства для ввода данных пользователя
        public int UserAge
        {
            get => _userAge;
            set { _userAge = value; OnPropertyChanged(); }
        }

        public string UserGender
        {
            get => _userGender;
            set { _userGender = value; OnPropertyChanged(); }
        }

        public decimal UserHeight
        {
            get => _userHeight;
            set { _userHeight = value; OnPropertyChanged(); }
        }

        public decimal UserWeight
        {
            get => _userWeight;
            set { _userWeight = value; OnPropertyChanged(); }
        }

        public string ActivityLevel
        {
            get => _activityLevel;
            set { _activityLevel = value; OnPropertyChanged(); }
        }

        public string Goal
        {
            get => _goal;
            set { _goal = value; OnPropertyChanged(); }
        }

        // Списки для ComboBox
        public List<string> Genders { get; } = new List<string> { "Мужской", "Женский" };
        public List<string> ActivityLevels { get; } = new List<string>
        {
            "Сидячий",
            "Легкая",
            "Умеренная",
            "Активная",
            "Очень активная"
        };
        public List<string> Goals { get; } = new List<string>
        {
            "Похудение",
            "Поддержание",
            "Набор массы"
        };

        // Команды
        public ICommand AddProductCommand { get; }
        public ICommand ClearDataCommand { get; }
        public ICommand SaveToDatabaseCommand { get; }
        public ICommand CalculateTargetsCommand { get; }

        public DailyNutritionViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _context = new DatabaseContext();
            _nutritionData = new DailyNutritionDto();
            _availableProducts = new ObservableCollection<ProductDto>();

            // Инициализация команд
            AddProductCommand = new RelayCommand(AddProduct, CanAddProduct);
            ClearDataCommand = new RelayCommand(ClearData);
            SaveToDatabaseCommand = new RelayCommand(SaveToDatabase);
            CalculateTargetsCommand = new RelayCommand(CalculateTargets);

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
                CalculateTargets(); // Рассчитываем целевые показатели при запуске
                _mainViewModel.UpdateStatus("Данные загружены успешно");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка загрузки: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает продукты из базы данных
        /// </summary>
        private async Task LoadProductsFromDatabase()
        {
            var products = await _context.Products.ToListAsync();
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

        /// <summary>
        /// Рассчитывает целевые показатели по формуле Миффлина-Сан Жеора
        /// </summary>
        private void CalculateTargets()
        {
            try
            {
               
                if (UserAge <= 0 || UserHeight <= 0 || UserWeight <= 0)
                {
                    _mainViewModel.UpdateStatus("Ошибка: Проверьте возраст, рост и вес");
                    return;
                }

              
                decimal bmr;
                if (UserGender == "Мужской")
                {
                    bmr = 10 * UserWeight + 6.25m * UserHeight - 5 * UserAge + 5;
                }
                else
                {
                    bmr = 10 * UserWeight + 6.25m * UserHeight - 5 * UserAge - 161;
                }

               
                decimal activityMultiplier = GetActivityMultiplier(ActivityLevel);

            
                decimal tdee = bmr * activityMultiplier;

              
                decimal goalMultiplier = GetGoalMultiplier(Goal);
                decimal targetCalories = tdee * goalMultiplier;

               
                decimal targetProtein = targetCalories * 0.3m / 4; 
                decimal targetFat = targetCalories * 0.25m / 9;     
                decimal targetCarbs = targetCalories * 0.45m / 4;  

              
                NutritionData.TargetCalories = Math.Round(targetCalories, 2);
                NutritionData.TargetProtein = Math.Round(targetProtein, 2);
                NutritionData.TargetFat = Math.Round(targetFat, 2);
                NutritionData.TargetCarbs = Math.Round(targetCarbs, 2);

               
                UpdateProgress();

                _mainViewModel.UpdateStatus($"Расчет выполнен: {NutritionData.TargetCalories} ккал/день");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка расчета: {ex.Message}");
            }
        }

        /// <summary>
        /// Возвращает коэффициент физической активности
        /// </summary>
        private decimal GetActivityMultiplier(string activityLevel)
        {
            return activityLevel switch
            {
                "Сидячий" => 1.2m,
                "Легкая" => 1.375m,
                "Умеренная" => 1.55m,
                "Активная" => 1.725m,
                "Очень активная" => 1.9m,
                _ => 1.2m
            };
        }

        /// <summary>
        /// Возвращает коэффициент цели
        /// </summary>
        private decimal GetGoalMultiplier(string goal)
        {
            return goal switch
            {
                "Похудение" => 0.8m,
                "Поддержание" => 1.0m,
                "Набор массы" => 1.2m,
                _ => 1.0m
            };
        }

        /// <summary>
        /// Обновляет прогресс выполнения дневных целей
        /// </summary>
        private void UpdateProgress()
        {
            if (NutritionData.TargetCalories > 0)
                NutritionData.CaloriesProgress = Math.Round((NutritionData.TotalCalories / NutritionData.TargetCalories) * 100, 1);

            if (NutritionData.TargetProtein > 0)
                NutritionData.ProteinProgress = Math.Round((NutritionData.TotalProtein / NutritionData.TargetProtein) * 100, 1);

            if (NutritionData.TargetFat > 0)
                NutritionData.FatProgress = Math.Round((NutritionData.TotalFat / NutritionData.TargetFat) * 100, 1);

            if (NutritionData.TargetCarbs > 0)
                NutritionData.CarbsProgress = Math.Round((NutritionData.TotalCarbs / NutritionData.TargetCarbs) * 100, 1);
        }

        /// <summary>
        /// Добавляет продукт в дневник питания
        /// </summary>
        private void AddProduct()
        {
            if (SelectedProduct == null) return;

            try
            {
                var (calories, protein, fat, carbs) = CalculateNutrition(SelectedProduct, ProductQuantity);

                
                NutritionData.TotalCalories += calories;
                NutritionData.TotalProtein += protein;
                NutritionData.TotalFat += fat;
                NutritionData.TotalCarbs += carbs;


                NutritionData.Meals.Add(new MealDto
                {
                    MealName = $"{SelectedProduct.ProductName} ({ProductQuantity}г)",
                    Calories = calories,
                    Protein = protein,
                    Fat = fat,
                    Carbs = carbs
                });

                // Обновляем прогресс
                UpdateProgress();

                OnPropertyChanged(nameof(NutritionData));
                _mainViewModel.UpdateStatus($"Добавлен: {SelectedProduct.ProductName}");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Рассчитывает пищевую ценность продукта
        /// </summary>
        private (decimal calories, decimal protein, decimal fat, decimal carbs) CalculateNutrition(ProductDto product, decimal quantity)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentException("Количество должно быть положительным");

            var multiplier = quantity / 100;
            return (
                Math.Round(product.Calories * multiplier, 2),
                Math.Round(product.Protein * multiplier, 2),
                Math.Round(product.Fat * multiplier, 2),
                Math.Round(product.Carbohydrates * multiplier, 2)
            );
        }

        /// <summary>
        /// Сохраняет данные в базу данных
        /// </summary>
        private async void SaveToDatabase()
        {
            try
            {
                var foodEntry = new FoodDiary
                {
                    Date = DateTime.Today,
                    ProductId = SelectedProduct?.ProductId,
                    Quantity = ProductQuantity,
                    Calories = NutritionData.TotalCalories,
                    Protein = NutritionData.TotalProtein,
                    Fat = NutritionData.TotalFat,
                    Carbohydrates = NutritionData.TotalCarbs,
                    UserId = 1 
                };

                await _context.FoodDiaries.AddAsync(foodEntry);
                await _context.SaveChangesAsync();

                _mainViewModel.UpdateStatus("Данные сохранены в базу");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка сохранения: {ex.Message}");
            }
        }

        /// <summary>
        /// Очищает данные дневника
        /// </summary>
        private void ClearData()
        {
            NutritionData = new DailyNutritionDto();
            CalculateTargets(); 
            OnPropertyChanged(nameof(NutritionData));
            _mainViewModel.UpdateStatus("Данные очищены");
        }

        private bool CanAddProduct() => SelectedProduct != null && ProductQuantity > 0;
    }
}
