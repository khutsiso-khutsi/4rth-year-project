using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace admin.Models
{
    public class ConditionCategory
    {
        public int ConditionCategoryID { get; set; }
        public string CategoryName { get; set; }
        public string? Description { get; set; }
    }

    public class MedicalCondition
    {
        public int ConditionID { get; set; }
        public string ConditionName { get; set; }
        public string? Description { get; set; }
        public int ConditionCategoryID { get; set; }
        public string? CategoryName { get; set; }
    }

    public class AllergyCategory
    {
        public int AllergyCategoryID { get; set; }
        public string CategoryName { get; set; }
        public string? Description { get; set; }
    }

    public class Allergy
    {
        public int AllergyID { get; set; }
        public string AllergyName { get; set; }
        public string? Description { get; set; }
        public int AllergyCategoryID { get; set; }
        public string? CategoryName { get; set; }
    }

    public class MedicationCategory
    {
        public int MedicationCategoryID { get; set; }
        public string CategoryName { get; set; }
        public string? Description { get; set; }
    }

    public class Medication
    {
        public int MedicationID { get; set; }
        public string MedicationName { get; set; }
        public string? Description { get; set; }
        public int MedicationCategoryID { get; set; }
        public string? CategoryName { get; set; }
    }

    public class ActivityLog
    {
        public int LogID { get; set; }
        public string Action { get; set; }
        public string PerformedBy { get; set; }
        public DateTime LogDate { get; set; }
    }
}