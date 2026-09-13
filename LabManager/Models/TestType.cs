using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public  class TestType
    {

        public int TestId { get; set; }

        [Required]
        [StringLength(150)]
        public string TestName { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        // Populated by the join in sp_GetAllTestTypes / sp_GetTestTypeById.
        // Not sent back on Add/Edit posts — CategoryId is the source of truth.
        public string? CategoryName { get; set; }

        [Required]
        [StringLength(50)]
        public string SampleTypeId { get; set; } = string.Empty;


        public string ? SampleName { get; set; }    
        [StringLength(30)]
        public string? UnitMeasurement { get; set; }

        public decimal? NormalRangeMin { get; set; }

        public decimal? NormalRangeMax { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Turnaround time cannot be negative.")]
        public int? TurnaroundTime { get; set; }

        [StringLength(500)]
        public string? ConsumablesUsed { get; set; }

        // Convenience for display; not persisted directly.
        public string NormalRangeDisplay =>
            (NormalRangeMin.HasValue && NormalRangeMax.HasValue)
                ? $"{NormalRangeMin:0.##} - {NormalRangeMax:0.##}"
                : "N/A";
    }

  
}
