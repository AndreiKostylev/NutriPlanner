using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public string ActivityLevel { get; set; } = string.Empty;
        public string Goal { get; set; } = string.Empty;
        public decimal DailyCalorieTarget { get; set; }
        public decimal DailyProteinTarget { get; set; }
        public decimal DailyFatTarget { get; set; }
        public decimal DailyCarbsTarget { get; set; }
    }
}
