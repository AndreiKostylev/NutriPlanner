using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    public class MealDto
    {
        public string MealName { get; set; } = string.Empty;
        public string MealType { get; set; } = string.Empty;
        public DateTime MealTime { get; set; }
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbs { get; set; }
        public decimal Progress { get; set; }
    }
}
