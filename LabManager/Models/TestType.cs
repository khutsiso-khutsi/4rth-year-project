using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public  class TestType
    {

        public int Id { get; set; }

        public string TestName { get; set; }

        public string Category { get; set; }

        public string SampleType { get; set; }

        public string UnitMeasurement { get; set; }

        public double NormalRangeMin { get; set; }

        public double NormalRangeMax { get; set; }

        public int TurnaroundTime { get; set; }

        public string ConsumablesUsed { get; set; }
        public string NormalRange { get; set; }
        public string Units { get; set; }
        public int TAT { get; set; }
        public int TestId { get; set; }
    }

  
}
