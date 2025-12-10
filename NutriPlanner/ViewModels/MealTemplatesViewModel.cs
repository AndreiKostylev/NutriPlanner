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
    /// ViewModel для управления шаблонами блюд
    /// </summary>
    public class MealTemplatesViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private readonly User _currentUser;

        // НЕ храним контекст как поле, создаем каждый раз новый
        private ObservableCollection<DishDto> _dishTemplates;
        private DishDto _selectedTemplate;
        private ObservableCollection<ProductDto> _availableProducts;
        private ProductDto _selectedProduct;
        private string _searchText = "";
        private decimal _ingredientQuantity = 100;

        public ObservableCollection<DishDto> DishTemplates
        {
            get => _dishTemplates;
            set { _dishTemplates = value; OnPropertyChanged(); }
        }

        public DishDto SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEditTemplate));
                if (value != null)
                {
                    LoadTemplateIngredients();
                }
            }
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

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                LoadDishTemplates();
            }
        }

        public decimal IngredientQuantity
        {
            get => _ingredientQuantity;
            set { _ingredientQuantity = value; OnPropertyChanged(); }
        }

        public bool CanEditTemplate => SelectedTemplate != null;

        // Для отображения ингредиентов выбранного шаблона
        public ObservableCollection<ProductDto> TemplateIngredients { get; set; }

        // Команды
        public ICommand LoadTemplatesCommand { get; }
        public ICommand LoadProductsCommand { get; }
        public ICommand CreateTemplateCommand { get; }
        public ICommand SaveTemplateCommand { get; }
        public ICommand DeleteTemplateCommand { get; }
        public ICommand AddIngredientCommand { get; }
        public ICommand RemoveIngredientCommand { get; }
        public ICommand CalculateNutritionCommand { get; }

        public MealTemplatesViewModel(MainViewModel mainVM, User currentUser)
        {
            _mainVM = mainVM;
            _currentUser = currentUser;

            DishTemplates = new ObservableCollection<DishDto>();
            AvailableProducts = new ObservableCollection<ProductDto>();
            TemplateIngredients = new ObservableCollection<ProductDto>();

            LoadTemplatesCommand = new RelayCommand(LoadDishTemplates);
            LoadProductsCommand = new RelayCommand(LoadProducts);
            CreateTemplateCommand = new RelayCommand(CreateTemplate);
            SaveTemplateCommand = new RelayCommand(SaveTemplate, () => CanEditTemplate);
            DeleteTemplateCommand = new RelayCommand(DeleteTemplate, () => CanEditTemplate);
            AddIngredientCommand = new RelayCommand(AddIngredient, () => SelectedProduct != null && IngredientQuantity > 0);
            RemoveIngredientCommand = new RelayCommand(RemoveIngredient, () => SelectedProduct != null && CanEditTemplate);
            CalculateNutritionCommand = new RelayCommand(CalculateNutrition, () => CanEditTemplate);

            LoadProducts();
            LoadDishTemplates();
        }

        private async void LoadDishTemplates()
        {
            try
            {
                using (var context = new DatabaseContext())
                {
                    DishTemplates.Clear();

                    var query = context.Dishes
                        .Include(d => d.User)
                        .Where(d => d.UserId == _currentUser.UserId); // Только свои шаблоны

                    // Фильтрация по поиску
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        query = query.Where(d => d.DishName.Contains(SearchText));
                    }

                    var dishes = await query.OrderBy(d => d.DishName).ToListAsync();

                    foreach (var dish in dishes)
                    {
                        // Загружаем ингредиенты для блюда в ОТДЕЛЬНОМ запросе
                        var ingredients = await context.DishProducts
                            .Include(dp => dp.Product)
                            .Where(dp => dp.DishId == dish.DishId)
                            .ToListAsync();

                        var dishDto = new DishDto
                        {
                            DishId = dish.DishId,
                            DishName = dish.DishName,
                            TotalCalories = dish.TotalCalories,
                            TotalProtein = dish.TotalProtein,
                            TotalFat = dish.TotalFat,
                            TotalCarbohydrates = dish.TotalCarbohydrates,
                            Recipe = "Автоматически сгенерированный рецепт"
                        };

                        // Добавляем ингредиенты в DTO
                        foreach (var ingredient in ingredients)
                        {
                            dishDto.Ingredients.Add(new ProductDto
                            {
                                ProductId = ingredient.Product.ProductId,
                                ProductName = ingredient.Product.ProductName,
                                Category = ingredient.Product.Category,
                                Calories = ingredient.Product.Calories,
                                Protein = ingredient.Product.Protein,
                                Fat = ingredient.Product.Fat,
                                Carbohydrates = ingredient.Product.Carbohydrates,
                                Unit = ingredient.Product.Unit,
                                Quantity = ingredient.Quantity
                            });
                        }

                        DishTemplates.Add(dishDto);
                    }
                }

                _mainVM.UpdateStatus($"Загружено {DishTemplates.Count} шаблонов блюд");
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка загрузки шаблонов: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки шаблонов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadProducts()
        {
            try
            {
                using (var context = new DatabaseContext())
                {
                    AvailableProducts.Clear();

                    var products = await context.Products
                        .OrderBy(p => p.ProductName)
                        .ToListAsync();

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
                            Unit = product.Unit,
                            Quantity = 100 // Значение по умолчанию
                        });
                    }

                    if (AvailableProducts.Any())
                    {
                        SelectedProduct = AvailableProducts.First();
                    }

                    _mainVM.UpdateStatus($"Загружено {AvailableProducts.Count} продуктов");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продуктов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadTemplateIngredients()
        {
            try
            {
                if (SelectedTemplate == null) return;

                using (var context = new DatabaseContext())
                {
                    TemplateIngredients.Clear();

                    var ingredients = await context.DishProducts
                        .Include(dp => dp.Product)
                        .Where(dp => dp.DishId == SelectedTemplate.DishId)
                        .ToListAsync();

                    foreach (var ingredient in ingredients)
                    {
                        TemplateIngredients.Add(new ProductDto
                        {
                            ProductId = ingredient.Product.ProductId,
                            ProductName = ingredient.Product.ProductName,
                            Category = ingredient.Product.Category,
                            Calories = ingredient.Product.Calories,
                            Protein = ingredient.Product.Protein,
                            Fat = ingredient.Product.Fat,
                            Carbohydrates = ingredient.Product.Carbohydrates,
                            Unit = ingredient.Product.Unit,
                            Quantity = ingredient.Quantity
                        });
                    }

                    OnPropertyChanged(nameof(TemplateIngredients));
                    CalculateNutrition();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ингредиентов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateTemplate()
        {
            var newTemplate = new DishDto
            {
                DishName = "Новый шаблон блюда",
                Recipe = "Описание рецепта..."
            };

            DishTemplates.Add(newTemplate);
            SelectedTemplate = newTemplate;
            _mainVM.UpdateStatus("Создан новый шаблон блюда");
        }

        private async void SaveTemplate()
        {
            if (SelectedTemplate == null) return;

            try
            {
                using (var context = new DatabaseContext())
                {
                    Dish dish;

                    if (SelectedTemplate.DishId == 0) // Новый шаблон
                    {
                        dish = new Dish
                        {
                            DishName = SelectedTemplate.DishName,
                            TotalCalories = SelectedTemplate.TotalCalories,
                            TotalProtein = SelectedTemplate.TotalProtein,
                            TotalFat = SelectedTemplate.TotalFat,
                            TotalCarbohydrates = SelectedTemplate.TotalCarbohydrates,
                            UserId = _currentUser.UserId // Создатель шаблона
                        };

                        context.Dishes.Add(dish);
                        await context.SaveChangesAsync(); // Сохраняем, чтобы получить DishId

                        SelectedTemplate.DishId = dish.DishId;
                        _mainVM.UpdateStatus($"Создан новый шаблон: {dish.DishName}");
                    }
                    else // Обновление существующего
                    {
                        dish = await context.Dishes.FindAsync(SelectedTemplate.DishId);
                        if (dish != null)
                        {
                            dish.DishName = SelectedTemplate.DishName;
                            dish.TotalCalories = SelectedTemplate.TotalCalories;
                            dish.TotalProtein = SelectedTemplate.TotalProtein;
                            dish.TotalFat = SelectedTemplate.TotalFat;
                            dish.TotalCarbohydrates = SelectedTemplate.TotalCarbohydrates;

                            _mainVM.UpdateStatus($"Шаблон обновлен: {dish.DishName}");
                        }
                    }

                    // Удаляем старые ингредиенты
                    var oldIngredients = context.DishProducts
                        .Where(dp => dp.DishId == SelectedTemplate.DishId);
                    context.DishProducts.RemoveRange(oldIngredients);

                    // Добавляем новые ингредиенты из TemplateIngredients
                    foreach (var ingredient in TemplateIngredients)
                    {
                        var dishProduct = new DishProduct
                        {
                            DishId = SelectedTemplate.DishId,
                            ProductId = ingredient.ProductId,
                            Quantity = ingredient.Quantity
                        };
                        context.DishProducts.Add(dishProduct);
                    }

                    await context.SaveChangesAsync();
                }

                LoadDishTemplates(); // Перезагружаем список
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка сохранения: {ex.Message}");
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteTemplate()
        {
            if (SelectedTemplate == null) return;

            var result = MessageBox.Show($"Удалить шаблон '{SelectedTemplate.DishName}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new DatabaseContext())
                    {
                        var dish = await context.Dishes.FindAsync(SelectedTemplate.DishId);
                        if (dish != null)
                        {
                            // Сначала удаляем связанные ингредиенты
                            var ingredients = context.DishProducts
                                .Where(dp => dp.DishId == SelectedTemplate.DishId);
                            context.DishProducts.RemoveRange(ingredients);

                            // Затем удаляем само блюдо
                            context.Dishes.Remove(dish);
                            await context.SaveChangesAsync();

                            DishTemplates.Remove(SelectedTemplate);
                            _mainVM.UpdateStatus($"Шаблон '{SelectedTemplate.DishName}' удален");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddIngredient()
        {
            if (SelectedTemplate == null || SelectedProduct == null || IngredientQuantity <= 0) return;

            // Проверяем, нет ли уже такого продукта в блюде
            var existingIngredient = TemplateIngredients
                .FirstOrDefault(i => i.ProductId == SelectedProduct.ProductId);

            if (existingIngredient != null)
            {
                // Увеличиваем количество существующего ингредиента
                existingIngredient.Quantity += IngredientQuantity;
            }
            else
            {
                // Добавляем новый ингредиент
                var newIngredient = new ProductDto
                {
                    ProductId = SelectedProduct.ProductId,
                    ProductName = SelectedProduct.ProductName,
                    Category = SelectedProduct.Category,
                    Calories = SelectedProduct.Calories,
                    Protein = SelectedProduct.Protein,
                    Fat = SelectedProduct.Fat,
                    Carbohydrates = SelectedProduct.Carbohydrates,
                    Unit = SelectedProduct.Unit,
                    Quantity = IngredientQuantity
                };

                TemplateIngredients.Add(newIngredient);
            }

            CalculateNutrition(); // Пересчитываем общую питательную ценность
            _mainVM.UpdateStatus($"Добавлен ингредиент: {SelectedProduct.ProductName}");
            OnPropertyChanged(nameof(TemplateIngredients));
        }

        private void RemoveIngredient()
        {
            if (SelectedProduct == null || !TemplateIngredients.Any()) return;

            // Находим ингредиент по выбранному продукту
            var ingredientToRemove = TemplateIngredients
                .FirstOrDefault(i => i.ProductId == SelectedProduct.ProductId);

            if (ingredientToRemove != null)
            {
                TemplateIngredients.Remove(ingredientToRemove);
                CalculateNutrition(); // Пересчитываем общую питательную ценность
                _mainVM.UpdateStatus($"Удален ингредиент: {ingredientToRemove.ProductName}");
                OnPropertyChanged(nameof(TemplateIngredients));
            }
        }

        private void CalculateNutrition()
        {
            if (SelectedTemplate == null) return;

            decimal totalCalories = 0;
            decimal totalProtein = 0;
            decimal totalFat = 0;
            decimal totalCarbs = 0;

            foreach (var ingredient in TemplateIngredients)
            {
                // Расчет с учетом количества
                var multiplier = ingredient.Quantity / 100;
                totalCalories += ingredient.Calories * multiplier;
                totalProtein += ingredient.Protein * multiplier;
                totalFat += ingredient.Fat * multiplier;
                totalCarbs += ingredient.Carbohydrates * multiplier;
            }

            SelectedTemplate.TotalCalories = Math.Round(totalCalories, 1);
            SelectedTemplate.TotalProtein = Math.Round(totalProtein, 1);
            SelectedTemplate.TotalFat = Math.Round(totalFat, 1);
            SelectedTemplate.TotalCarbohydrates = Math.Round(totalCarbs, 1);

            OnPropertyChanged(nameof(SelectedTemplate));
            _mainVM.UpdateStatus("Питательная ценность пересчитана");
        }
    }
}
