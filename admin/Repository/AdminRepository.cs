using admin.DataAccess;
using admin.Models;

namespace admin.Repository
{
    public class AdminRepository
    {
        private readonly AdminDataAccess _dataAccess;

        public AdminRepository(string connectionString)
        {
            _dataAccess = new AdminDataAccess(connectionString);
        }

        // ── DASHBOARD ─────────────────────────────────────────────────
        public (int conditions, int allergies, int medications, int conditionCats, int allergyCats, int medicationCats) GetDashboardCounts()
            => _dataAccess.GetDashboardCounts();

        // ── CONDITION CATEGORIES ──────────────────────────────────────
        public List<ConditionCategory> GetAllConditionCategories(bool includeInactive = false) => _dataAccess.GetAllConditionCategories(includeInactive);
        public ConditionCategory? GetConditionCategoryById(int id) => _dataAccess.GetConditionCategoryById(id);
        public string CreateConditionCategory(string name, string? desc) => _dataAccess.CreateConditionCategory(name, desc);
        public string UpdateConditionCategory(int id, string name, string? desc) => _dataAccess.UpdateConditionCategory(id, name, desc);
        public string DeleteConditionCategory(int id) => _dataAccess.DeleteConditionCategory(id);
        public string RestoreConditionCategory(int id) => _dataAccess.RestoreConditionCategory(id);

        // ── MEDICAL CONDITIONS ────────────────────────────────────────
        public List<MedicalCondition> GetAllConditions(bool includeInactive = false) => _dataAccess.GetAllConditions(includeInactive);
        public MedicalCondition? GetConditionById(int id) => _dataAccess.GetConditionById(id);
        public string CreateCondition(string name, string? desc, int catId) => _dataAccess.CreateCondition(name, desc, catId);
        public string UpdateCondition(int id, string name, string? desc, int catId) => _dataAccess.UpdateCondition(id, name, desc, catId);
        public string DeleteCondition(int id) => _dataAccess.DeleteCondition(id);
        public string RestoreCondition(int id) => _dataAccess.RestoreCondition(id);

        // ── ALLERGY CATEGORIES ────────────────────────────────────────
        public List<AllergyCategory> GetAllAllergyCategories(bool includeInactive = false) => _dataAccess.GetAllAllergyCategories(includeInactive);
        public AllergyCategory? GetAllergyCategoryById(int id) => _dataAccess.GetAllergyCategoryById(id);
        public string CreateAllergyCategory(string name, string? desc) => _dataAccess.CreateAllergyCategory(name, desc);
        public string UpdateAllergyCategory(int id, string name, string? desc) => _dataAccess.UpdateAllergyCategory(id, name, desc);
        public string DeleteAllergyCategory(int id) => _dataAccess.DeleteAllergyCategory(id);
        public string RestoreAllergyCategory(int id) => _dataAccess.RestoreAllergyCategory(id);

        // ── ALLERGIES ─────────────────────────────────────────────────
        public List<Allergy> GetAllAllergies(bool includeInactive = false) => _dataAccess.GetAllAllergies(includeInactive);
        public Allergy? GetAllergyById(int id) => _dataAccess.GetAllergyById(id);
        public string CreateAllergy(string name, string? desc, int catId) => _dataAccess.CreateAllergy(name, desc, catId);
        public string UpdateAllergy(int id, string name, string? desc, int catId) => _dataAccess.UpdateAllergy(id, name, desc, catId);
        public string DeleteAllergy(int id) => _dataAccess.DeleteAllergy(id);
        public string RestoreAllergy(int id) => _dataAccess.RestoreAllergy(id);

        // ── MEDICATION CATEGORIES ─────────────────────────────────────
        public List<MedicationCategory> GetAllMedicationCategories(bool includeInactive = false) => _dataAccess.GetAllMedicationCategories(includeInactive);
        public MedicationCategory? GetMedicationCategoryById(int id) => _dataAccess.GetMedicationCategoryById(id);
        public string CreateMedicationCategory(string name, string? desc) => _dataAccess.CreateMedicationCategory(name, desc);
        public string UpdateMedicationCategory(int id, string name, string? desc) => _dataAccess.UpdateMedicationCategory(id, name, desc);
        public string DeleteMedicationCategory(int id) => _dataAccess.DeleteMedicationCategory(id);
        public string RestoreMedicationCategory(int id) => _dataAccess.RestoreMedicationCategory(id);

        // ── MEDICATIONS ───────────────────────────────────────────────
        public List<Medication> GetAllMedications(bool includeInactive = false) => _dataAccess.GetAllMedications(includeInactive);
        public Medication? GetMedicationById(int id) => _dataAccess.GetMedicationById(id);
        public string CreateMedication(string name, string? desc, int catId) => _dataAccess.CreateMedication(name, desc, catId);
        public string UpdateMedication(int id, string name, string? desc, int catId) => _dataAccess.UpdateMedication(id, name, desc, catId);
        public string DeleteMedication(int id) => _dataAccess.DeleteMedication(id);
        public string RestoreMedication(int id) => _dataAccess.RestoreMedication(id);

        // ── ACTIVITY LOG ──────────────────────────────────────────────
        public List<ActivityLogEntry> GetActivityLog() => _dataAccess.GetActivityLog();
        public void LogActivity(string action, string performedBy) => _dataAccess.LogActivity(action, performedBy);
    }
}