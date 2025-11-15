using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models
{
    public partial class NutritionPlan
    {
        public int PlanId { get; set; }

        public int UserId { get; set; }

        public string PlanName { get; set; } = null!;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal DailyCalories { get; set; }

        public decimal DailyProtein { get; set; }

        public decimal DailyFat { get; set; }

        public decimal DailyCarbohydrates { get; set; }

        public string Status { get; set; } = null!;

        public virtual User User { get; set; } = null!;
    }

}
