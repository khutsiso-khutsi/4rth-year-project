using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using _4th_year_set_up.Models;

namespace _4th_year_set_up.DataAccess
{
    /// <summary>
    /// Data access for the Doctor module (patient records, test requests,
    /// results, alerts, reports). Follows the same raw ADO.NET +
    /// stored-procedure convention as Patient.DataAccess.UserDataAccess,
    /// so it can sit next to it without introducing a third pattern.
    /// See Database/DoctorModule.sql for the stored procedures this calls.
    /// </summary>
    public class DoctorDataAccess
    {
        private readonly string _connectionString;

        public DoctorDataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ---- Identity -----------------------------------------------------

        public (int DoctorID, string DoctorName, string Email)? GetDoctorByUserId(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetDoctorIdByUserId", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserID", userId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (
                    reader.GetInt32(reader.GetOrdinal("DoctorID")),
                    reader.GetString(reader.GetOrdinal("DoctorName")),
                    reader.GetString(reader.GetOrdinal("Email"))
                );
            }
            return null;
        }

        // ---- 1. Manage Patient Records -------------------------------------

        public List<PatientRecordListItem> SearchPatients(string? searchTerm)
        {
            var list = new List<PatientRecordListItem>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_SearchPatients", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new PatientRecordListItem
                {
                    PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    IDNumber = reader.GetString(reader.GetOrdinal("IDNumber")),
                    DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                    CellphoneNumber = reader.IsDBNull(reader.GetOrdinal("CellphoneNumber")) ? "" : reader.GetString(reader.GetOrdinal("CellphoneNumber")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                });
            }
            return list;
        }

        public bool PatientIdNumberExists(string idNumber)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CheckPatientIdNumberExists", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IDNumber", idNumber);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt32(result) > 0;
        }

        // ---- 2. Create Test Requests ---------------------------------------

        public List<TestTypeOption> GetAllTestTypes()
        {
            var list = new List<TestTypeOption>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetAllTestTypes", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TestTypeOption
                {
                    TestTypeID = reader.GetInt32(reader.GetOrdinal("TestTypeID")),
                    TestName = reader.GetString(reader.GetOrdinal("TestName")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName")) ? null : reader.GetString(reader.GetOrdinal("UnitName")),
                    NormalRangeMin = reader.IsDBNull(reader.GetOrdinal("NormalRangeMin")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMin")),
                    NormalRangeMax = reader.IsDBNull(reader.GetOrdinal("NormalRangeMax")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMax")),
                    SampleTypeName = reader.IsDBNull(reader.GetOrdinal("SampleTypeName")) ? null : reader.GetString(reader.GetOrdinal("SampleTypeName"))
                });
            }
            return list;
        }

        public (int RequestID, string RequestNumber) CreateTestRequest(
            int patientId, int doctorId, string urgency, string? clinicalNotes,
            List<int> testTypeIds, List<string> barcodes)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CreateTestRequest", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@Urgency", urgency);
            cmd.Parameters.AddWithValue("@ClinicalNotes", (object?)clinicalNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TestTypeIdsCsv", string.Join(",", testTypeIds));
            cmd.Parameters.AddWithValue("@BarcodesCsv", string.Join(",", barcodes));
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (reader.GetInt32(reader.GetOrdinal("RequestID")),
                        reader.GetString(reader.GetOrdinal("RequestNumber")));
            }
            throw new InvalidOperationException("Test request could not be created.");
        }

        // ---- 3/4. Track status + View results ------------------------------

        public List<DoctorTestRequestSummary> GetDoctorTestRequests(int doctorId, DateTime? from = null, DateTime? to = null)
        {
            var list = new List<DoctorTestRequestSummary>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetDoctorTestRequests", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@FromDate", (object?)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)to ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DoctorTestRequestSummary
                {
                    RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                    RequestNumber = reader.GetString(reader.GetOrdinal("RequestNumber")),
                    RequestDate = reader.GetDateTime(reader.GetOrdinal("RequestDate")),
                    Urgency = reader.GetString(reader.GetOrdinal("Urgency")),
                    RequestStatus = reader.GetString(reader.GetOrdinal("RequestStatus")),
                    ClinicalNotes = reader.IsDBNull(reader.GetOrdinal("ClinicalNotes")) ? null : reader.GetString(reader.GetOrdinal("ClinicalNotes")),
                    ReleaseNotes = reader.IsDBNull(reader.GetOrdinal("ReleaseNotes")) ? null : reader.GetString(reader.GetOrdinal("ReleaseNotes")),
                    ReleasedDate = reader.IsDBNull(reader.GetOrdinal("ReleasedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReleasedDate")),
                    CancelReason = reader.IsDBNull(reader.GetOrdinal("CancelReason")) ? null : reader.GetString(reader.GetOrdinal("CancelReason")),
                    CancelledBy = reader.IsDBNull(reader.GetOrdinal("CancelledBy")) ? null : reader.GetString(reader.GetOrdinal("CancelledBy")),
                    PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                    PatientName = reader.GetString(reader.GetOrdinal("PatientName")),
                    PatientEmail = reader.GetString(reader.GetOrdinal("PatientEmail"))
                });
            }
            return list;
        }

        public DoctorTestRequestSummary? GetTestRequestDetail(int doctorId, int requestId)
        {
            var request = GetDoctorTestRequests(doctorId).Find(r => r.RequestID == requestId);
            if (request == null) return null;

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetTestRequestItemsForDoctor", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                request.Items.Add(new TestRequestItemDetail
                {
                    RequestItemID = reader.GetInt32(reader.GetOrdinal("RequestItemID")),
                    RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                    TestName = reader.GetString(reader.GetOrdinal("TestName")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    ItemStatus = reader.GetString(reader.GetOrdinal("ItemStatus")),
                    ResultValue = reader.IsDBNull(reader.GetOrdinal("ResultValue")) ? null : reader.GetDecimal(reader.GetOrdinal("ResultValue")),
                    ResultNotes = reader.IsDBNull(reader.GetOrdinal("ResultNotes")) ? null : reader.GetString(reader.GetOrdinal("ResultNotes")),
                    IsAbnormal = !reader.IsDBNull(reader.GetOrdinal("IsAbnormal")) && reader.GetBoolean(reader.GetOrdinal("IsAbnormal")),
                    CompletionDateTime = reader.IsDBNull(reader.GetOrdinal("CompletionDateTime")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletionDateTime")),
                    VerificationDateTime = reader.IsDBNull(reader.GetOrdinal("VerificationDateTime")) ? null : reader.GetDateTime(reader.GetOrdinal("VerificationDateTime")),
                    UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName")) ? null : reader.GetString(reader.GetOrdinal("UnitName")),
                    NormalRangeMin = reader.IsDBNull(reader.GetOrdinal("NormalRangeMin")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMin")),
                    NormalRangeMax = reader.IsDBNull(reader.GetOrdinal("NormalRangeMax")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMax"))
                });
            }

            if (reader.NextResult())
            {
                while (reader.Read())
                {
                    request.Samples.Add(new TestRequestSample
                    {
                        SampleID = reader.GetInt32(reader.GetOrdinal("SampleID")),
                        BarcodeValue = reader.GetString(reader.GetOrdinal("BarcodeValue")),
                        CollectedDate = reader.IsDBNull(reader.GetOrdinal("CollectedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CollectedDate")),
                        ReceivedDate = reader.IsDBNull(reader.GetOrdinal("ReceivedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReceivedDate"))
                    });
                }
            }

            return request;
        }

        public bool CancelTestRequest(int requestId, string reason)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CancelTestRequest", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            cmd.Parameters.AddWithValue("@Reason", reason);
            cmd.Parameters.AddWithValue("@CancelledBy", "Doctor");
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return Convert.ToInt32(reader["RowsAffected"]) > 0;
            return false;
        }

        public void ReleaseResults(int requestId, string releaseNotes)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ReleaseTestRequestResults", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            cmd.Parameters.AddWithValue("@ReleaseNotes", releaseNotes);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // ---- 5. Alerts ------------------------------------------------------

        public List<AbnormalAlertViewModel> GetAbnormalAlerts(int doctorId, DateTime? from, DateTime? to)
        {
            var list = new List<AbnormalAlertViewModel>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetDoctorAbnormalAlerts", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@FromDate", (object?)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)to ?? DBNull.Value);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new AbnormalAlertViewModel
                {
                    RequestItemID = reader.GetInt32(reader.GetOrdinal("RequestItemID")),
                    RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                    RequestNumber = reader.GetString(reader.GetOrdinal("RequestNumber")),
                    RequestDate = reader.GetDateTime(reader.GetOrdinal("RequestDate")),
                    PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                    PatientName = reader.GetString(reader.GetOrdinal("PatientName")),
                    TestName = reader.GetString(reader.GetOrdinal("TestName")),
                    ResultValue = reader.IsDBNull(reader.GetOrdinal("ResultValue")) ? null : reader.GetDecimal(reader.GetOrdinal("ResultValue")),
                    UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName")) ? null : reader.GetString(reader.GetOrdinal("UnitName")),
                    NormalRangeMin = reader.IsDBNull(reader.GetOrdinal("NormalRangeMin")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMin")),
                    NormalRangeMax = reader.IsDBNull(reader.GetOrdinal("NormalRangeMax")) ? null : reader.GetDecimal(reader.GetOrdinal("NormalRangeMax")),
                    ResultNotes = reader.IsDBNull(reader.GetOrdinal("ResultNotes")) ? null : reader.GetString(reader.GetOrdinal("ResultNotes")),
                    CompletionDateTime = reader.IsDBNull(reader.GetOrdinal("CompletionDateTime")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletionDateTime"))
                });
            }
            return list;
        }
    }
}
