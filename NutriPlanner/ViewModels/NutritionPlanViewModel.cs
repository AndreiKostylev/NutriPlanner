using NutriPlanner.Data;
using NutriPlanner.Models;
using NutriPlanner.Models.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NutriPlanner.ViewModels
{
    public class NutritionPlanViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;

        private Product _selectedProduct;
        private NutritionPlanItemDto _selectedPlanItem;

        private decimal _selectedWeight = 100;

        
        private decimal _totalCalories;
        private decimal _totalProteins;
        private decimal _totalFats;
        private decimal _totalCarbs;

        
        private decimal _dailyCaloriesNorm;
        private decimal _dailyProteinsNorm;
        private decimal _dailyFatsNorm;
        private decimal _dailyCarbsNorm;

        private string _goal = "Поддержание";

        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<NutritionPlanItemDto> PlanItems { get; set; }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public NutritionPlanItemDto SelectedPlanItem
        {
            get => _selectedPlanItem;
            set { _selectedPlanItem = value; OnPropertyChanged(); }
        }

        public decimal SelectedWeight
        {
            get => _selectedWeight;
            set { _selectedWeight = value; OnPropertyChanged(); }
        }

     
        public decimal TotalCalories
        {
            get => _totalCalories;
            set { _totalCalories = value; OnPropertyChanged(); }
        }

        public decimal TotalProteins
        {
            get => _totalProteins;
            set { _totalProteins = value; OnPropertyChanged(); }
        }

        public decimal TotalFats
        {
            get => _totalFats;
            set { _totalFats = value; OnPropertyChanged(); }
        }

        public decimal TotalCarbs
        {
            get => _totalCarbs;
            set { _totalCarbs = value; OnPropertyChanged(); }
        }

        public decimal DailyCaloriesNorm
        {
            get => _dailyCaloriesNorm;
            set { _dailyCaloriesNorm = value; OnPropertyChanged(); }
        }

        public decimal DailyProteinsNorm
        {
            get => _dailyProteinsNorm;
            set { _dailyProteinsNorm = value; OnPropertyChanged(); }
        }

        public decimal DailyFatsNorm
        {
            get => _dailyFatsNorm;
            set { _dailyFatsNorm = value; OnPropertyChanged(); }
        }

        public decimal DailyCarbsNorm
        {
            get => _dailyCarbsNorm;
            set { _dailyCarbsNorm = value; OnPropertyChanged(); }
        }

        public string Goal
        {
            get => _goal;
            set { _goal = value; OnPropertyChanged(); RecalculateNorms(); }
        }

       
        public ICommand AddProductCommand { get; }
        public ICommand RemoveProductCommand { get; }

        public NutritionPlanViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            Products = new ObservableCollection<Product>();
            PlanItems = new ObservableCollection<NutritionPlanItemDto>();

            AddProductCommand = new RelayCommand(AddProduct);                        
            RemoveProductCommand = new RelayCommand(RemoveProduct);

            LoadProducts();
            RecalculateNorms();
        }

        /// <summary>
        /// Загружаем продукты из MSSQL
        /// </summary>
        private void LoadProducts()
        {
            using var db = new DatabaseContext();
            var all = db.Products.ToList();

            Products.Clear();
            foreach (var p in all)
                Products.Add(p);

            _mainVM.UpdateStatus($"Загружено {Products.Count} продуктов");
        }

        /// <summary>
        /// Добавление продукта в план
        /// </summary>
        private void AddProduct()
        {
            if (SelectedProduct == null || SelectedWeight <= 0)
                return;

            var item = new NutritionPlanItemDto
            {
                ProductName = SelectedProduct.ProductName,
                Weight = SelectedWeight,

                Calories = SelectedProduct.Calories * SelectedWeight / 100,
                Proteins = SelectedProduct.Protein * SelectedWeight / 100,
                Fats = SelectedProduct.Fat * SelectedWeight / 100,
                Carbs = SelectedProduct.Carbohydrates * SelectedWeight / 100
            };

            PlanItems.Add(item);
            UpdateTotals();

            _mainVM.UpdateStatus($"Добавлено: {SelectedProduct.ProductName} ({SelectedWeight} г)");
        }

        private void RemoveProduct()
        {
            if (SelectedPlanItem == null)
                return;

            PlanItems.Remove(SelectedPlanItem);
            UpdateTotals();

            _mainVM.UpdateStatus("Продукт удалён");
        }

        private void UpdateTotals()
        {
            TotalCalories = PlanItems.Sum(i => i.Calories);
            TotalProteins = PlanItems.Sum(i => i.Proteins);
            TotalFats = PlanItems.Sum(i => i.Fats);
            TotalCarbs = PlanItems.Sum(i => i.Carbs);
        }

        /// <summary>
        /// Автоматический расчёт норм (упрощённый вариант)
        /// </summary>
        private void RecalculateNorms()
        {
            DailyCaloriesNorm = Goal switch
            {
                "Похудение" => 1800,
                "Набор массы" => 2700,
                _ => 2200,
            };

            DailyProteinsNorm = DailyCaloriesNorm * 0.30m / 4;
            DailyFatsNorm = DailyCaloriesNorm * 0.25m / 9;
            DailyCarbsNorm = DailyCaloriesNorm * 0.45m / 4;

            _mainVM.UpdateStatus("Нормы обновлены");
        }
    }
}
