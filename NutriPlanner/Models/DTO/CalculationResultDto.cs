using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    public class CalculationResultDto
    {
        public decimal BMR { get; set; }
        public decimal TDEE { get; set; }
        public decimal TargetCalories { get; set; }
        public decimal TargetProtein { get; set; }
        public decimal TargetFat { get; set; }
        public decimal TargetCarbs { get; set; }
    }
}
