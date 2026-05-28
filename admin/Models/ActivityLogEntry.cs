using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace admin.Models
{
    public class ActivityLogEntry
    {
        public int LogID { get; set; }
        public string? Action { get; set; }
        public string? PerformedBy { get; set; }
        public DateTime LogDate { get; set; }
        public DateTime Timestamp { get; set; }
    }
}