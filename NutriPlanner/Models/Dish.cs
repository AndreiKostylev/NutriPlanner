using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models
{
    public partial class Dish
    {
        public int DishId { get; set; }

        public string DishName { get; set; } = null!;

        public int CreatedBy { get; set; }

        public decimal TotalCalories { get; set; }

        public decimal TotalProtein { get; set; }

        public decimal TotalFat { get; set; }

        public decimal TotalCarbohydrates { get; set; }

        public virtual User CreatedByNavigation { get; set; } = null!;

        public virtual ICollection<DishProduct> DishProducts { get; set; } = new List<DishProduct>();

        public virtual ICollection<FoodDiary> FoodDiaries { get; set; } = new List<FoodDiary>();
    }
}
