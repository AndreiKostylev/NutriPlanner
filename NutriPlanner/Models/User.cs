using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models
{
    public partial class User
    {
        public int UserId { get; set; }

        public int RoleId { get; set; }

        public string Username { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public int Age { get; set; }

        public string Gender { get; set; } = null!;

        public decimal Height { get; set; }

        public decimal Weight { get; set; }

        public string ActivityLevel { get; set; } = null!;

        public string Goal { get; set; } = null!;

        public decimal DailyCalorieTarget { get; set; }

        public decimal DailyProteinTarget { get; set; }

        public decimal DailyFatTarget { get; set; }

        public decimal DailyCarbsTarget { get; set; }

        public DateTime RegistrationDate { get; set; }

        public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();

        public virtual ICollection<FoodDiary> FoodDiaries { get; set; } = new List<FoodDiary>();

        public virtual ICollection<NutritionPlan> NutritionPlans { get; set; } = new List<NutritionPlan>();

        public virtual Role Role { get; set; } = null!;
    }
}
