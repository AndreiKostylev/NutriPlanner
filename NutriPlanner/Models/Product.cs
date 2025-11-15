using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models
{
    public partial class Product
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public string Category { get; set; } = null!;

        public decimal Calories { get; set; }

        public decimal Protein { get; set; }

        public decimal Fat { get; set; }

        public decimal Carbohydrates { get; set; }

        public string Unit { get; set; } = null!;

        public virtual ICollection<DishProduct> DishProducts { get; set; } = new List<DishProduct>();

        public virtual ICollection<FoodDiary> FoodDiaries { get; set; } = new List<FoodDiary>();
    }
}
