using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    public class NutritionPlanDto
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DailyCalories { get; set; }
        public decimal DailyProtein { get; set; }
        public decimal DailyFat { get; set; }
        public decimal DailyCarbohydrates { get; set; }
        public string Status { get; set; } = string.Empty; 
        public string CreatedBy { get; set; } =  "Диетолог"; 

        
        public bool IsActiveToday(DateTime date)
        {
            return Status == "Активен" && date >= StartDate && date <= EndDate;
        }

        public int DaysRemaining(DateTime currentDate)
        {
            return EndDate >= currentDate ? (EndDate.Date - currentDate.Date).Days + 1 : 0;
        }
    }
}
