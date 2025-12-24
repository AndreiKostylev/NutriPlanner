using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    public class PlanProductDto
    {
        public int PlanProductId { get; set; }
        public int PlanId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string MealType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "г";
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbohydrates { get; set; }
        public string Notes { get; set; } = string.Empty;

        // Расчетные поля
        public decimal TotalCalories => Calories * (Quantity / 100);
        public decimal TotalProtein => Protein * (Quantity / 100);
        public decimal TotalFat => Fat * (Quantity / 100);
        public decimal TotalCarbs => Carbohydrates * (Quantity / 100);
    }
}
