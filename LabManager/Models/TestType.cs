using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public class TestType
    {
        public int TestId { get; set; }

        public string? TestName { get; set; }

        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public int SampleTypeId { get; set; }

        public string? SampleName { get; set; }
        public string? UnitMeasurement { get; set; }

        public decimal? NormalRangeMin { get; set; }

        public decimal? NormalRangeMax { get; set; }

        public int? TurnaroundTime { get; set; }

        public string? ConsumablesUsed { get; set; }

        public bool IsActive { get; set; }

        public string NormalRangeDisplay
        {
            get
            {
                if (NormalRangeMin.HasValue && NormalRangeMax.HasValue)
                {
                    return $"{NormalRangeMin:0.##} - {NormalRangeMax:0.##}";
                }

                if (NormalRangeMin.HasValue)
                {
                    return $"{NormalRangeMin:0.##} -";
                }

                if (NormalRangeMax.HasValue)
                {
                    return $"- {NormalRangeMax:0.##}";
                }

                return "—";
            }
        }
    }


}
