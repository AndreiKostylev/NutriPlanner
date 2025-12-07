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
    public class ProductsViewModel : BaseViewModel
    {
        private readonly DatabaseContext _context;
        private readonly MainViewModel _mainVM;
        private readonly User _currentUser;

        public ObservableCollection<ProductDto> Products { get; set; }
        public ProductDto SelectedProduct { get; set; }

        // Права доступа
        public bool CanEditProducts => _currentUser?.Role?.RoleName == "Dietitian" ||
                                      _currentUser?.Role?.RoleName == "Admin";

        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand LoadCommand { get; }

        public ProductsViewModel(MainViewModel mainVM, User currentUser)
        {
            _context = new DatabaseContext();
            _mainVM = mainVM;
            _currentUser = currentUser;

            Products = new ObservableCollection<ProductDto>();

            AddCommand = new RelayCommand(AddProduct, () => CanEditProducts);
            SaveCommand = new RelayCommand(SaveProduct, () => CanEditProducts && SelectedProduct != null);
            DeleteCommand = new RelayCommand(DeleteProduct, () => CanEditProducts && SelectedProduct != null && SelectedProduct.ProductId != 0);
            LoadCommand = new RelayCommand(LoadProducts);

            LoadProducts();
        }

        private async void LoadProducts()
        {
            try
            {
                Products.Clear();
                var items = await _context.Products.ToListAsync();

                foreach (var p in items)
                    Products.Add(new ProductDto
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Category = p.Category,
                        Calories = p.Calories,
                        Protein = p.Protein,
                        Fat = p.Fat,
                        Carbohydrates = p.Carbohydrates,
                        Unit = p.Unit
                    });

                _mainVM.UpdateStatus($"Загружено {Products.Count} продуктов");
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void AddProduct()
        {
            if (!CanEditProducts)
            {
                MessageBox.Show("Недостаточно прав для добавления продуктов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var p = new ProductDto { ProductName = "Новый продукт", Unit = "г" };
            Products.Add(p);
            SelectedProduct = p;
            OnPropertyChanged(nameof(SelectedProduct));
        }

        private async void SaveProduct()
        {
            if (!CanEditProducts)
            {
                MessageBox.Show("Недостаточно прав для сохранения продуктов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Product entity;

                if (SelectedProduct.ProductId == 0)
                {
                    entity = new Product();
                    _context.Products.Add(entity);
                }
                else
                {
                    entity = await _context.Products.FindAsync(SelectedProduct.ProductId);
                }

                entity.ProductName = SelectedProduct.ProductName;
                entity.Category = SelectedProduct.Category;
                entity.Calories = SelectedProduct.Calories;
                entity.Protein = SelectedProduct.Protein;
                entity.Fat = SelectedProduct.Fat;
                entity.Carbohydrates = SelectedProduct.Carbohydrates;
                entity.Unit = SelectedProduct.Unit;

                await _context.SaveChangesAsync();
                _mainVM.UpdateStatus("Продукт сохранён");
                LoadProducts();
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка сохранения: {ex.Message}");
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteProduct()
        {
            if (!CanEditProducts)
            {
                MessageBox.Show("Недостаточно прав для удаления продуктов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить продукт '{SelectedProduct.ProductName}'?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var entity = await _context.Products.FindAsync(SelectedProduct.ProductId);

                if (entity != null)
                {
                    _context.Products.Remove(entity);
                    await _context.SaveChangesAsync();
                    Products.Remove(SelectedProduct);
                    _mainVM.UpdateStatus("Продукт удалён");
                }
            }
            catch (Exception ex)
            {
                _mainVM.UpdateStatus($"Ошибка удаления: {ex.Message}");
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
