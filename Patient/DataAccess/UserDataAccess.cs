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

        public (UserSession? user, string? passwordHash, bool isVerified) GetUserLoginData(string username)
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
                    IsEmailVerified = reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
                };
                return (user, passwordHash, user.IsEmailVerified);
            }

            return (null, null, false);
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

        public bool ResetPasswordByEmail(string email, string newPassword)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET PasswordHash = @Hash WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Hash", hashed);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<TestRequest> GetPatientTestRequests(int patientId)
        {
            var requests = new List<TestRequest>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetPatientTestRequests", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                requests.Add(new TestRequest
                {
                    RequestID = reader.GetInt32(0),
                    RequestNumber = reader.GetString(1),
                    RequestDate = reader.GetDateTime(2),
                    Urgency = reader.GetString(3),
                    RequestStatus = reader.GetString(4),
                    ClinicalNotes = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ReleaseNotes = reader.IsDBNull(6) ? null : reader.GetString(6),
                    ReleasedDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    DoctorName = reader.GetString(8)
                });
            }
            return requests;
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
                        PatientConditionID = (int)dr["PatientConditionID"],
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

            using (var cmd = new SqlCommand("sp_GetAllConditions", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.AllConditions.Add(((int)dr["ConditionID"], dr["ConditionName"].ToString()!));
            }

            using (var cmd = new SqlCommand("sp_GetAllAllergies", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.AllAllergies.Add(((int)dr["AllergyID"], dr["AllergyName"].ToString()!));
            }

            using (var cmd = new SqlCommand("sp_GetAllMedications", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    vm.AllMedications.Add(((int)dr["MedicationID"], dr["MedicationName"].ToString()!));
            }

            return vm;
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

        public void RemovePatientCondition(int patientConditionId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RemovePatientCondition", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientConditionID", patientConditionId);
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
            DateTime dob, string cellphone, string homeAddress)
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