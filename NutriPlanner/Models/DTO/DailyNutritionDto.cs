using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    public class DailyNutritionDto
    {
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalFat { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TargetCalories { get; set; }
        public decimal TargetProtein { get; set; }
        public decimal TargetFat { get; set; }
        public decimal TargetCarbs { get; set; }
        public decimal CaloriesProgress { get; set; }
        public decimal ProteinProgress { get; set; }
        public decimal FatProgress { get; set; }
        public decimal CarbsProgress { get; set; }

        public ObservableCollection<MealDto> Meals { get; set; } = new ObservableCollection<MealDto>();
    }
}
