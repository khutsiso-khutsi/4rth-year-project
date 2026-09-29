using System.Data;
using Microsoft.Data.SqlClient;
using Doctor.Models;
using Patient.Models;

namespace Doctor.DataAccess
{
    public class DoctorDataAccess
    {
        private readonly string _connectionString;

        public DoctorDataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ---------- Feature 1: Manage Patient Records ----------

        public (string result, int newUserId, int newPatientId) RegisterPatientByDoctor(
            string username, string email, string passwordHash, string firstName,
            string lastName, string idNumber, DateTime dateOfBirth, string cellphone)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RegisterPatientByDoctor", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@IDNumber", idNumber);
            cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
            cmd.Parameters.AddWithValue("@CellphoneNumber", cellphone);

            var resultParam = new SqlParameter("@Result", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
            var newUserIdParam = new SqlParameter("@NewUserID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var newPatientIdParam = new SqlParameter("@NewPatientID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultParam);
            cmd.Parameters.Add(newUserIdParam);
            cmd.Parameters.Add(newPatientIdParam);

            conn.Open();
            cmd.ExecuteNonQuery();

            return (
                resultParam.Value?.ToString() ?? "ERROR",
                newUserIdParam.Value != DBNull.Value ? (int)newUserIdParam.Value : 0,
                newPatientIdParam.Value != DBNull.Value ? (int)newPatientIdParam.Value : 0
            );
        }

        // ---------- Feature 2: Create Test Requests ----------

