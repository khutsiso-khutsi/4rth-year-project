using System.Data;
using Microsoft.Data.SqlClient;
using Patient.Models;

namespace Patient.DataAccess
{
    public class UserDataAccess
    {
        private readonly string _connectionString;

        public UserDataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        public (UserSession? user, string? passwordHash) GetUserLoginData(string username)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_LoginUser", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                var passwordHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                var user = new UserSession
                {
                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    RoleID = reader.GetInt32(reader.GetOrdinal("RoleID")),
                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                };
                return (user, passwordHash);
            }

            reader.Close();

            // Staff added through sp_AddDoctor (and similar) get a blank
            // Username, and the spec says the e-mail IS the username, so if
            // the proc found nobody, look the account up by e-mail instead.
            using var byEmail = new SqlCommand(@"
                SELECT u.UserID, u.Email, u.RoleID, r.RoleName, u.PasswordHash
                FROM Users u
                INNER JOIN Roles r ON r.RoleID = u.RoleID
                WHERE u.Email = @Email AND u.IsActive = 1", conn);
            byEmail.Parameters.AddWithValue("@Email", username);
            using var r2 = byEmail.ExecuteReader();
            if (r2.Read())
            {
                var user = new UserSession
                {
                    UserID = r2.GetInt32(r2.GetOrdinal("UserID")),
                    Email = r2.GetString(r2.GetOrdinal("Email")),
                    RoleID = r2.GetInt32(r2.GetOrdinal("RoleID")),
                    RoleName = r2.GetString(r2.GetOrdinal("RoleName")),
                };
                return (user, r2.GetString(r2.GetOrdinal("PasswordHash")));
            }

            return (null, null);
        }

        /// <summary>True when the account still has a temporary password that must be changed.</summary>
        public bool MustChangePassword(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT ISNULL(MustChangePassword, 0) FROM Users WHERE UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value && Convert.ToBoolean(result);
        }

        /// <summary>Checks a password against the one currently stored for this user.</summary>
        public bool PasswordMatches(int userId, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT PasswordHash FROM Users WHERE UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            conn.Open();
            var hash = cmd.ExecuteScalar() as string;
            return hash != null && BCrypt.Net.BCrypt.Verify(password, hash);
        }

