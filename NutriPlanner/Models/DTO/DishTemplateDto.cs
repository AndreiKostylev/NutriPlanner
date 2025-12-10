using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    /// <summary>
    /// DTO для шаблона блюда
    /// </summary>
    public class DishTemplateDto
    {
        public int DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recipe { get; set; } = string.Empty;
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalFat { get; set; }
        public decimal TotalCarbohydrates { get; set; }
        public decimal CookingTime { get; set; } // в минутах
        public bool IsVegetarian { get; set; }
        public bool IsVegan { get; set; }
        public bool IsGlutenFree { get; set; }

        // Ингредиенты блюда
        public ObservableCollection<DishIngredientDto> Ingredients { get; set; } = new ObservableCollection<DishIngredientDto>();
    }

   
}
