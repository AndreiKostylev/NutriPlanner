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
                InitializeTargets();
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
        /// Инициализирует целевые показатели
        /// </summary>
        private void InitializeTargets()
        {
            NutritionData.TargetCalories = 2000;
            NutritionData.TargetProtein = 150;
            NutritionData.TargetFat = 67;
            NutritionData.TargetCarbs = 250;
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

                // Обновляем общие показатели
                NutritionData.TotalCalories += calories;
                NutritionData.TotalProtein += protein;
                NutritionData.TotalFat += fat;
                NutritionData.TotalCarbs += carbs;

                // Добавляем запись в список приемов пищи
                NutritionData.Meals.Add(new MealDto
                {
                    MealName = $"{SelectedProduct.ProductName} ({ProductQuantity}г)",
                    Calories = calories,
                    Protein = protein,
                    Fat = fat,
                    Carbs = carbs
                });

                OnPropertyChanged(nameof(NutritionData));
                _mainViewModel.UpdateStatus($"Добавлен: {SelectedProduct.ProductName}");
            }
            catch (Exception ex)
            {
                _mainViewModel.UpdateStatus($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Рассчитывает пищевую ценность продукта (Паттерн Strategy)
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
                    UserId = 1 // Временное значение
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
        /// Рассчитывает целевые показатели
        /// </summary>
        private void CalculateTargets()
        {
            // Здесь будет расчет по формуле Миффлина-Сан Жеора
            _mainViewModel.UpdateStatus("Расчет выполнен");
        }

        /// <summary>
        /// Очищает данные дневника
        /// </summary>
        private void ClearData()
        {
            NutritionData = new DailyNutritionDto();
            InitializeTargets();
            OnPropertyChanged(nameof(NutritionData));
            _mainViewModel.UpdateStatus("Данные очищены");
        }

        private bool CanAddProduct() => SelectedProduct != null && ProductQuantity > 0;
    }
}
