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
    public class ProductsViewModel : BaseViewModel
    {
        private readonly DatabaseContext _context;
        private readonly MainViewModel _mainVM;
        private readonly User _currentUser;

        public ObservableCollection<ProductDto> Products { get; set; }
        public ProductDto SelectedProduct { get; set; }

        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public ProductsViewModel(MainViewModel mainVM, User currentUser)
        {
            _context = new DatabaseContext();
            _mainVM = mainVM;
            _currentUser = currentUser;
            Products = new ObservableCollection<ProductDto>();

            AddCommand = new RelayCommand(AddProduct, CanAdd);
            SaveCommand = new RelayCommand(SaveProduct, CanSave);
            DeleteCommand = new RelayCommand(DeleteProduct, CanDelete);
            RefreshCommand = new RelayCommand(RefreshProducts);

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
                _mainVM.UpdateStatus($"Ошибка загрузки продуктов: {ex.Message}");
            }
        }

        private void RefreshProducts() => LoadProducts();

        private bool CanAdd() => _currentUser != null && (_currentUser.IsAdmin() || _currentUser.IsDietitian());
        private bool CanSave() => SelectedProduct != null && CanAdd();
        private bool CanDelete() => SelectedProduct != null && SelectedProduct.ProductId != 0 && CanAdd();

        private void AddProduct()
        {
            if (!CanAdd())
            {
                _mainVM.UpdateStatus("У вас нет прав на добавление продуктов");
                return;
            }

            var p = new ProductDto { ProductName = "Новый продукт" };
            Products.Add(p);
            SelectedProduct = p;
            OnPropertyChanged(nameof(SelectedProduct));
            _mainVM.UpdateStatus("Добавлен новый продукт");
        }

        private async void SaveProduct()
        {
            if (!CanSave())
            {
                _mainVM.UpdateStatus("У вас нет прав на сохранение продуктов");
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
            }
        }

        private async void DeleteProduct()
        {
            if (!CanDelete())
            {
                _mainVM.UpdateStatus("У вас нет прав на удаление продуктов");
                return;
            }

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
            }
        }
    }

}
