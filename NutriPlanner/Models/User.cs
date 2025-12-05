using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
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
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;

        public virtual ICollection<NutritionPlan> NutritionPlans { get; set; } = new List<NutritionPlan>();
        public virtual ICollection<FoodDiary> FoodDiaries { get; set; } = new List<FoodDiary>();
        public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();

        // Безопасные методы для проверки ролей
        public bool IsAdmin()
        {
            if (Role == null) return false;
            return Role.RoleName == "Admin" || Role.RoleName == "Администратор";
        }

        public bool IsDietitian()
        {
            if (Role == null) return false;
            return Role.RoleName == "Dietitian" || Role.RoleName == "Диетолог";
        }

        public bool IsUser()
        {
            if (Role == null) return false;
            return Role.RoleName == "User" || Role.RoleName == "Пользователь";
        }

        public bool IsDietitianOrAdmin()
        {
            return IsDietitian() || IsAdmin();
        }

        // Метод для получения названия роли безопасно
        public string GetRoleName()
        {
            return Role?.RoleName ?? "Неизвестно";
        }
    }
}