        /// <summary>Saves the user's own new password and clears the "must change" flag.</summary>
        public void SetOwnPassword(int userId, string newPassword)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET PasswordHash = @Hash, MustChangePassword = 0 WHERE UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@Hash", BCrypt.Net.BCrypt.HashPassword(newPassword));
            cmd.Parameters.AddWithValue("@UserID", userId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void UpdateLastLogin(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET LastLoginDate = GETDATE() WHERE UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public (string result, int newUserId) RegisterUser(string username, string email,
    string passwordHash, int roleId, string firstName, string lastName,
    string idNumber, DateTime dateOfBirth, string cellphone, string homeAddress)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RegisterUser", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@RoleID", roleId);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@IDNumber", idNumber);
            cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
            cmd.Parameters.AddWithValue("@CellphoneNumber", cellphone);
            cmd.Parameters.AddWithValue("@HomeAddress", homeAddress);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string result = reader.GetString(reader.GetOrdinal("Result"));
                int newUserId = reader.GetInt32(reader.GetOrdinal("NewUserID"));
                return (result, newUserId);
            }
            return ("ERROR", 0);
        }

        public void CreatePatientRecord(int userId, string firstName, string lastName,
            string idNumber, DateTime dateOfBirth, string cellphoneNumber, string homeAddress)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                INSERT INTO Patients (UserID, FirstName, LastName, IDNumber, DateOfBirth, CellphoneNumber, HomeAddress)
                VALUES (@UserId, @FirstName, @LastName, @IDNumber, @DateOfBirth, @CellphoneNumber, @HomeAddress)", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@IDNumber", idNumber);
            cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
            cmd.Parameters.AddWithValue("@CellphoneNumber", cellphoneNumber);
            cmd.Parameters.AddWithValue("@HomeAddress", homeAddress);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<(int Id, string Name)> GetAllRoles()
        {
            var roles = new List<(int, string)>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT RoleID, RoleName FROM Roles ORDER BY RoleName", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                roles.Add((reader.GetInt32(0), reader.GetString(1)));
            return roles;
        }

        public bool SaveResetToken(string email, string token, DateTime expiry)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET ResetToken = @Token, ResetTokenExpiry = @Expiry WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@Expiry", expiry);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ValidateResetToken(string token)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Users WHERE ResetToken = @Token AND ResetTokenExpiry > GETDATE()", conn);
            cmd.Parameters.AddWithValue("@Token", token);
            conn.Open();
            return (int)cmd.ExecuteScalar() > 0;
        }

        public bool ResetPassword(string token, string newPassword)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET PasswordHash = @Hash, ResetToken = NULL, ResetTokenExpiry = NULL WHERE ResetToken = @Token AND ResetTokenExpiry > GETDATE()", conn);
            cmd.Parameters.AddWithValue("@Hash", hashed);
            cmd.Parameters.AddWithValue("@Token", token);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        // Is there an active account with this e-mail address? (forgot password)
        public bool EmailExists(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Users WHERE Email = @Email AND IsActive = 1", conn);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public bool ResetPasswordByEmail(string email, string newPassword)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET PasswordHash = @Hash, MustChangePassword = 1 WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Hash", hashed);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<TestRequest> GetPatientTestRequests(int patientId)
        {
            // Call stored procedure dbo.GetPatientTestRequests which returns one row per request item
            var list = new List<TestRequest>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetPatientTestRequests", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            conn.Open();
            using var reader = cmd.ExecuteReader();

            var map = new Dictionary<int, TestRequest>();
            while (reader.Read())
            {
                var requestId = reader.GetInt32(reader.GetOrdinal("RequestID"));
                if (!map.TryGetValue(requestId, out var req))
                {
                    req = new TestRequest
                    {
                        RequestID = requestId,
                        RequestNumber = reader.IsDBNull(reader.GetOrdinal("RequestNumber")) ? "" : reader.GetString(reader.GetOrdinal("RequestNumber")),
                        RequestDate = reader.GetDateTime(reader.GetOrdinal("RequestDate")),
                        Urgency = reader.IsDBNull(reader.GetOrdinal("Urgency")) ? "" : reader.GetString(reader.GetOrdinal("Urgency")),
                        RequestStatus = reader.IsDBNull(reader.GetOrdinal("RequestStatus")) ? "" : reader.GetString(reader.GetOrdinal("RequestStatus")),
                        ClinicalNotes = reader.IsDBNull(reader.GetOrdinal("ClinicalNotes")) ? null : reader.GetString(reader.GetOrdinal("ClinicalNotes")),
                        ReleaseNotes = reader.IsDBNull(reader.GetOrdinal("ReleaseNotes")) ? null : reader.GetString(reader.GetOrdinal("ReleaseNotes")),
                        ReleasedDate = reader.IsDBNull(reader.GetOrdinal("ReleasedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReleasedDate")),
                        DoctorName = reader.IsDBNull(reader.GetOrdinal("DoctorName")) ? "" : reader.GetString(reader.GetOrdinal("DoctorName")),
                        Items = new List<TestRequestItem>()
                    };
                    map[requestId] = req;
                }

                // If row contains item data, add it
                if (!reader.IsDBNull(reader.GetOrdinal("RequestItemID")))
                {
                    var item = new TestRequestItem
                    {
                        RequestItemID = reader.GetInt32(reader.GetOrdinal("RequestItemID")),
                        RequestID = requestId,
                        TestName = reader.IsDBNull(reader.GetOrdinal("TestName")) ? "" : reader.GetString(reader.GetOrdinal("TestName")),
                        CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? "" : reader.GetString(reader.GetOrdinal("CategoryName")),
                        ItemStatus = reader.IsDBNull(reader.GetOrdinal("ItemStatus")) ? "" : reader.GetString(reader.GetOrdinal("ItemStatus")),
                        ResultValue = reader.IsDBNull(reader.GetOrdinal("ResultValue")) ? null : reader.GetDecimal(reader.GetOrdinal("ResultValue")),
                        ResultNotes = reader.IsDBNull(reader.GetOrdinal("ResultNotes")) ? null : reader.GetString(reader.GetOrdinal("ResultNotes")),
                        IsAbnormal = !reader.IsDBNull(reader.GetOrdinal("IsAbnormal")) && reader.GetBoolean(reader.GetOrdinal("IsAbnormal")),
                        CompletionDateTime = reader.IsDBNull(reader.GetOrdinal("CompletionDateTime")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletionDateTime")),
                        VerificationDateTime = reader.IsDBNull(reader.GetOrdinal("VerificationDateTime")) ? null : reader.GetDateTime(reader.GetOrdinal("VerificationDateTime")),
                        UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName")) ? null : reader.GetString(reader.GetOrdinal("UnitName")),
                        NormalRangeMin = reader.IsDBNull(reader.GetOrdinal("NormalRangeMin")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMin")),
                        NormalRangeMax = reader.IsDBNull(reader.GetOrdinal("NormalRangeMax")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMax"))
                    };
                    req.Items.Add(item);
                }
            }

            list.AddRange(map.Values);
            return list;
        }

        public List<TestRequestItem> GetTestRequestItems(int requestId)
        {
            var items = new List<TestRequestItem>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetTestRequestItems", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new TestRequestItem
                {
                    RequestItemID = reader.GetInt32(0),
                    RequestID = reader.GetInt32(1),
                    TestName = reader.GetString(2),
                    CategoryName = reader.GetString(3),
                    ItemStatus = reader.GetString(4),
                    ResultValue = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    ResultNotes = reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsAbnormal = !reader.IsDBNull(7) && reader.GetBoolean(7),
                    CompletionDateTime = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    VerificationDateTime = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    UnitName = reader.IsDBNull(10) ? null : reader.GetString(10),
                    NormalRangeMin = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
                    NormalRangeMax = reader.IsDBNull(12) ? null : reader.GetDecimal(12)
                });
            }
            return items;
        }

        public int? GetPatientIdByUserId(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT PatientID FROM Patients WHERE UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result == null ? null : Convert.ToInt32(result);
        }

        public MedicalHistoryViewModel GetMedicalHistory(int patientId)
        {
            var vm = new MedicalHistoryViewModel();
            using var con = new SqlConnection(_connectionString);
            con.Open();

            using (var cmd = new SqlCommand("sp_GetPatientConditions", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PatientID", patientId);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.Conditions.Add(new PatientCondition
                    {
                        PatientID = (int)dr["PatientID"],
                        ConditionID = (int)dr["ConditionID"],
                        ConditionName = dr["ConditionName"].ToString()!,
                        DiagnosedDate = dr["DiagnosedDate"] as DateTime?,
                        Notes = dr["Notes"] as string
                    });
            }

            using (var cmd = new SqlCommand("sp_GetPatientAllergies", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PatientID", patientId);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.Allergies.Add(new PatientAllergy
                    {
                        AllergyID = (int)dr["AllergyID"],
                        AllergyName = dr["AllergyName"].ToString()!,
                        Severity = dr["Severity"] as string,
                        Notes = dr["Notes"] as string
                    });
            }

            using (var cmd = new SqlCommand("sp_GetPatientMedications", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PatientID", patientId);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.Medications.Add(new PatientMedication
                    {
                        MedicationID = (int)dr["MedicationID"],
                        MedicationName = dr["MedicationName"].ToString()!,
                        Dosage = dr["Dosage"] as string,
                        Frequency = dr["Frequency"] as string,
                        StartDate = dr["StartDate"] as DateTime?,
                        EndDate = dr["EndDate"] as DateTime?,
                        Notes = dr["Notes"] as string
                    });
            }

            // Pick-lists for the "add" forms, each item with its category so the
            // pickers can group them. Plain SQL (same pattern as GetAllRoles)
            // because the sp_GetAll* procs don't reliably return CategoryName.
            // Only active, non-deleted items; items whose category is missing
            // are grouped under "Other".
            vm.AllConditions = LoadLookup(con, @"
                SELECT x.ConditionID AS Id, x.ConditionName AS Name, ISNULL(c.CategoryName, 'Other') AS Category
                FROM MedicalConditions x
                LEFT JOIN MedicalConditionCategories c ON c.ConditionCategoryID = x.ConditionCategoryID
                WHERE x.IsActive = 1 AND x.DeletedAt IS NULL
                ORDER BY Category, Name");

            vm.AllAllergies = LoadLookup(con, @"
                SELECT x.AllergyID AS Id, x.AllergyName AS Name, ISNULL(c.CategoryName, 'Other') AS Category
                FROM Allergies x
                LEFT JOIN AllergyCategories c ON c.AllergyCategoryID = x.AllergyCategoryID
                WHERE x.IsActive = 1 AND x.DeletedAt IS NULL
                ORDER BY Category, Name");

            vm.AllMedications = LoadLookup(con, @"
                SELECT x.MedicationID AS Id, x.MedicationName AS Name, ISNULL(c.CategoryName, 'Other') AS Category
                FROM Medications x
                LEFT JOIN MedicationCategories c ON c.MedicationCategoryID = x.MedicationCategoryID
                WHERE x.IsActive = 1 AND x.DeletedAt IS NULL
                ORDER BY Category, Name");

            return vm;
        }

        private static List<LookupItem> LoadLookup(SqlConnection con, string sql)
        {
            var list = new List<LookupItem>();
            using var cmd = new SqlCommand(sql, con);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
                list.Add(new LookupItem
                {
                    Id = (int)dr["Id"],
                    Name = dr["Name"].ToString()!,
                    Category = dr["Category"].ToString()!
                });
            return list;
        }

        public void AddPatientCondition(int patientId, int conditionId, DateTime? diagnosedDate, string? notes)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_AddPatientCondition", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@ConditionID", conditionId);
            cmd.Parameters.AddWithValue("@DiagnosedDate", (object?)diagnosedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void RemovePatientCondition(int patientId, int conditionId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RemovePatientCondition", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@ConditionID", conditionId);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void AddPatientAllergy(int patientId, int allergyId, string? severity, string? notes)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_AddPatientAllergy", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@AllergyID", allergyId);
            cmd.Parameters.AddWithValue("@Severity", (object?)severity ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void RemovePatientAllergy(int patientId, int allergyId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RemovePatientAllergy", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@AllergyID", allergyId);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void AddPatientMedication(int patientId, int medicationId, string? dosage, string? frequency,
            DateTime? startDate, DateTime? endDate, string? notes)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_AddPatientMedication", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@MedicationID", medicationId);
            cmd.Parameters.AddWithValue("@Dosage", (object?)dosage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Frequency", (object?)frequency ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void RemovePatientMedication(int patientId, int medicationId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RemovePatientMedication", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@MedicationID", medicationId);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void LogActivity(string action, string performedBy)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_LogActivity", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@PerformedBy", performedBy);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public ConsentViewModel GetConsentData(int patientId, int selectedDoctorId = 0)
        {
            var vm = new ConsentViewModel();
            vm.SelectedDoctorID = selectedDoctorId;
            using var con = new SqlConnection(_connectionString);
            con.Open();

            using (var cmd = new SqlCommand("sp_GetAllDoctors", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.AllDoctors.Add(new DoctorOption
                    {
                        DoctorID = (int)dr["DoctorID"],
                        DoctorName = dr["DoctorName"].ToString()!,
                        Email = dr["Email"].ToString()!
                    });
            }

            using (var cmd = new SqlCommand("sp_GetPatientConsents", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PatientID", patientId);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.Consents.Add(new DoctorConsent
                    {
                        ConsentID = (int)dr["ConsentID"],
                        DoctorID = (int)dr["DoctorID"],
                        DoctorName = dr["DoctorName"].ToString()!,
                        DoctorEmail = dr["DoctorEmail"].ToString()!,
                        ConsentGranted = (bool)dr["ConsentGranted"],
                        GrantedDate = (DateTime)dr["GrantedDate"],
                        RevokedDate = dr["RevokedDate"] as DateTime?
                    });
            }

            // The patient's own conditions, for the "choose which ones" list
            using (var cmd = new SqlCommand("sp_GetPatientConditions", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PatientID", patientId);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.MyConditions.Add(new PatientCondition
                    {
                        PatientID = (int)dr["PatientID"],
                        ConditionID = (int)dr["ConditionID"],
                        ConditionName = dr["ConditionName"].ToString()!,
                        DiagnosedDate = dr["DiagnosedDate"] as DateTime?,
                        Notes = dr["Notes"] as string
                    });
            }

            // "All conditions" / "3 conditions" label for each doctor in the list
            foreach (var c in vm.Consents.Where(c => c.ConsentGranted))
            {
                var (all, ids) = LoadConditionAccess(con, patientId, c.DoctorID);
                vm.AccessSummaryByDoctor[c.DoctorID] = all
                    ? "All conditions"
                    : ids.Count == 1 ? "1 condition" : $"{ids.Count} conditions";
            }

            if (selectedDoctorId > 0)
            {
                vm.SelectedDoctorHasConsent = vm.Consents.Any(c => c.DoctorID == selectedDoctorId && c.ConsentGranted);
                var (all, ids) = LoadConditionAccess(con, patientId, selectedDoctorId);
                vm.ShareAllConditions = all;
                vm.SharedConditionIds = ids;
            }

            if (selectedDoctorId > 0)
            {
                using var cmd = new SqlCommand("sp_GetConsentTestRequests", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PatientID", patientId);
                cmd.Parameters.AddWithValue("@DoctorID", selectedDoctorId);
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.TestRequests.Add(new ConsentTestRequest
                    {
                        RequestID = (int)dr["RequestID"],
                        RequestNumber = dr["RequestNumber"].ToString()!,
                        RequestDate = (DateTime)dr["RequestDate"],
                        RequestStatus = dr["RequestStatus"].ToString()!,
                        IsShared = Convert.ToBoolean(dr["IsShared"])
                    });
            }

            return vm;
        }

        // ---- Condition-level access -----------------------------------------

        /// <summary>
        /// Reads which conditions a doctor may see. No row yet means the
        /// patient consented before this feature existed, so: all conditions.
        /// </summary>
        private static (bool ShareAll, List<int> ConditionIds) LoadConditionAccess(SqlConnection con, int patientId, int doctorId)
        {
            bool shareAll = true;
            var ids = new List<int>();

            using (var cmd = new SqlCommand(
                "SELECT ShareAllConditions FROM PatientConditionAccess WHERE PatientID = @P AND DoctorID = @D", con))
            {
                cmd.Parameters.AddWithValue("@P", patientId);
                cmd.Parameters.AddWithValue("@D", doctorId);
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return (true, ids);
                shareAll = Convert.ToBoolean(result);
            }

            if (!shareAll)
            {
                using var cmd = new SqlCommand(
                    "SELECT ConditionID FROM PatientConditionAccessItems WHERE PatientID = @P AND DoctorID = @D", con);
                cmd.Parameters.AddWithValue("@P", patientId);
                cmd.Parameters.AddWithValue("@D", doctorId);
                using var dr = cmd.ExecuteReader();
                while (dr.Read()) ids.Add((int)dr["ConditionID"]);
            }
            return (shareAll, ids);
        }

        public (bool ShareAll, List<int> ConditionIds) GetConditionAccess(int patientId, int doctorId)
        {
            using var con = new SqlConnection(_connectionString);
            con.Open();
            return LoadConditionAccess(con, patientId, doctorId);
        }

        /// <summary>
        /// Saves "all conditions" or the exact list of ticked conditions for
        /// one doctor. Replaces whatever was there before.
        /// </summary>
        public void SaveConditionAccess(int patientId, int doctorId, bool shareAll, IEnumerable<int> conditionIds)
        {
            using var con = new SqlConnection(_connectionString);
            con.Open();
            using var tx = con.BeginTransaction();

            using (var cmd = new SqlCommand(@"
                IF EXISTS (SELECT 1 FROM PatientConditionAccess WHERE PatientID = @P AND DoctorID = @D)
                    UPDATE PatientConditionAccess SET ShareAllConditions = @All, UpdatedAt = GETDATE()
                    WHERE PatientID = @P AND DoctorID = @D
                ELSE
                    INSERT INTO PatientConditionAccess (PatientID, DoctorID, ShareAllConditions, UpdatedAt)
                    VALUES (@P, @D, @All, GETDATE());
                DELETE FROM PatientConditionAccessItems WHERE PatientID = @P AND DoctorID = @D;", con, tx))
            {
                cmd.Parameters.AddWithValue("@P", patientId);
                cmd.Parameters.AddWithValue("@D", doctorId);
                cmd.Parameters.AddWithValue("@All", shareAll);
                cmd.ExecuteNonQuery();
            }

            if (!shareAll)
            {
                // The controller only passes conditions this patient actually has
                foreach (var id in conditionIds.Distinct())
                {
                    using var cmd = new SqlCommand(
                        "INSERT INTO PatientConditionAccessItems (PatientID, DoctorID, ConditionID) VALUES (@P, @D, @C)", con, tx);
                    cmd.Parameters.AddWithValue("@P", patientId);
                    cmd.Parameters.AddWithValue("@D", doctorId);
                    cmd.Parameters.AddWithValue("@C", id);
                    cmd.ExecuteNonQuery();
                }
            }

            tx.Commit();
        }

        /// <summary>True when the patient currently has consent switched on for this doctor.</summary>
        public bool DoctorHasConsent(int patientId, int doctorId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetPatientConsents", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
                if ((int)dr["DoctorID"] == doctorId && (bool)dr["ConsentGranted"])
                    return true;
            return false;
        }

        public void GrantConsent(int patientId, int doctorId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GrantConsent", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void RevokeConsent(int patientId, int doctorId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RevokeConsent", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void GrantTestRequestConsent(int patientId, int doctorId, int requestId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GrantTestRequestConsent", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void RevokeTestRequestConsent(int patientId, int doctorId, int requestId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RevokeTestRequestConsent", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public ProfileViewModel GetPatientProfile(int patientId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetPatientProfile", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new ProfileViewModel
                {
                    PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    IDNumber = reader.GetString(reader.GetOrdinal("IDNumber")),
                    DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                    CellphoneNumber = reader.GetString(reader.GetOrdinal("CellphoneNumber")),
                    HomeAddress = reader.GetString(reader.GetOrdinal("HomeAddress")),
                    RegistrationDate = reader.GetDateTime(reader.GetOrdinal("RegistrationDate")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                };
            }
            return null;
        }

        public void UpdatePatientProfile(int patientId, string firstName, string lastName,
    DateTime dob, string cellphone, string homeAddress, string email)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdatePatientProfile", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@DateOfBirth", dob);
            cmd.Parameters.AddWithValue("@CellphoneNumber", cellphone);
            cmd.Parameters.AddWithValue("@HomeAddress", homeAddress);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void ChangePatientPassword(int userId, string newPasswordHash)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ChangePatientPassword", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public (int UserId, string PasswordHash)? GetUserById(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT UserID, PasswordHash FROM Users WHERE UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return (reader.GetInt32(0), reader.GetString(1));
            return null;
        }

        public void SaveVerificationCode(string email, string code, DateTime expiry)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                UPDATE Users 
                SET VerificationCode = @Code, VerificationCodeExpiry = @Expiry
                WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Code", code);
            cmd.Parameters.AddWithValue("@Expiry", expiry);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public (bool success, int userId) VerifyCode(string email, string code)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT UserID, VerificationCode, VerificationCodeExpiry 
                FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string savedCode = reader["VerificationCode"]?.ToString() ?? "";
                DateTime expiry = Convert.ToDateTime(reader["VerificationCodeExpiry"]);
                int userId = Convert.ToInt32(reader["UserID"]);

                if (savedCode == code && DateTime.Now <= expiry)
                    return (true, userId);
            }
            return (false, 0);
        }

        public void MarkEmailVerified(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                UPDATE Users 
                SET IsEmailVerified = 1, 
                    VerificationCode = NULL, 
                    VerificationCodeExpiry = NULL
                WHERE UserID = @UserId", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}