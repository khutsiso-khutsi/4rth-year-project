using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public class StaffViewModel
    {
       
        
            public IEnumerable<Doctor> Doctors { get; set; }
                = new List<Doctor>();

            public IEnumerable<Technician> Technicians { get; set; }
                = new List<Technician>();

            public IEnumerable<TestType> TestTypes { get; set; }
                = new List<TestType>();
        
    }
}
