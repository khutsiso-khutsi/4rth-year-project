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

        // DASHBOARD COUNTS 

        public (int conditions, int allergies, int medications, int conditionCats, int allergyCats, int medicationCats) GetDashboardCounts()
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT " +
                "(SELECT COUNT(*) FROM MedicalConditions WHERE IsActive = 1) AS ConditionCount, " +
                "(SELECT COUNT(*) FROM Allergies WHERE IsActive = 1) AS AllergyCount, " +
                "(SELECT COUNT(*) FROM Medications WHERE IsActive = 1) AS MedicationCount, " +
                "(SELECT COUNT(*) FROM MedicalConditionCategories WHERE IsActive = 1) AS ConditionCatCount, " +
                "(SELECT COUNT(*) FROM AllergyCategories WHERE IsActive = 1) AS AllergyCatCount, " +
                "(SELECT COUNT(*) FROM MedicationCategories WHERE IsActive = 1) AS MedicationCatCount", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                        reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5));
            return (0, 0, 0, 0, 0, 0);
        }

        // CONDITION CATEGORIES 

        // Plain SQL (not the sp_) so IsActive/DeletedAt are always returned:
        // the page needs them to show deleted ("inactive") rows with a Restore button.
        // includeInactive now defaults to TRUE. Pass false explicitly for dropdowns.
        public List<ConditionCategory> GetAllConditionCategories(bool includeInactive = true)
        {
            var list = new List<ConditionCategory>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT ConditionCategoryID, CategoryName, Description, IsActive, DeletedAt
                FROM MedicalConditionCategories
                WHERE @IncludeInactive = 1 OR IsActive = 1
                ORDER BY IsActive DESC, CategoryName", conn);
            cmd.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new ConditionCategory
                {
                    ConditionCategoryID = reader.GetInt32(reader.GetOrdinal("ConditionCategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                            ? null
                                            : reader.GetString(reader.GetOrdinal("Description")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    DeletedAt = reader.IsDBNull(reader.GetOrdinal("DeletedAt"))
                                            ? null
                                            : reader.GetDateTime(reader.GetOrdinal("DeletedAt"))
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

        // Soft delete: marks the category inactive instead of removing the row,
        // so it can be restored. Blocked while it still has active condition(s),
        // otherwise they'd be left under a hidden category.
        public string DeleteConditionCategory(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using (var check = new SqlCommand(
                    "SELECT COUNT(*) FROM MedicalConditions WHERE ConditionCategoryID = @ID AND IsActive = 1", conn))
                {
                    check.Parameters.AddWithValue("@ID", id);
                    var inUse = Convert.ToInt32(check.ExecuteScalar());
                    if (inUse > 0)
                        return $"this category still has {inUse} active condition(s). Delete them or move them to another category first.";
                }

                using var cmd = new SqlCommand(
                    "UPDATE MedicalConditionCategories SET IsActive = 0, DeletedAt = GETDATE() WHERE ConditionCategoryID = @ID AND IsActive = 1", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                return cmd.ExecuteNonQuery() > 0 ? "SUCCESS" : "NOT_FOUND";
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
        }

        // MEDICAL CONDITIONS 

        // Plain SQL (not the sp_) so IsActive/DeletedAt and the category name are
        // always returned: the page needs them to show deleted ("inactive") rows
        // with a Restore button.
        public List<MedicalCondition> GetAllConditions(bool includeInactive = true)
        {
            var list = new List<MedicalCondition>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT x.ConditionID, x.ConditionName, x.Description, x.ConditionCategoryID, c.CategoryName, x.IsActive, x.DeletedAt
                FROM MedicalConditions x
                LEFT JOIN MedicalConditionCategories c ON c.ConditionCategoryID = x.ConditionCategoryID
                WHERE @IncludeInactive = 1 OR x.IsActive = 1
                ORDER BY x.IsActive DESC, x.ConditionName", conn);
            cmd.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);
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
                                            ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    DeletedAt = reader.IsDBNull(reader.GetOrdinal("DeletedAt"))
                                            ? null : reader.GetDateTime(reader.GetOrdinal("DeletedAt"))
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

        // Soft delete: marks the condition inactive instead of removing the row.
        // A real DELETE fails once patients have this condition on their records
        // (foreign key), and it couldn't be restored. RestoreCondition undoes this.
        public string DeleteCondition(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(
                    "UPDATE MedicalConditions SET IsActive = 0, DeletedAt = GETDATE() WHERE ConditionID = @ID AND IsActive = 1", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0 ? "SUCCESS" : "NOT_FOUND";
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
        }

        // ALLERGY CATEGORIES 

        // Plain SQL (not the sp_) so IsActive/DeletedAt are always returned:
        // the page needs them to show deleted ("inactive") rows with a Restore button.
        public List<AllergyCategory> GetAllAllergyCategories(bool includeInactive = true)
        {
            var list = new List<AllergyCategory>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT AllergyCategoryID, CategoryName, Description, IsActive, DeletedAt
                FROM AllergyCategories
                WHERE @IncludeInactive = 1 OR IsActive = 1
                ORDER BY IsActive DESC, CategoryName", conn);
            cmd.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new AllergyCategory
                {
                    AllergyCategoryID = reader.GetInt32(reader.GetOrdinal("AllergyCategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                            ? null
                                            : reader.GetString(reader.GetOrdinal("Description")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    DeletedAt = reader.IsDBNull(reader.GetOrdinal("DeletedAt"))
                                            ? null
                                            : reader.GetDateTime(reader.GetOrdinal("DeletedAt"))
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

        // Soft delete: marks the category inactive instead of removing the row,
        // so it can be restored. Blocked while it still has active allergy/allergies,
        // otherwise they'd be left under a hidden category.
        public string DeleteAllergyCategory(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using (var check = new SqlCommand(
                    "SELECT COUNT(*) FROM Allergies WHERE AllergyCategoryID = @ID AND IsActive = 1", conn))
                {
                    check.Parameters.AddWithValue("@ID", id);
                    var inUse = Convert.ToInt32(check.ExecuteScalar());
                    if (inUse > 0)
                        return $"this category still has {inUse} active allergy/allergies. Delete them or move them to another category first.";
                }

                using var cmd = new SqlCommand(
                    "UPDATE AllergyCategories SET IsActive = 0, DeletedAt = GETDATE() WHERE AllergyCategoryID = @ID AND IsActive = 1", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                return cmd.ExecuteNonQuery() > 0 ? "SUCCESS" : "NOT_FOUND";
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
        }

        //  ALLERGIES 

        // Plain SQL (not the sp_) so IsActive/DeletedAt and the category name are
        // always returned: the page needs them to show deleted ("inactive") rows
        // with a Restore button.
        public List<Allergy> GetAllAllergies(bool includeInactive = true)
        {
            var list = new List<Allergy>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT x.AllergyID, x.AllergyName, x.Description, x.AllergyCategoryID, c.CategoryName, x.IsActive, x.DeletedAt
                FROM Allergies x
                LEFT JOIN AllergyCategories c ON c.AllergyCategoryID = x.AllergyCategoryID
                WHERE @IncludeInactive = 1 OR x.IsActive = 1
                ORDER BY x.IsActive DESC, x.AllergyName", conn);
            cmd.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);
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
                                            ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    DeletedAt = reader.IsDBNull(reader.GetOrdinal("DeletedAt"))
                                            ? null : reader.GetDateTime(reader.GetOrdinal("DeletedAt"))
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

        // Soft delete: marks the allergy inactive instead of removing the row.
        // A real DELETE fails once patients have this allergy on their records
        // (foreign key), and it couldn't be restored. RestoreAllergy undoes this.
        public string DeleteAllergy(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(
                    "UPDATE Allergies SET IsActive = 0, DeletedAt = GETDATE() WHERE AllergyID = @ID AND IsActive = 1", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0 ? "SUCCESS" : "NOT_FOUND";
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
        }

        // MEDICATION CATEGORIES

        // Plain SQL (not the sp_) so IsActive/DeletedAt are always returned:
        // the page needs them to show deleted ("inactive") rows with a Restore button.
        public List<MedicationCategory> GetAllMedicationCategories(bool includeInactive = true)
        {
            var list = new List<MedicationCategory>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT MedicationCategoryID, CategoryName, Description, IsActive, DeletedAt
                FROM MedicationCategories
                WHERE @IncludeInactive = 1 OR IsActive = 1
                ORDER BY IsActive DESC, CategoryName", conn);
            cmd.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new MedicationCategory
                {
                    MedicationCategoryID = reader.GetInt32(reader.GetOrdinal("MedicationCategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                            ? null
                                            : reader.GetString(reader.GetOrdinal("Description")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    DeletedAt = reader.IsDBNull(reader.GetOrdinal("DeletedAt"))
                                            ? null
                                            : reader.GetDateTime(reader.GetOrdinal("DeletedAt"))
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

        // Soft delete: marks the category inactive instead of removing the row,
        // so it can be restored. Blocked while it still has active medication(s),
        // otherwise they'd be left under a hidden category.
        public string DeleteMedicationCategory(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using (var check = new SqlCommand(
                    "SELECT COUNT(*) FROM Medications WHERE MedicationCategoryID = @ID AND IsActive = 1", conn))
                {
                    check.Parameters.AddWithValue("@ID", id);
                    var inUse = Convert.ToInt32(check.ExecuteScalar());
                    if (inUse > 0)
                        return $"this category still has {inUse} active medication(s). Delete them or move them to another category first.";
                }

                using var cmd = new SqlCommand(
                    "UPDATE MedicationCategories SET IsActive = 0, DeletedAt = GETDATE() WHERE MedicationCategoryID = @ID AND IsActive = 1", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                return cmd.ExecuteNonQuery() > 0 ? "SUCCESS" : "NOT_FOUND";
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
        }

        //MEDICATIONS

        // Plain SQL (not the sp_) so IsActive/DeletedAt and the category name are
        // always returned: the page needs them to show deleted ("inactive") rows
        // with a Restore button.
        public List<Medication> GetAllMedications(bool includeInactive = true)
        {
            var list = new List<Medication>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT x.MedicationID, x.MedicationName, x.Description, x.MedicationCategoryID, c.CategoryName, x.IsActive, x.DeletedAt
                FROM Medications x
                LEFT JOIN MedicationCategories c ON c.MedicationCategoryID = x.MedicationCategoryID
                WHERE @IncludeInactive = 1 OR x.IsActive = 1
                ORDER BY x.IsActive DESC, x.MedicationName", conn);
            cmd.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);
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
                                            ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    DeletedAt = reader.IsDBNull(reader.GetOrdinal("DeletedAt"))
                                            ? null : reader.GetDateTime(reader.GetOrdinal("DeletedAt"))
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

        // Soft delete: marks the medication inactive instead of removing the row.
        // A real DELETE fails once patients have this medication on their records
        // (foreign key), and it couldn't be restored. RestoreMedication undoes this.
        public string DeleteMedication(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(
                    "UPDATE Medications SET IsActive = 0, DeletedAt = GETDATE() WHERE MedicationID = @ID AND IsActive = 1", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0 ? "SUCCESS" : "NOT_FOUND";
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
        }

        // RESTORE METHODS (soft-undelete)
        // Errors now return the real message ("ERROR: ...").
        // Items (condition/allergy/medication) cannot be restored while their
        // category is still inactive.

        // Categories: no parent, so a simple restore.
        private string RestoreCategory(string table, string idColumn, int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(
                    $"UPDATE {table} SET IsActive = 1, DeletedAt = NULL WHERE {idColumn} = @ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0 ? "SUCCESS" : "NOT_FOUND";
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
        }

        // Items: check the parent category is active first.
        private string RestoreItem(string itemTable, string itemIdColumn, string itemCategoryColumn,
                                   string categoryTable, string categoryIdColumn, int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using (var check = new SqlCommand($@"
                    SELECT COUNT(*) FROM {itemTable} x
                    INNER JOIN {categoryTable} c ON c.{categoryIdColumn} = x.{itemCategoryColumn}
                    WHERE x.{itemIdColumn} = @ID AND c.IsActive = 0", conn))
                {
                    check.Parameters.AddWithValue("@ID", id);
                    if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                        return "its category is still deleted. Restore the category first.";
                }

                using var cmd = new SqlCommand(
                    $"UPDATE {itemTable} SET IsActive = 1, DeletedAt = NULL WHERE {itemIdColumn} = @ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                return cmd.ExecuteNonQuery() > 0 ? "SUCCESS" : "NOT_FOUND";
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
        }

        public string RestoreConditionCategory(int id)
            => RestoreCategory("MedicalConditionCategories", "ConditionCategoryID", id);

        public string RestoreCondition(int id)
            => RestoreItem("MedicalConditions", "ConditionID", "ConditionCategoryID",
                           "MedicalConditionCategories", "ConditionCategoryID", id);

        public string RestoreAllergyCategory(int id)
            => RestoreCategory("AllergyCategories", "AllergyCategoryID", id);

        public string RestoreAllergy(int id)
            => RestoreItem("Allergies", "AllergyID", "AllergyCategoryID",
                           "AllergyCategories", "AllergyCategoryID", id);

        public string RestoreMedicationCategory(int id)
            => RestoreCategory("MedicationCategories", "MedicationCategoryID", id);

        public string RestoreMedication(int id)
            => RestoreItem("Medications", "MedicationID", "MedicationCategoryID",
                           "MedicationCategories", "MedicationCategoryID", id);

        // ACTIVITY LOG

        public List<ActivityLogEntry> GetActivityLog()
        {
            var logs = new List<ActivityLogEntry>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetActivityLog", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new ActivityLogEntry
                {
                    LogID = reader.GetInt32(0),
                    Action = reader.IsDBNull(1) ? null : reader.GetString(1),
                    PerformedBy = reader.IsDBNull(2) ? null : reader.GetString(2),
                    LogDate = reader.GetDateTime(3)
                });
            }
            return logs;
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