        public List<(int SampleTypeID, string SampleTypeName)> GetRequiredSamplesForTestTypes(string testTypeIdsJson)
        {
            var samples = new List<(int, string)>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetRequiredSamplesForTestTypes", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@TestTypeIDsJson", testTypeIdsJson);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                samples.Add((reader.GetInt32(0), reader.GetString(1)));
            return samples;
        }

        public (string result, int newRequestId) CreateTestRequest(
            int patientId, int doctorId, string urgency, string? clinicalNotes,
            string testTypeIdsJson, string barcodesJson)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CreateTestRequest", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@Urgency", urgency);
            cmd.Parameters.AddWithValue("@ClinicalNotes", (object?)clinicalNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TestTypeIDsJson", testTypeIdsJson);
            cmd.Parameters.AddWithValue("@BarcodesJson", barcodesJson);

            var resultParam = new SqlParameter("@Result", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
            var newRequestIdParam = new SqlParameter("@NewRequestID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultParam);
            cmd.Parameters.Add(newRequestIdParam);

            conn.Open();
            cmd.ExecuteNonQuery();

            return (
                resultParam.Value?.ToString() ?? "ERROR",
                newRequestIdParam.Value != DBNull.Value ? (int)newRequestIdParam.Value : 0
            );
        }

        // ---------- Feature 3: Track Test Request Status / Cancel ----------

        public (string result, string? notifyDoctorEmail, string? notifyDoctorName) CancelTestRequest(
            int requestId, string cancellationReason, int cancelledByUserId, string cancelledByRole)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CancelTestRequest", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            cmd.Parameters.AddWithValue("@CancellationReason", cancellationReason);
            cmd.Parameters.AddWithValue("@CancelledByUserID", cancelledByUserId);
            cmd.Parameters.AddWithValue("@CancelledByRole", cancelledByRole);

            var resultParam = new SqlParameter("@Result", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
            var emailParam = new SqlParameter("@NotifyDoctorEmail", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };
            var nameParam = new SqlParameter("@NotifyDoctorName", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultParam);
            cmd.Parameters.Add(emailParam);
            cmd.Parameters.Add(nameParam);

            conn.Open();
            cmd.ExecuteNonQuery();

            return (
                resultParam.Value?.ToString() ?? "ERROR",
                emailParam.Value == DBNull.Value ? null : emailParam.Value.ToString(),
                nameParam.Value == DBNull.Value ? null : nameParam.Value.ToString()
            );
        }

        public List<TestRequest> GetPatientTestRequests(int patientId)
        {
            var list = new List<TestRequest>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetPatientTestRequests", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TestRequest
                {
                    RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                    RequestNumber = reader.IsDBNull(reader.GetOrdinal("RequestNumber")) ? "" : reader.GetString(reader.GetOrdinal("RequestNumber")),
                    RequestDate = reader.GetDateTime(reader.GetOrdinal("RequestDate")),
                    RequestStatus = reader.IsDBNull(reader.GetOrdinal("RequestStatus")) ? "" : reader.GetString(reader.GetOrdinal("RequestStatus"))
                });
            }
            return list;
        }

        // ---------- Feature 4: View Results ----------

        public List<TestResultViewModel> GetTestResultsForDoctor(int requestId, int doctorId)
        {
            var results = new List<TestResultViewModel>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetTestResultsForDoctor", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new TestResultViewModel
                {
                    RequestItemID = reader.GetInt32(reader.GetOrdinal("RequestItemID")),
                    TestTypeName = reader.GetString(reader.GetOrdinal("TestName")),
                    ResultValue = reader.IsDBNull(reader.GetOrdinal("ResultValue")) ? null : reader.GetDecimal(reader.GetOrdinal("ResultValue")),
                    NormalRangeMin = reader.IsDBNull(reader.GetOrdinal("NormalRangeMin")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMin")),
                    NormalRangeMax = reader.IsDBNull(reader.GetOrdinal("NormalRangeMax")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMax")),
                    ResultNotes = reader.IsDBNull(reader.GetOrdinal("ResultNotes")) ? null : reader.GetString(reader.GetOrdinal("ResultNotes")),
                    IsAbnormal = !reader.IsDBNull(reader.GetOrdinal("IsAbnormal")) && reader.GetBoolean(reader.GetOrdinal("IsAbnormal")),
                    ReleasedDate = null
                });
            }
            return results;
        }

        public string ReleaseTestResults(int requestId, int doctorId, string? releaseNote)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ReleaseTestResults", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@ReleaseNote", (object?)releaseNote ?? DBNull.Value);

            var resultParam = new SqlParameter("@Result", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultParam);

            conn.Open();
            cmd.ExecuteNonQuery();
            return resultParam.Value?.ToString() ?? "ERROR";
        }

        // ---------- Feature 5: View Alerts ----------

        public List<AlertViewModel> GetDoctorAlerts(int doctorId, DateTime? fromDate)
        {
            var alerts = new List<AlertViewModel>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetDoctorAlerts", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                alerts.Add(new AlertViewModel
                {
                    PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                    PatientName = reader.GetString(reader.GetOrdinal("FirstName")) + " " + reader.GetString(reader.GetOrdinal("LastName")),
                    TestRequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                    TestTypeName = reader.GetString(reader.GetOrdinal("TestName")),
                    ResultValue = reader.IsDBNull(reader.GetOrdinal("ResultValue")) ? null : reader.GetDecimal(reader.GetOrdinal("ResultValue")),
                    DateFlagged = reader.IsDBNull(reader.GetOrdinal("CompletionDateTime")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("CompletionDateTime")),
                    IsAbnormal = reader.GetBoolean(reader.GetOrdinal("IsAbnormal"))
                });
            }
            return alerts;
        }

        // ---------- Feature 6: Doctor Reports ----------

        public List<TestRequestViewModel> GetDoctorTestRequestsByDateRange(int doctorId, DateTime start, DateTime end)
        {
            var list = new List<TestRequestViewModel>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetDoctorTestRequestsByDateRange", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@StartDate", start);
            cmd.Parameters.AddWithValue("@EndDate", end);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TestRequestViewModel
                {
                    RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                    PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                    RequestDate = reader.GetDateTime(reader.GetOrdinal("RequestDate")),
                    Urgency = reader.GetString(reader.GetOrdinal("Urgency")),
                    Status = reader.GetString(reader.GetOrdinal("RequestStatus"))
                });
            }
            return list;
        }

        // ---------- Shared ----------

        public List<(int DoctorID, string DoctorName, string Email)> GetAllDoctors()
        {
            var doctors = new List<(int, string, string)>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllDoctors", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                doctors.Add((
                    (int)reader["DoctorID"],
                    reader["DoctorName"].ToString()!,
                    reader["Email"].ToString()!
                ));
            return doctors;
        }
    }
}