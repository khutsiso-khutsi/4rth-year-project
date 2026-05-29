using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
   public class StaffViewModel
    {

        public List<Doctor> Doctors { get; set; } = new();

        public List<Technician> Technicians { get; set; } = new();
    }
}
