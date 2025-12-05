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
        optionsBuilder.UseSqlServer(@"Server=DESKTOP-FRH9KJ6\SQLEXPRESS;Database=NutriPlanner;Trusted_Connection=true;TrustServerCertificate=true;");
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
            new Product { ProductId = 5, ProductName = "Яблоко", Category = "Фрукты", Calories = 52, Protein = 0.3m, Fat = 0.2m, Carbohydrates = 14, Unit = "г" }
        );
    }
}