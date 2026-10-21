using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
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

        private SqlConnection Open()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // ---- Identity -------------------------------------------------------
        // No sp_GetDoctorIdByUserId exists in the real database, so this is a
        // plain join (same "raw SQL where there's no matching proc" pattern
        // UserDataAccess.GetAllRoles() already uses).

        public (int DoctorID, string DoctorName, string Email)? GetDoctorByUserId(int userId)
        {
            using var conn = Open();
            using var cmd = new SqlCommand(@"
                SELECT d.DoctorID, (d.FirstName + ' ' + d.LastName) AS DoctorName, u.Email
                FROM Doctors d
                INNER JOIN Users u ON u.UserID = d.UserID
                WHERE d.UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
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


        public (int DoctorID, string FirstName, string LastName, string Email)? GetDoctorProfileByUserId(int userId)
        {
            using var conn = Open();
            using var cmd = new SqlCommand(@"
                SELECT d.DoctorID, d.FirstName, d.LastName, u.Email
                FROM Doctors d
                INNER JOIN Users u ON u.UserID = d.UserID
                WHERE d.UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (
                    reader.GetInt32(reader.GetOrdinal("DoctorID")),
                    reader.GetString(reader.GetOrdinal("FirstName")),
                    reader.GetString(reader.GetOrdinal("LastName")),
                    reader.GetString(reader.GetOrdinal("Email"))
                );
            }
            return null;
        }


        public DoctorProfileViewModel? GetDoctorOwnProfile(int userId)
        {
            using var conn = Open();
            using var cmd = new SqlCommand(@"
                SELECT d.DoctorID, d.FirstName, d.LastName, d.HPCSANumber, d.ContactNumber,
                       u.Email, u.CreatedDate
                FROM Doctors d
                INNER JOIN Users u ON u.UserID = d.UserID
                WHERE d.UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new DoctorProfileViewModel
            {
                DoctorID = Convert.ToInt32(r["DoctorID"]),
                FirstName = r["FirstName"] as string ?? "",
                LastName = r["LastName"] as string ?? "",
                HPCSANumber = r["HPCSANumber"] as string ?? "",
                ContactNumber = r["ContactNumber"] as string ?? "",
                Email = r["Email"] as string ?? "",
                RegistrationDate = r["CreatedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(r["CreatedDate"])
            };
        }

        /// <summary>
        /// Saves the fields a doctor may change on their own profile. E-mail and
        /// HPCSA number are left alone (the admin manages those via sp_UpdateDoctor).
        /// </summary>
        public void UpdateDoctorOwnProfile(int doctorId, string firstName, string lastName, string contactNumber)
        {
            using var conn = Open();
            using var cmd = new SqlCommand(@"
                UPDATE Doctors
                SET FirstName = @FirstName, LastName = @LastName, ContactNumber = @ContactNumber
                WHERE DoctorID = @DoctorID", conn);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@ContactNumber", contactNumber);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.ExecuteNonQuery();
        }

        // ---- 1. Manage Patient Records ---------------------------------------
        // No sp_SearchPatients / sp_CheckPatientIdNumberExists exist, so these
        // are plain SQL too.

        // A patient "belongs" to a doctor when the doctor registered them, has a test
        // request for them, or the patient has granted (and not revoked) consent to
        // that doctor. Shared by SearchPatients and DoctorCanAccessPatient.
        private const string PatientBelongsToDoctorSql = @"
                (p.RegisteredByDoctorID = @DoctorID
                 OR EXISTS (SELECT 1 FROM TestRequests tr
                            WHERE tr.PatientID = p.PatientID AND tr.DoctorID = @DoctorID)
                 OR EXISTS (SELECT 1 FROM PatientDoctorConsent pdc
                            WHERE pdc.PatientID = p.PatientID AND pdc.DoctorID = @DoctorID
                              AND pdc.ConsentGranted = 1 AND pdc.RevokedDate IS NULL))";

        /// <summary>Only the patients that belong to this doctor.</summary>
        public List<PatientRecordListItem> SearchPatients(string? searchTerm, int doctorId)
        {
            var list = new List<PatientRecordListItem>();
            using var conn = Open();
            using var cmd = new SqlCommand(@"
                SELECT p.PatientID, p.FirstName, p.LastName, p.IDNumber, p.DateOfBirth,
                       p.CellphoneNumber, u.Email
                FROM Patients p
                INNER JOIN Users u ON u.UserID = p.UserID
                WHERE " + PatientBelongsToDoctorSql + @"
                  AND (@SearchTerm IS NULL
                       OR p.FirstName LIKE '%' + @SearchTerm + '%'
                       OR p.LastName LIKE '%' + @SearchTerm + '%'
                       OR p.IDNumber LIKE '%' + @SearchTerm + '%'
                       OR u.Email LIKE '%' + @SearchTerm + '%')
                ORDER BY p.LastName, p.FirstName", conn);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
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

        /// <summary>True when this patient belongs to this doctor (see PatientBelongsToDoctorSql).</summary>
        public bool DoctorCanAccessPatient(int doctorId, int patientId)
        {
            using var conn = Open();
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*) FROM Patients p
                WHERE p.PatientID = @PatientID AND " + PatientBelongsToDoctorSql, conn);
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public bool PatientIdNumberExists(string idNumber)
        {
            using var conn = Open();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Patients WHERE IDNumber = @IDNumber", conn);
            cmd.Parameters.AddWithValue("@IDNumber", idNumber);
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt32(result) > 0;
        }


        public (string Result, int NewUserId, int NewPatientId) RegisterPatientByDoctor(
            string username, string email, string passwordHash,
            string firstName, string lastName, string idNumber,
            DateTime dateOfBirth, string cellphoneNumber, int registeredByDoctorId = 0)
        {
            using var conn = Open();
            using var cmd = new SqlCommand("sp_RegisterPatientByDoctor", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@IDNumber", idNumber);
            cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
            cmd.Parameters.AddWithValue("@CellphoneNumber", cellphoneNumber);

            var resultParam = new SqlParameter("@Result", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
            var newUserIdParam = new SqlParameter("@NewUserID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var newPatientIdParam = new SqlParameter("@NewPatientID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultParam);
            cmd.Parameters.Add(newUserIdParam);
            cmd.Parameters.Add(newPatientIdParam);

            cmd.ExecuteNonQuery();

            var result = resultParam.Value as string ?? "ERROR: Unknown";
            var newUserId = newUserIdParam.Value == DBNull.Value ? 0 : (int)newUserIdParam.Value;
            var newPatientId = newPatientIdParam.Value == DBNull.Value ? 0 : (int)newPatientIdParam.Value;

            // The stored procedure doesn't record which doctor registered the patient,
            // so do it here. This is what makes the patient "belong" to that doctor.
            if (result == "SUCCESS" && newPatientId > 0 && registeredByDoctorId > 0)
            {
                using var link = new SqlCommand(
                    "UPDATE Patients SET RegisteredByDoctorID = @DoctorID WHERE PatientID = @PatientID", conn);
                link.Parameters.AddWithValue("@DoctorID", registeredByDoctorId);
                link.Parameters.AddWithValue("@PatientID", newPatientId);
                link.ExecuteNonQuery();
            }

            return (result, newUserId, newPatientId);
        }

        // ---- 2. Create Test Requests -----------------------------------------
        // No sp_GetAllTestTypes exists, so this is a plain join
        // (TestTypes -> TestCategories / SampleTypes).

        public List<TestTypeOption> GetAllTestTypes()
        {
            var list = new List<TestTypeOption>();
            using var conn = Open();
            // TestTypes keeps its unit as plain text in the UnitofMeasurement column
            // (there is no UnitID on that table), so no join to UnitsOfMeasurement.
            using var cmd = new SqlCommand(@"
                SELECT tt.TestTypeID, tt.TestName, tc.CategoryName,
                       tt.SampleTypeID, st.SampleTypeName,
                       tt.UnitofMeasurement AS UnitName,
                       tt.NormalRangeMin, tt.NormalRangeMax
                FROM TestTypes tt
                INNER JOIN TestCategories tc ON tt.CategoryID = tc.CategoryID
                INNER JOIN SampleTypes st ON tt.SampleTypeID = st.SampleTypeID
                WHERE tt.IsActive = 1
                ORDER BY tc.CategoryName, tt.TestName", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TestTypeOption
                {
                    TestTypeID = reader.GetInt32(reader.GetOrdinal("TestTypeID")),
                    TestName = reader.GetString(reader.GetOrdinal("TestName")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    SampleTypeID = reader.GetInt32(reader.GetOrdinal("SampleTypeID")),
                    SampleTypeName = reader.GetString(reader.GetOrdinal("SampleTypeName")),
                    UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName")) ? "" : reader.GetString(reader.GetOrdinal("UnitName")),
                    NormalRangeMin = reader.IsDBNull(reader.GetOrdinal("NormalRangeMin")) ? 0 : reader.GetDecimal(reader.GetOrdinal("NormalRangeMin")),
                    NormalRangeMax = reader.IsDBNull(reader.GetOrdinal("NormalRangeMax")) ? 0 : reader.GetDecimal(reader.GetOrdinal("NormalRangeMax"))
                });
            }
            return list;
        }

        // Full SampleTypes lookup list for the barcode-row dropdown.
        public List<SampleTypeOption> GetAllSampleTypes()
        {
            var list = new List<SampleTypeOption>();
            using var conn = Open();
            using var cmd = new SqlCommand("SELECT SampleTypeID, SampleTypeName FROM SampleTypes ORDER BY SampleTypeName", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SampleTypeOption
                {
                    SampleTypeID = reader.GetInt32(0),
                    SampleTypeName = reader.GetString(1)
                });
            }
            return list;
        }

        /// <summary>
        /// Calls the real sp_GetRequiredSamplesForTestTypes so the UI can show
        /// the doctor which sample type(s) the selected tests need ("system
        /// must indicate required samples to doctor" in the spec).
        /// </summary>
        public List<SampleTypeOption> GetRequiredSamplesForTestTypes(List<int> testTypeIds)
        {
            var list = new List<SampleTypeOption>();
            if (testTypeIds == null || testTypeIds.Count == 0) return list;

            using var conn = Open();
            using var cmd = new SqlCommand("sp_GetRequiredSamplesForTestTypes", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@TestTypeIDsJson", JsonSerializer.Serialize(testTypeIds));
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SampleTypeOption
                {
                    SampleTypeID = reader.GetInt32(reader.GetOrdinal("SampleTypeID")),
                    SampleTypeName = reader.GetString(reader.GetOrdinal("SampleTypeName"))
                });
            }
            return list;
        }

        /// <summary>
        /// Calls the real sp_CreateTestRequest. It requires the test-type IDs
        /// and the barcode/sample-type pairs as JSON (it uses OPENJSON /
        /// JSON_VALUE internally) rather than the CSV strings this file used
        /// to build — that was based on a guessed signature before the real
        /// schema was available.
        /// </summary>
        public (int RequestID, string RequestNumber) CreateTestRequest(
            int patientId, int doctorId, string urgency, string? clinicalNotes,
            List<int> testTypeIds, List<SampleBarcodeInput> samples)
        {
            using var conn = Open();
            using var cmd = new SqlCommand("sp_CreateTestRequest", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PatientID", patientId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@Urgency", urgency);
            cmd.Parameters.AddWithValue("@ClinicalNotes", (object?)clinicalNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TestTypeIDsJson", JsonSerializer.Serialize(testTypeIds));
            cmd.Parameters.AddWithValue("@BarcodesJson", JsonSerializer.Serialize(samples));

            var resultParam = new SqlParameter("@Result", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
            var newRequestIdParam = new SqlParameter("@NewRequestID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultParam);
            cmd.Parameters.Add(newRequestIdParam);

            cmd.ExecuteNonQuery();

            var result = resultParam.Value as string ?? "ERROR: Unknown";
            if (result != "SUCCESS")
                throw new InvalidOperationException(result);

            var requestId = (int)newRequestIdParam.Value;

            // sp_CreateTestRequest doesn't hand back the generated
            // RequestNumber, so read the row it just inserted.
            using var lookup = new SqlCommand("SELECT RequestNumber FROM TestRequests WHERE RequestID = @RequestID", conn);
            lookup.Parameters.AddWithValue("@RequestID", requestId);
            var requestNumber = (string)lookup.ExecuteScalar();

            return (requestId, requestNumber);
        }

        // ---- 3/4. Track status + View results --------------------------------

        /// <summary>
        /// sp_GetDoctorTestRequestsByDateRange requires BOTH a start and end
        /// date (unlike the sp_GetDoctorTestRequests this file used to call,
        /// which doesn't exist for real). When the caller doesn't need a
        /// specific range (e.g. the plain "TestRequests" history list), this
        /// defaults to "everything up to today".
        /// </summary>
        public List<DoctorTestRequestSummary> GetDoctorTestRequests(int doctorId, DateTime? from = null, DateTime? to = null)
        {
            var list = new List<DoctorTestRequestSummary>();
            using var conn = Open();
            using var cmd = new SqlCommand("sp_GetDoctorTestRequestsByDateRange", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@StartDate", from ?? new DateTime(2000, 1, 1));
            cmd.Parameters.AddWithValue("@EndDate", to ?? DateTime.Today);
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
                    ReleasedDate = reader.IsDBNull(reader.GetOrdinal("ReleasedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReleasedDate")),
                    PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                    PatientName = reader.GetString(reader.GetOrdinal("FirstName")) + " " + reader.GetString(reader.GetOrdinal("LastName"))
                });
            }
            return list;
        }

        /// <summary>
        /// Fetches one request's full detail for this doctor. Ownership is
        /// enforced here in C# (only this doctor's own requests, from
        /// GetDoctorTestRequests, are searched) rather than relying on a
        /// stored procedure to check it, since sp_GetTestRequestItems takes
        /// only a RequestID with no doctor check at all.
        /// </summary>
        public DoctorTestRequestSummary? GetTestRequestDetail(int doctorId, int requestId)
        {
            var request = GetDoctorTestRequests(doctorId, new DateTime(2000, 1, 1), DateTime.Today.AddDays(1))
                .Find(r => r.RequestID == requestId);
            if (request == null) return null;

            // Patient email/details aren't returned by
            // sp_GetDoctorTestRequestsByDateRange, so fill them in here.
            using (var conn = Open())
            using (var cmd = new SqlCommand(@"
                SELECT u.Email FROM Patients p
                INNER JOIN Users u ON u.UserID = p.UserID
                WHERE p.PatientID = @PatientID", conn))
            {
                cmd.Parameters.AddWithValue("@PatientID", request.PatientID);
                var email = cmd.ExecuteScalar();
                request.PatientEmail = email as string ?? "";
            }

            // Same story for ReleaseNotes/CancellationReason/CancelledBy —
            // sp_GetDoctorTestRequestsByDateRange doesn't return them, so
            // pull them straight off TestRequests for this one request.
            using (var conn = Open())
            using (var cmd = new SqlCommand(@"
                SELECT tr.ReleaseNotes, tr.CancellationReason,
                       (cu.FirstName + ' ' + cu.LastName) AS CancelledByName
                FROM TestRequests tr
                LEFT JOIN Doctors cd ON cd.UserID = tr.CancelledBy
                LEFT JOIN LabTechnicians clt ON clt.UserID = tr.CancelledBy
                CROSS APPLY (SELECT ISNULL(cd.FirstName, clt.FirstName) AS FirstName,
                                    ISNULL(cd.LastName, clt.LastName) AS LastName) cu
                WHERE tr.RequestID = @RequestID", conn))
            {
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    request.ReleaseNotes = reader.IsDBNull(reader.GetOrdinal("ReleaseNotes")) ? null : reader.GetString(reader.GetOrdinal("ReleaseNotes"));
                    request.CancelReason = reader.IsDBNull(reader.GetOrdinal("CancellationReason")) ? null : reader.GetString(reader.GetOrdinal("CancellationReason"));
                    request.CancelledBy = reader.IsDBNull(reader.GetOrdinal("CancelledByName")) ? null : reader.GetString(reader.GetOrdinal("CancelledByName"));
                }
            }

            using (var conn = Open())
            using (var cmd = new SqlCommand("sp_GetTestRequestItems", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    request.Items.Add(new TestRequestItemDetail
                    {
                        RequestItemID = reader.GetInt32(reader.GetOrdinal("RequestItemID")),
                        RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                        TestName = reader.GetString(reader.GetOrdinal("TestName")),
                        CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? "" : reader.GetString(reader.GetOrdinal("CategoryName")),
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
            }

            using (var conn = Open())
            using (var cmd = new SqlCommand(@"
                SELECT sb.BarcodeID, sb.BarcodeNumber, st.SampleTypeName, sb.CollectionDate, sb.ReceivedDate
                FROM SampleBarcodes sb
                INNER JOIN SampleTypes st ON sb.SampleTypeID = st.SampleTypeID
                WHERE sb.RequestID = @RequestID", conn))
            {
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    request.Samples.Add(new TestRequestSample
                    {
                        BarcodeID = reader.GetInt32(reader.GetOrdinal("BarcodeID")),
                        BarcodeNumber = reader.GetString(reader.GetOrdinal("BarcodeNumber")),
                        SampleTypeName = reader.GetString(reader.GetOrdinal("SampleTypeName")),
                        CollectionDate = reader.GetDateTime(reader.GetOrdinal("CollectionDate")),
                        ReceivedDate = reader.IsDBNull(reader.GetOrdinal("ReceivedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReceivedDate"))
                    });
                }
            }

            return request;
        }


        public (bool Success, string Message, string? NotifyDoctorEmail, string? NotifyDoctorName) CancelTestRequest(
            int requestId, string reason, int cancelledByUserId, string cancelledByRole)
        {
            using var conn = Open();
            using var cmd = new SqlCommand("sp_CancelTestRequest", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            cmd.Parameters.AddWithValue("@CancellationReason", reason);
            cmd.Parameters.AddWithValue("@CancelledByUserID", cancelledByUserId);
            cmd.Parameters.AddWithValue("@CancelledByRole", cancelledByRole);

            var resultParam = new SqlParameter("@Result", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
            var notifyEmailParam = new SqlParameter("@NotifyDoctorEmail", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };
            var notifyNameParam = new SqlParameter("@NotifyDoctorName", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultParam);
            cmd.Parameters.Add(notifyEmailParam);
            cmd.Parameters.Add(notifyNameParam);

            cmd.ExecuteNonQuery();

            var result = resultParam.Value as string ?? "ERROR: Unknown";
            var notifyEmail = notifyEmailParam.Value as string;
            var notifyName = notifyNameParam.Value as string;
            return (result == "SUCCESS", result, notifyEmail, notifyName);
        }


        public (bool Success, string Message) ReleaseResults(int requestId, int doctorId, string releaseNotes)
        {
            using var conn = Open();
            using var cmd = new SqlCommand("sp_ReleaseTestResults", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@ReleaseNote", (object?)releaseNotes ?? DBNull.Value);

            var resultParam = new SqlParameter("@Result", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultParam);

            cmd.ExecuteNonQuery();

            var result = resultParam.Value as string ?? "ERROR: Unknown";
            return (result == "SUCCESS", result);
        }


        public List<AbnormalAlertViewModel> GetAbnormalAlerts(int doctorId, DateTime? from, DateTime? to)
        {
            var list = new List<AbnormalAlertViewModel>();
            using var conn = Open();
            using var cmd = new SqlCommand("sp_GetDoctorAlerts", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DoctorID", doctorId);
            cmd.Parameters.AddWithValue("@FromDate", (object?)from ?? DBNull.Value);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var completed = reader.IsDBNull(reader.GetOrdinal("CompletionDateTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CompletionDateTime"));
                if (to.HasValue && completed.HasValue && completed.Value.Date > to.Value.Date) continue;

                list.Add(new AbnormalAlertViewModel
                {
                    RequestItemID = reader.GetInt32(reader.GetOrdinal("RequestItemID")),
                    RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                    RequestNumber = reader.GetString(reader.GetOrdinal("RequestNumber")),
                    PatientID = reader.GetInt32(reader.GetOrdinal("PatientID")),
                    PatientName = reader.GetString(reader.GetOrdinal("FirstName")) + " " + reader.GetString(reader.GetOrdinal("LastName")),
                    TestName = reader.GetString(reader.GetOrdinal("TestName")),
                    ResultValue = reader.IsDBNull(reader.GetOrdinal("ResultValue")) ? null : reader.GetDecimal(reader.GetOrdinal("ResultValue")),
                    UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName")) ? null : reader.GetString(reader.GetOrdinal("UnitName")),
                    ResultNotes = reader.IsDBNull(reader.GetOrdinal("ResultNotes")) ? null : reader.GetString(reader.GetOrdinal("ResultNotes")),
                    CompletionDateTime = completed,
                    RequestDate = completed ?? DateTime.Today
                });
            }

            // sp_GetDoctorAlerts doesn't return NormalRangeMin/Max, so fill
            // those in with one follow-up query keyed by RequestItemID.
            if (list.Count > 0)
            {
                var ids = string.Join(",", list.Select(a => a.RequestItemID));
                using var conn2 = Open();
                using var cmd2 = new SqlCommand($@"
                    SELECT tri.RequestItemID, tt.NormalRangeMin, tt.NormalRangeMax
                    FROM TestRequestItems tri
                    INNER JOIN TestTypes tt ON tri.TestTypeID = tt.TestTypeID
                    WHERE tri.RequestItemID IN ({ids})", conn2);
                using var reader2 = cmd2.ExecuteReader();
                var ranges = new Dictionary<int, (decimal Min, decimal Max)>();
                while (reader2.Read())
                {
                    ranges[reader2.GetInt32(0)] = (reader2.GetDecimal(1), reader2.GetDecimal(2));
                }
                foreach (var alert in list)
                {
                    if (ranges.TryGetValue(alert.RequestItemID, out var range))
                    {
                        alert.NormalRangeMin = range.Min;
                        alert.NormalRangeMax = range.Max;
                    }
                }
            }

            return list;
        }
    }
}