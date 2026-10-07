using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Doctor.Models;

namespace Doctor.DataAccess
{
    /// <summary>
    /// Data access for the Doctor module (patient records, test requests,
    /// results, alerts, reports).
    ///
    /// This calls the REAL, already-existing stored procedures from the
    /// team's NMB_HaematologyLab database (see the SSMS schema script the
    /// user attached) wherever one exists for the job, and falls back to
    /// plain parameterised SQL — the same convention already used in
    /// Patient.DataAccess.UserDataAccess (e.g. its GetAllRoles()) — for the
    /// handful of lookups that have no matching stored procedure (a
    /// doctor-by-user-id lookup, a joined test-type list, a sample-type
    /// list). No new stored procedures or schema changes are introduced
    /// here, to avoid any further drift from the shared database.
    ///
    /// KNOWN EXISTING-DATABASE BUG (not fixed here — see CancelTestRequest
    /// below): sp_CancelTestRequest sets TestRequestItems.ItemStatus to
    /// 'Cancelled' for every non-Verified item, but the ItemStatus column's
    /// own CHECK constraint does not allow 'Cancelled' as a value. That
    /// will make a real cancel throw a constraint-violation error from SQL
    /// Server. This needs a real fix in the shared database (either the
    /// procedure or the constraint) — flag it to the team rather than
    /// silently altering a constraint everyone else's code also relies on.
    /// </summary>
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

        /// <summary>
        /// Same join as GetDoctorByUserId, but with FirstName/LastName kept
        /// separate (rather than concatenated into DoctorName) for the
        /// profile page's editable name fields.
        ///
        /// NOTE: only DoctorID/FirstName/LastName/Email are confirmed real
        /// Doctors-table columns. LicenseNumber, Specialization, Department,
        /// PracticeAddress, HomeAddress, CellphoneNumber and DateOfBirth are
        /// NOT populated here — nobody has confirmed those columns exist on
        /// the real Doctors table yet, so the profile page leaves them blank
        /// rather than showing invented placeholder data.
        /// </summary>
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

        // ---- 1. Manage Patient Records ---------------------------------------
        // No sp_SearchPatients / sp_CheckPatientIdNumberExists exist, so these
        // are plain SQL too.

        public List<PatientRecordListItem> SearchPatients(string? searchTerm)
        {
            var list = new List<PatientRecordListItem>();
            using var conn = Open();
            using var cmd = new SqlCommand(@"
                SELECT p.PatientID, p.FirstName, p.LastName, p.IDNumber, p.DateOfBirth,
                       p.CellphoneNumber, u.Email
                FROM Patients p
                INNER JOIN Users u ON u.UserID = p.UserID
                WHERE (@SearchTerm IS NULL
                       OR p.FirstName LIKE '%' + @SearchTerm + '%'
                       OR p.LastName LIKE '%' + @SearchTerm + '%'
                       OR p.IDNumber LIKE '%' + @SearchTerm + '%'
                       OR u.Email LIKE '%' + @SearchTerm + '%')
                ORDER BY p.LastName, p.FirstName", conn);
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

        public bool PatientIdNumberExists(string idNumber)
        {
            using var conn = Open();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Patients WHERE IDNumber = @IDNumber", conn);
            cmd.Parameters.AddWithValue("@IDNumber", idNumber);
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Calls the real sp_RegisterPatientByDoctor — NOT the generic
        /// sp_RegisterUser. This one is the correct proc for this feature:
        /// it sets MustChangePassword = 1 and IsEmailVerified = 0, matching
        /// the spec's "must change password at first login" requirement
        /// (sp_RegisterUser used elsewhere in the app always sets
        /// MustChangePassword = 0, which is right for public self sign-up
        /// but wrong here).
        /// </summary>
        public (string Result, int NewUserId, int NewPatientId) RegisterPatientByDoctor(
            string username, string email, string passwordHash,
            string firstName, string lastName, string idNumber,
            DateTime dateOfBirth, string cellphoneNumber)
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
            return (result, newUserId, newPatientId);
        }

        // ---- 2. Create Test Requests -----------------------------------------
        // No sp_GetAllTestTypes exists, so this is a plain join
        // (TestTypes -> TestCategories / SampleTypes / UnitsOfMeasurement).

        public List<TestTypeOption> GetAllTestTypes()
        {
            var list = new List<TestTypeOption>();
            using var conn = Open();
            using var cmd = new SqlCommand(@"
                SELECT tt.TestTypeID, tt.TestName, tc.CategoryName,
                       tt.SampleTypeID, st.SampleTypeName,
                       uom.UnitName, tt.NormalRangeMin, tt.NormalRangeMax
                FROM TestTypes tt
                INNER JOIN TestCategories tc ON tt.CategoryID = tc.CategoryID
                INNER JOIN SampleTypes st ON tt.SampleTypeID = st.SampleTypeID
                INNER JOIN UnitsOfMeasurement uom ON tt.UnitID = uom.UnitID
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
                    UnitName = reader.GetString(reader.GetOrdinal("UnitName")),
                    NormalRangeMin = reader.GetDecimal(reader.GetOrdinal("NormalRangeMin")),
                    NormalRangeMax = reader.GetDecimal(reader.GetOrdinal("NormalRangeMax"))
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

        /// <summary>
        /// Calls the real sp_CancelTestRequest(@RequestID, @CancellationReason,
        /// @CancelledByUserID, @CancelledByRole, @Result OUTPUT,
        /// @NotifyDoctorEmail OUTPUT, @NotifyDoctorName OUTPUT).
        ///
        /// NOTE: as written in the real database, this procedure will throw a
        /// CHECK-constraint violation the moment it tries to set a
        /// non-Verified item's ItemStatus to 'Cancelled' (see the class
        /// remarks above) — that surfaces here as a SqlException, not a
        /// friendly @Result string, since the constraint violation happens
        /// inside the same transaction the proc's own TRY/CATCH wraps... but
        /// a CHECK constraint violation IS caught by that CATCH block (it's a
        /// regular T-SQL error), so in practice @Result will come back as
        /// "ERROR: The UPDATE statement conflicted with the CHECK
        /// constraint...". Callers must treat any @Result that isn't exactly
        /// "SUCCESS" as a failure and show it to the doctor rather than
        /// assuming the cancel went through.
        /// </summary>
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

        /// <summary>
        /// Calls the real sp_ReleaseTestResults(@RequestID, @DoctorID,
        /// @ReleaseNote, @Result OUTPUT). The procedure itself enforces two
        /// preconditions: only the requesting doctor may release, and every
        /// item on the request must already be 'Verified' — it throws
        /// otherwise, which comes back here as an @Result starting with
        /// "ERROR:". Callers should check every item's ItemStatus themselves
        /// first (see TestItemStatus.Verified) so the doctor gets a clear
        /// message before even trying, rather than only finding out from a
        /// raw SQL error message.
        /// </summary>
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

        // ---- 5. Alerts --------------------------------------------------------

        /// <summary>
        /// sp_GetDoctorAlerts only takes @FromDate (defaults to "5 days ago"
        /// inside the proc itself if NULL is passed) — there is no @ToDate
        /// parameter in the real procedure, so an upper bound is applied
        /// here in C# after the fact.
        /// </summary>
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