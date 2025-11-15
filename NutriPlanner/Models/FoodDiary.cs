using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models
{
    public partial class FoodDiary
    {
        public int DiaryId { get; set; }

        public int UserId { get; set; }

        public DateTime Date { get; set; }

        public int? ProductId { get; set; }

        public int? DishId { get; set; }

        public decimal Quantity { get; set; }

        public decimal Calories { get; set; }

        public decimal Protein { get; set; }

        public decimal Fat { get; set; }

        public decimal Carbohydrates { get; set; }

        public virtual Dish? Dish { get; set; }

        public virtual Product? Product { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
