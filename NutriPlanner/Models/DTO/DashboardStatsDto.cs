using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriPlanner.Models.DTO
{
    public class DashboardStatsDto
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int ClientsNeedingAttention { get; set; }
        public int PlansCreatedThisWeek { get; set; }
        public int MessagesUnread { get; set; }
    }
}
