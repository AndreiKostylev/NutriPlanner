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

        public ObservableCollection<ProductDto> Products { get; set; }
        public ProductDto SelectedProduct { get; set; }

        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public ProductsViewModel(MainViewModel mainVM)
        {
            _context = new DatabaseContext();
            _mainVM = mainVM;
            Products = new ObservableCollection<ProductDto>();

            AddCommand = new RelayCommand(AddProduct);
            SaveCommand = new RelayCommand(SaveProduct, CanSave);
            DeleteCommand = new RelayCommand(DeleteProduct, CanDelete);

            LoadProducts();
        }

        private async void LoadProducts()
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
        }

        private void AddProduct()
        {
            var p = new ProductDto { ProductName = "Новый продукт" };
            Products.Add(p);
            SelectedProduct = p;
            OnPropertyChanged(nameof(SelectedProduct));
        }

        private bool CanSave() => SelectedProduct != null;

        private async void SaveProduct()
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

        private bool CanDelete() => SelectedProduct != null && SelectedProduct.ProductId != 0;

        private async void DeleteProduct()
        {
            var entity = await _context.Products.FindAsync(SelectedProduct.ProductId);

            if (entity != null)
            {
                _context.Products.Remove(entity);
                await _context.SaveChangesAsync();
                Products.Remove(SelectedProduct);
                _mainVM.UpdateStatus("Удалено");
            }
        }
    }

}
