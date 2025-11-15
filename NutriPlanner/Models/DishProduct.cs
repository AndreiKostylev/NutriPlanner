using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models
{
    public partial class DishProduct
    {
        public int DishId { get; set; }

        public int ProductId { get; set; }

        public decimal Quantity { get; set; }

        public virtual Dish Dish { get; set; } = null!;

        public virtual Product Product { get; set; } = null!;
    }

}
