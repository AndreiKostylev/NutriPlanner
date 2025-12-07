using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    public class ClientAlertDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime AlertDate { get; set; }
        public string Priority { get; set; } = "Medium"; // High, Medium, Low
    }
}
