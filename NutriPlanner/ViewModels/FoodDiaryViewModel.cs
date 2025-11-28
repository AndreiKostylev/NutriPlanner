using Microsoft.EntityFrameworkCore;
using NutriPlanner.Data;
using NutriPlanner.Models.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NutriPlanner.ViewModels
{
    public class FoodDiaryViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private readonly DatabaseContext _context;

        public ObservableCollection<DailyNutritionDto> DiaryEntries { get; set; }
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public ICommand LoadCommand { get; }

        public FoodDiaryViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _context = new DatabaseContext();

            DiaryEntries = new ObservableCollection<DailyNutritionDto>();
            LoadCommand = new RelayCommand(LoadDiary);

            LoadDiary();
        }

        private void LoadDiary()
        {
            DiaryEntries.Clear();

            // Получаем все записи за день
            var entries = _context.FoodDiaries
                .Where(fd => fd.Date.Date == SelectedDate.Date && fd.UserId == 1) 
                .Include(fd => fd.Product)
                .Include(fd => fd.Dish)
                .ToList();

            if (!entries.Any())
            {
                _mainVM.UpdateStatus($"Нет записей за {SelectedDate:dd.MM.yyyy}");
                return;
            }

            var dailyDto = new DailyNutritionDto
            {
                Meals = new ObservableCollection<MealDto>()
            };

            foreach (var e in entries)
            {
                var mealName = e.Product?.ProductName ?? e.Dish?.DishName ?? "Неизвестно";

                var meal = new MealDto
                {
                    MealName = mealName,    
                    MealTime = e.Date,
                    Calories = e.Calories,
                    Protein = e.Protein,
                    Fat = e.Fat,
                    Carbs = e.Carbohydrates
                };

                dailyDto.Meals.Add(meal);

              
                dailyDto.TotalCalories += e.Calories;
                dailyDto.TotalProtein += e.Protein;
                dailyDto.TotalFat += e.Fat;
                dailyDto.TotalCarbs += e.Carbohydrates;
            }

            

            DiaryEntries.Add(dailyDto);

            _mainVM.UpdateStatus($"Загружено {entries.Count} записей за {SelectedDate:dd.MM.yyyy}");
        }
    }
}
