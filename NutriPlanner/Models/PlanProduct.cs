using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models
{
    public class PlanProduct
    {
        public int PlanProductId { get; set; }

        public int PlanId { get; set; }
        public int ProductId { get; set; }
        public string MealType { get; set; } = string.Empty; 
        public decimal Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;

        public virtual NutritionPlan NutritionPlan { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
