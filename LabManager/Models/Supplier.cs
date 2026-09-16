using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public class Supplier
    {
        
        
            public int SupplierID { get; set; }

            public string SupplierName { get; set; }

            public string ContactPerson { get; set; }

            public string? EmailAddress { get; set; }

            public bool IsActive { get; set; }
        
    }
}
