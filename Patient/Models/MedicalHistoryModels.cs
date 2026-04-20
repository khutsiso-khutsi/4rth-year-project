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

    public class MedicalHistoryViewModel
    {
        public List<PatientCondition> Conditions { get; set; } = new();
        public List<PatientAllergy> Allergies { get; set; } = new();
        public List<PatientMedication> Medications { get; set; } = new();
        public List<(int Id, string Name)> AllConditions { get; set; } = new();
        public List<(int Id, string Name)> AllAllergies { get; set; } = new();
        public List<(int Id, string Name)> AllMedications { get; set; } = new();
    }

}
