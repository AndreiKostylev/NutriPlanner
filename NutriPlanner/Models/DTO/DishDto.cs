using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    public class DishDto
    {
        public int DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalFat { get; set; }
        public decimal TotalCarbohydrates { get; set; }
        public string Recipe { get; set; } = string.Empty;

        public ObservableCollection<ProductDto> Ingredients { get; set; } = new ObservableCollection<ProductDto>();
    }
}
