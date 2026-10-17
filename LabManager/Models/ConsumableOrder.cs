using Microsoft.AspNetCore.Mvc.Rendering;
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
        public int  SupplierID { get; set; }

        public int ConsumableID { get; set; }
        public string Items { get; set; }

        public int QuantityOrdered { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string CancellationReason { get; set; }
    }
}
