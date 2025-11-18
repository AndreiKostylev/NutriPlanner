using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models
{
    public class Dish
    {
        public int DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalFat { get; set; }
        public decimal TotalCarbohydrates { get; set; }

     
        public int UserId { get; set; }

       
        public virtual User User { get; set; } = null!;

        public virtual ICollection<FoodDiary> FoodDiaries { get; set; } = new List<FoodDiary>();
        public virtual ICollection<DishProduct> DishProducts { get; set; } = new List<DishProduct>();
    }
}
