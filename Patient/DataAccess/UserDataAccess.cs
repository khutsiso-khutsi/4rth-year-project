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

            return (null, null);
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
        public string RegisterUser(string username, string email, string passwordHash, int roleId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RegisterUser", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@RoleID", roleId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
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

            // Conditions
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

            // Allergies
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

            // Medications
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

            // Dropdowns
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

        public void AddPatientMedication(int patientId, int medicationId, string? dosage, string? frequency, DateTime? startDate, DateTime? endDate, string? notes)
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
    }
}