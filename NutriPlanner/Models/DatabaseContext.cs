using Microsoft.EntityFrameworkCore;
using NutriPlanner.Models;


namespace NutriPlanner.Data;

/// <summary>
/// Контекст базы данных для приложения планирования питания
/// </summary>
public class DatabaseContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Dish> Dishes { get; set; }
    public DbSet<NutritionPlan> NutritionPlans { get; set; }
    public DbSet<FoodDiary> FoodDiaries { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<DishProduct> DishProducts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=DESKTOP-FRH9KJ6\SQLEXPRESS;Database=NutriPlaner;Trusted_Connection=true;TrustServerCertificate=true;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });


        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.ProductId);
            entity.HasIndex(p => p.ProductName).IsUnique();
            entity.Property(p => p.ProductName).HasMaxLength(200);
        });


        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasKey(d => d.DishId);


            entity.HasOne(d => d.User)
                  .WithMany(u => u.Dishes)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });


        modelBuilder.Entity<NutritionPlan>(entity =>
        {
            entity.HasKey(np => np.PlanId);


            entity.HasOne(np => np.User)
                  .WithMany(u => u.NutritionPlans)
                  .HasForeignKey(np => np.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });


        modelBuilder.Entity<FoodDiary>(entity =>
        {
            entity.HasKey(fd => fd.DiaryId);


            entity.HasOne(fd => fd.User)
                  .WithMany(u => u.FoodDiaries)
                  .HasForeignKey(fd => fd.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(fd => fd.Product)
                  .WithMany(p => p.FoodDiaries)
                  .HasForeignKey(fd => fd.ProductId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(fd => fd.Dish)
                  .WithMany(d => d.FoodDiaries)
                  .HasForeignKey(fd => fd.DishId)
                  .OnDelete(DeleteBehavior.NoAction);
        });


        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.RoleId);

            entity.HasMany(r => r.Users)
                  .WithOne(u => u.Role)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.NoAction);
        });


        modelBuilder.Entity<DishProduct>(entity =>
        {
            entity.HasKey(dp => new { dp.DishId, dp.ProductId });

            entity.HasOne(dp => dp.Dish)
                  .WithMany(d => d.DishProducts)
                  .HasForeignKey(dp => dp.DishId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dp => dp.Product)
                  .WithMany(p => p.DishProducts)
                  .HasForeignKey(dp => dp.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });


        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "User" },
            new Role { RoleId = 2, RoleName = "Dietitian" },
            new Role { RoleId = 3, RoleName = "Admin" }
        );


        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                Username = "admin",
                Email = "admin@nutriplanner.com",
                PasswordHash = "hashed_password",
                Age = 30,
                Gender = "Male",
                Height = 180,
                Weight = 75,
                ActivityLevel = "Moderate",
                Goal = "Maintenance",
                DailyCalorieTarget = 2500,
                DailyProteinTarget = 150,
                DailyFatTarget = 83,
                DailyCarbsTarget = 281,
                RoleId = 3,
                RegistrationDate = DateTime.Now
            },
            new User
            {
                UserId = 2,
                Username = "dietitian",
                Email = "dietitian@nutriplanner.com",
                PasswordHash = "hashed_password",
                Age = 35,
                Gender = "Female",
                Height = 165,
                Weight = 60,
                ActivityLevel = "Moderate",
                Goal = "Maintenance",
                DailyCalorieTarget = 2200,
                DailyProteinTarget = 130,
                DailyFatTarget = 73,
                DailyCarbsTarget = 247,
                RoleId = 2,
                RegistrationDate = DateTime.Now
            }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product { ProductId = 1, ProductName = "Куриная грудка", Category = "Мясо", Calories = 165, Protein = 31, Fat = 3.6m, Carbohydrates = 0, Unit = "г" },
            new Product { ProductId = 2, ProductName = "Говядина", Category = "Мясо", Calories = 250, Protein = 26, Fat = 15, Carbohydrates = 0, Unit = "г" },
            new Product { ProductId = 3, ProductName = "Рис вареный", Category = "Крупы", Calories = 130, Protein = 2.7m, Fat = 0.3m, Carbohydrates = 28, Unit = "г" },
            new Product { ProductId = 4, ProductName = "Гречка", Category = "Крупы", Calories = 132, Protein = 4.5m, Fat = 2.3m, Carbohydrates = 25, Unit = "г" },
            new Product { ProductId = 5, ProductName = "Яблоко", Category = "Фрукты", Calories = 52, Protein = 0.3m, Fat = 0.2m, Carbohydrates = 14, Unit = "г" },
            new Product { ProductId = 6, ProductName = "Свинина", Category = "Мясо", Calories = 242, Protein = 25, Fat = 16, Carbohydrates = 0, Unit = "г" },
            new Product { ProductId = 7, ProductName = "Индейка", Category = "Мясо", Calories = 135, Protein = 29, Fat = 1, Carbohydrates = 0, Unit = "г" },
            new Product { ProductId = 8, ProductName = "Бекон", Category = "Мясо", Calories = 541, Protein = 37, Fat = 42, Carbohydrates = 1.4m, Unit = "г" },
            new Product { ProductId = 9, ProductName = "Лосось", Category = "Рыба", Calories = 208, Protein = 20, Fat = 13, Carbohydrates = 0, Unit = "г" },
            new Product { ProductId = 10, ProductName = "Тунец", Category = "Рыба", Calories = 184, Protein = 30, Fat = 6, Carbohydrates = 0, Unit = "г" },
            new Product { ProductId = 11, ProductName = "Треска", Category = "Рыба", Calories = 82, Protein = 18, Fat = 0.7m, Carbohydrates = 0, Unit = "г" },
            new Product { ProductId = 12, ProductName = "Креветки", Category = "Морепродукты", Calories = 85, Protein = 20, Fat = 0.5m, Carbohydrates = 0, Unit = "г" },
            new Product { ProductId = 13, ProductName = "Яйцо куриное", Category = "Яйца", Calories = 155, Protein = 13, Fat = 11, Carbohydrates = 1.1m, Unit = "шт" },
            new Product { ProductId = 14, ProductName = "Творог 5%", Category = "Молочные", Calories = 121, Protein = 17, Fat = 5, Carbohydrates = 1.8m, Unit = "г" },
            new Product { ProductId = 15, ProductName = "Сыр Чеддер", Category = "Молочные", Calories = 404, Protein = 23, Fat = 34, Carbohydrates = 1.3m, Unit = "г" },
            new Product { ProductId = 16, ProductName = "Молоко 2.5%", Category = "Молочные", Calories = 52, Protein = 2.9m, Fat = 2.5m, Carbohydrates = 4.7m, Unit = "мл" },
            new Product { ProductId = 17, ProductName = "Йогурт греческий", Category = "Молочные", Calories = 59, Protein = 10, Fat = 0.4m, Carbohydrates = 3.6m, Unit = "г" },
            new Product { ProductId = 18, ProductName = "Овсянка", Category = "Крупы", Calories = 68, Protein = 2.4m, Fat = 1.4m, Carbohydrates = 12, Unit = "г" },
            new Product { ProductId = 19, ProductName = "Макароны вареные", Category = "Крупы", Calories = 158, Protein = 5.8m, Fat = 0.9m, Carbohydrates = 31, Unit = "г" },
            new Product { ProductId = 20, ProductName = "Хлеб цельнозерновой", Category = "Хлеб", Calories = 247, Protein = 13, Fat = 3.4m, Carbohydrates = 41, Unit = "г" },
            new Product { ProductId = 21, ProductName = "Картофель", Category = "Овощи", Calories = 77, Protein = 2, Fat = 0.1m, Carbohydrates = 17, Unit = "г" },
            new Product { ProductId = 22, ProductName = "Морковь", Category = "Овощи", Calories = 41, Protein = 0.9m, Fat = 0.2m, Carbohydrates = 10, Unit = "г" },
            new Product { ProductId = 23, ProductName = "Помидор", Category = "Овощи", Calories = 18, Protein = 0.9m, Fat = 0.2m, Carbohydrates = 3.9m, Unit = "г" },
            new Product { ProductId = 24, ProductName = "Огурец", Category = "Овощи", Calories = 15, Protein = 0.7m, Fat = 0.1m, Carbohydrates = 3.6m, Unit = "г" },
            new Product { ProductId = 25, ProductName = "Брокколи", Category = "Овощи", Calories = 34, Protein = 2.8m, Fat = 0.4m, Carbohydrates = 7, Unit = "г" },
            new Product { ProductId = 26, ProductName = "Шпинат", Category = "Овощи", Calories = 23, Protein = 2.9m, Fat = 0.4m, Carbohydrates = 3.6m, Unit = "г" },
            new Product { ProductId = 27, ProductName = "Банан", Category = "Фрукты", Calories = 89, Protein = 1.1m, Fat = 0.3m, Carbohydrates = 23, Unit = "г" },
            new Product { ProductId = 28, ProductName = "Апельсин", Category = "Фрукты", Calories = 47, Protein = 0.9m, Fat = 0.1m, Carbohydrates = 12, Unit = "г" },
            new Product { ProductId = 29, ProductName = "Клубника", Category = "Фрукты", Calories = 32, Protein = 0.7m, Fat = 0.3m, Carbohydrates = 7.7m, Unit = "г" },
            new Product { ProductId = 30, ProductName = "Авокадо", Category = "Фрукты", Calories = 160, Protein = 2, Fat = 15, Carbohydrates = 9, Unit = "г" },
            new Product { ProductId = 31, ProductName = "Миндаль", Category = "Орехи", Calories = 579, Protein = 21, Fat = 50, Carbohydrates = 22, Unit = "г" },
            new Product { ProductId = 32, ProductName = "Грецкие орехи", Category = "Орехи", Calories = 654, Protein = 15, Fat = 65, Carbohydrates = 14, Unit = "г" },
            new Product { ProductId = 33, ProductName = "Арахис", Category = "Орехи", Calories = 567, Protein = 26, Fat = 49, Carbohydrates = 16, Unit = "г" },
            new Product { ProductId = 34, ProductName = "Оливковое масло", Category = "Масла", Calories = 884, Protein = 0, Fat = 100, Carbohydrates = 0, Unit = "мл" },
            new Product { ProductId = 35, ProductName = "Сливочное масло", Category = "Масла", Calories = 717, Protein = 0.9m, Fat = 81, Carbohydrates = 0.1m, Unit = "г" },
            new Product { ProductId = 36, ProductName = "Чечевица вареная", Category = "Бобовые", Calories = 116, Protein = 9, Fat = 0.4m, Carbohydrates = 20, Unit = "г" },
            new Product { ProductId = 37, ProductName = "Фасоль", Category = "Бобовые", Calories = 127, Protein = 9, Fat = 0.5m, Carbohydrates = 23, Unit = "г" },
            new Product { ProductId = 38, ProductName = "Вода", Category = "Напитки", Calories = 0, Protein = 0, Fat = 0, Carbohydrates = 0, Unit = "мл" },
            new Product { ProductId = 39, ProductName = "Чай черный", Category = "Напитки", Calories = 1, Protein = 0, Fat = 0, Carbohydrates = 0.3m, Unit = "мл" },
            new Product { ProductId = 40, ProductName = "Кофе черный", Category = "Напитки", Calories = 1, Protein = 0.1m, Fat = 0, Carbohydrates = 0, Unit = "мл" }
        );

    }
}
