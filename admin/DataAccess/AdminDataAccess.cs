using System.Data;
using Microsoft.Data.SqlClient;
using admin.Models;

namespace admin.DataAccess
{
    public class AdminDataAccess
    {
        private readonly string _connectionString;

        public AdminDataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ── DASHBOARD COUNTS ─────────────────────────────────────────

        public (int conditions, int allergies, int medications, int conditionCats, int allergyCats, int medicationCats) GetDashboardCounts()
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT " +
                "(SELECT COUNT(*) FROM MedicalConditions) AS ConditionCount, " +
                "(SELECT COUNT(*) FROM Allergies) AS AllergyCount, " +
                "(SELECT COUNT(*) FROM Medications) AS MedicationCount, " +
                "(SELECT COUNT(*) FROM MedicalConditionCategories) AS ConditionCatCount, " +
                "(SELECT COUNT(*) FROM AllergyCategories) AS AllergyCatCount, " +
                "(SELECT COUNT(*) FROM MedicationCategories) AS MedicationCatCount", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                        reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5));
            return (0, 0, 0, 0, 0, 0);
        }

        // ── CONDITION CATEGORIES ──────────────────────────────────────

        public List<ConditionCategory> GetAllConditionCategories()
        {
            var list = new List<ConditionCategory>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllConditionCategories", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new ConditionCategory
                {
                    ConditionCategoryID = reader.GetInt32(reader.GetOrdinal("ConditionCategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                            ? null
                                            : reader.GetString(reader.GetOrdinal("Description"))
                });
            return list;
        }

        public ConditionCategory? GetConditionCategoryById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetConditionCategoryById", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ConditionCategoryID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return new ConditionCategory
                {
                    ConditionCategoryID = reader.GetInt32(reader.GetOrdinal("ConditionCategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                            ? null
                                            : reader.GetString(reader.GetOrdinal("Description"))
                };
            return null;
        }

        public string CreateConditionCategory(string categoryName, string? description)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CreateConditionCategory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CategoryName", categoryName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string UpdateConditionCategory(int id, string categoryName, string? description)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdateConditionCategory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ConditionCategoryID", id);
            cmd.Parameters.AddWithValue("@CategoryName", categoryName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string DeleteConditionCategory(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_DeleteConditionCategory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ConditionCategoryID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        // ── MEDICAL CONDITIONS ────────────────────────────────────────

        public List<MedicalCondition> GetAllConditions()
        {
            var list = new List<MedicalCondition>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllConditions", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new MedicalCondition
                {
                    ConditionID = reader.GetInt32(reader.GetOrdinal("ConditionID")),
                    ConditionName = reader.GetString(reader.GetOrdinal("ConditionName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                            ? null : reader.GetString(reader.GetOrdinal("Description")),
                    ConditionCategoryID = reader.GetInt32(reader.GetOrdinal("ConditionCategoryID")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                                            ? null : reader.GetString(reader.GetOrdinal("CategoryName"))
                });
            return list;
        }

        public MedicalCondition? GetConditionById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetConditionById", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ConditionID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return new MedicalCondition
                {
                    ConditionID = reader.GetInt32(reader.GetOrdinal("ConditionID")),
                    ConditionName = reader.GetString(reader.GetOrdinal("ConditionName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                            ? null : reader.GetString(reader.GetOrdinal("Description")),
                    ConditionCategoryID = reader.GetInt32(reader.GetOrdinal("ConditionCategoryID")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                                            ? null : reader.GetString(reader.GetOrdinal("CategoryName"))
                };
            return null;
        }

        public string CreateCondition(string conditionName, string? description, int categoryId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CreateCondition", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ConditionName", conditionName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ConditionCategoryID", categoryId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string UpdateCondition(int id, string conditionName, string? description, int categoryId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdateCondition", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ConditionID", id);
            cmd.Parameters.AddWithValue("@ConditionName", conditionName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ConditionCategoryID", categoryId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string DeleteCondition(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_DeleteCondition", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ConditionID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        // ── ALLERGY CATEGORIES ────────────────────────────────────────

        public List<AllergyCategory> GetAllAllergyCategories()
        {
            var list = new List<AllergyCategory>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllAllergyCategories", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new AllergyCategory
                {
                    AllergyCategoryID = reader.GetInt32(reader.GetOrdinal("AllergyCategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                          ? null : reader.GetString(reader.GetOrdinal("Description"))
                });
            return list;
        }

        public AllergyCategory? GetAllergyCategoryById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllergyCategoryById", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@AllergyCategoryID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return new AllergyCategory
                {
                    AllergyCategoryID = reader.GetInt32(reader.GetOrdinal("AllergyCategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                          ? null : reader.GetString(reader.GetOrdinal("Description"))
                };
            return null;
        }

        public string CreateAllergyCategory(string categoryName, string? description)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CreateAllergyCategory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CategoryName", categoryName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string UpdateAllergyCategory(int id, string categoryName, string? description)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdateAllergyCategory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@AllergyCategoryID", id);
            cmd.Parameters.AddWithValue("@CategoryName", categoryName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string DeleteAllergyCategory(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_DeleteAllergyCategory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@AllergyCategoryID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        // ── ALLERGIES ─────────────────────────────────────────────────

        public List<Allergy> GetAllAllergies()
        {
            var list = new List<Allergy>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllAllergies", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Allergy
                {
                    AllergyID = reader.GetInt32(reader.GetOrdinal("AllergyID")),
                    AllergyName = reader.GetString(reader.GetOrdinal("AllergyName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                          ? null : reader.GetString(reader.GetOrdinal("Description")),
                    AllergyCategoryID = reader.GetInt32(reader.GetOrdinal("AllergyCategoryID")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                                          ? null : reader.GetString(reader.GetOrdinal("CategoryName"))
                });
            return list;
        }

        public Allergy? GetAllergyById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllergyById", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@AllergyID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return new Allergy
                {
                    AllergyID = reader.GetInt32(reader.GetOrdinal("AllergyID")),
                    AllergyName = reader.GetString(reader.GetOrdinal("AllergyName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                          ? null : reader.GetString(reader.GetOrdinal("Description")),
                    AllergyCategoryID = reader.GetInt32(reader.GetOrdinal("AllergyCategoryID")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                                          ? null : reader.GetString(reader.GetOrdinal("CategoryName"))
                };
            return null;
        }

        public string CreateAllergy(string allergyName, string? description, int categoryId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CreateAllergy", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@AllergyName", allergyName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AllergyCategoryID", categoryId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string UpdateAllergy(int id, string allergyName, string? description, int categoryId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdateAllergy", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@AllergyID", id);
            cmd.Parameters.AddWithValue("@AllergyName", allergyName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AllergyCategoryID", categoryId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string DeleteAllergy(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_DeleteAllergy", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@AllergyID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        // ── MEDICATION CATEGORIES ─────────────────────────────────────

        public List<MedicationCategory> GetAllMedicationCategories()
        {
            var list = new List<MedicationCategory>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllMedicationCategories", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new MedicationCategory
                {
                    MedicationCategoryID = reader.GetInt32(reader.GetOrdinal("MedicationCategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                             ? null : reader.GetString(reader.GetOrdinal("Description"))
                });
            return list;
        }

        public MedicationCategory? GetMedicationCategoryById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetMedicationCategoryById", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MedicationCategoryID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return new MedicationCategory
                {
                    MedicationCategoryID = reader.GetInt32(reader.GetOrdinal("MedicationCategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                             ? null : reader.GetString(reader.GetOrdinal("Description"))
                };
            return null;
        }

        public string CreateMedicationCategory(string categoryName, string? description)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CreateMedicationCategory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CategoryName", categoryName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string UpdateMedicationCategory(int id, string categoryName, string? description)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdateMedicationCategory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MedicationCategoryID", id);
            cmd.Parameters.AddWithValue("@CategoryName", categoryName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string DeleteMedicationCategory(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_DeleteMedicationCategory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MedicationCategoryID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        // ── MEDICATIONS ───────────────────────────────────────────────

        public List<Medication> GetAllMedications()
        {
            var list = new List<Medication>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllMedications", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Medication
                {
                    MedicationID = reader.GetInt32(reader.GetOrdinal("MedicationID")),
                    MedicationName = reader.GetString(reader.GetOrdinal("MedicationName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                             ? null : reader.GetString(reader.GetOrdinal("Description")),
                    MedicationCategoryID = reader.GetInt32(reader.GetOrdinal("MedicationCategoryID")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                                             ? null : reader.GetString(reader.GetOrdinal("CategoryName"))
                });
            return list;
        }

        public Medication? GetMedicationById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetMedicationById", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MedicationID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return new Medication
                {
                    MedicationID = reader.GetInt32(reader.GetOrdinal("MedicationID")),
                    MedicationName = reader.GetString(reader.GetOrdinal("MedicationName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                             ? null : reader.GetString(reader.GetOrdinal("Description")),
                    MedicationCategoryID = reader.GetInt32(reader.GetOrdinal("MedicationCategoryID")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                                             ? null : reader.GetString(reader.GetOrdinal("CategoryName"))
                };
            return null;
        }

        public string CreateMedication(string medicationName, string? description, int categoryId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CreateMedication", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MedicationName", medicationName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MedicationCategoryID", categoryId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string UpdateMedication(int id, string medicationName, string? description, int categoryId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdateMedication", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MedicationID", id);
            cmd.Parameters.AddWithValue("@MedicationName", medicationName);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MedicationCategoryID", categoryId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        public string DeleteMedication(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_DeleteMedication", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MedicationID", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }

        // ── ACTIVITY LOG ──────────────────────────────────────────────

        public List<ActivityLog> GetActivityLog()
        {
            var list = new List<ActivityLog>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetActivityLog", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new ActivityLog
                {
                    LogID = reader.GetInt32(reader.GetOrdinal("LogID")),
                    Action = reader.GetString(reader.GetOrdinal("Action")),
                    PerformedBy = reader.GetString(reader.GetOrdinal("PerformedBy")),
                    LogDate = reader.GetDateTime(reader.GetOrdinal("LogDate"))
                });
            return list;
        }

        public void LogActivity(string action, string performedBy)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_LogActivity", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@PerformedBy", performedBy);
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}