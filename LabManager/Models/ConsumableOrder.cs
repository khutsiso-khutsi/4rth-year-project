using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public class ConsumableOrder
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; }

        public string Supplier { get; set; }

        public string Items { get; set; }

        public DateTime OrderDate { get; set; }

        public string Status { get; set; }

        public DateTime? CompletedDate { get; set; }
    }
}
