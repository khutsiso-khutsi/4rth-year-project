using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patient.Models
{
    public class PatientCondition
    {
        public int PatientConditionID { get; set; }
        public int ConditionID { get; set; }
        public string ConditionName { get; set; } = "";
        public DateTime? DiagnosedDate { get; set; }
        public string? Notes { get; set; }
        public int PatientID { get; set; }
    }

    public class PatientAllergy
    {
        public int AllergyID { get; set; }
        public string AllergyName { get; set; } = "";
        public string? Severity { get; set; }
        public string? Notes { get; set; }
    }

    public class PatientMedication
    {
        public int MedicationID { get; set; }
        public string MedicationName { get; set; } = "";
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// One entry in a pick-list (a condition, allergy or medication) with
    /// the category it belongs to, so the pickers can group by category.
    /// </summary>
    public class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
    }

    public class MedicalHistoryViewModel
    {
        public List<PatientCondition> Conditions { get; set; } = new();
        public List<PatientAllergy> Allergies { get; set; } = new();
        public List<PatientMedication> Medications { get; set; } = new();
        public List<LookupItem> AllConditions { get; set; } = new();
        public List<LookupItem> AllAllergies { get; set; } = new();
        public List<LookupItem> AllMedications { get; set; } = new();
    }

}