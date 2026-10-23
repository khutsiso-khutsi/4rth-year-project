using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using _4th_year_set_up.Models;

namespace _4th_year_set_up.DataAccess
{
    // Data access for the Technician sub-system (SQL Server, Dapper). Same shape as AdminDataAccess / DoctorDataAccess:
    // one class, connection string passed to the constructor, one method per action.
    // Every query and every write is a STORED PROCEDURE (sp_Tech_*, created by Database/Step2_Technician_StoredProcedures.sql).
    // There is no SQL text in this file.
    //
    // Tables used (existing):  TestRequests, TestRequestItems, SampleBarcodes, SampleTypes, TestTypes, Patients, Doctors,
    //                          PatientMedicalConditions/MedicalConditions, PatientAllergies/Allergies,
    //                          PatientMedications/Medications, TechnicianTestTypes, TestResultReviews
    // Tables used (ours):      TestResultValues, TestEvents, plus the new columns added by Technician_DB_Setup.sql
    //
    // Your group's words are kept in ItemStatus / RequestStatus; our finer stage lives in TestRequestItems.Stage.
    // Reading returns the same LabRequest -> SampleTube -> TestItem objects the screens already use.
    // Writing is one method per action, each in a single transaction; false means somebody else got there first
    // (two technicians picking the same test, a result already verified, and so on).
    // Technician IDs travel as strings ("1") because the models use strings; they are LabTechnicians.TechnicianID.
    //
    // Register one per web request (Scoped) so a page loads its data once.
    public partial class TechnicianDataAccess
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private readonly string _cs;
        private List<LabRequest> _cache;

        private readonly bool _sample;

        // useSampleData = true: OFFLINE DEMO. The screens use the fictional data in TechnicianSampleData and the database
        // is never contacted (so no connection string is needed). Switched on with "Technician": { "UseSampleData": true }.
        public TechnicianDataAccess(string connectionString, bool useSampleData = false)
        {
            _sample = useSampleData;
            if (!_sample)
                _cs = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public bool IsSampleMode => _sample;

        private SqlConnection Open()
        {
            var c = new SqlConnection(_cs);
            try { c.Open(); }
            catch (SqlException ex) when (ex.Number == 18456)
            {
                // Say which login was used (never the password) so it can be compared with the one that works elsewhere.
                var b = new SqlConnectionStringBuilder(_cs);
                throw new InvalidOperationException(
                    $"SQL Server rejected the login '{b.UserID}' on {b.DataSource} / {b.InitialCatalog}. " +
                    "Check the 'conn' value in appsettings.json (and appsettings.Development.json / User Secrets) " +
                    "and that the app was fully restarted after changing it.", ex);
            }
            return c;
        }

        // =====================================================================================
        // Reading
        // =====================================================================================

        public List<LabRequest> GetRequests() => _sample ? Sample() : (_cache ??= LoadChecked());

        private static List<LabRequest> Sample()
        {
            TechnicianNames.Set(TechnicianSampleData.Names);
            return TechnicianSampleData.Graph;
        }

        // If the database changes or the stored procedures are missing, say so in plain words
        // instead of a bare "Invalid column name 'Stage'" / "Could not find stored procedure".
        private List<LabRequest> LoadChecked()
        {
            try { return Load(); }
            catch (SqlException ex) when (ex.Number is 207 or 208 or 4104 or 2812)
            {
                throw new InvalidOperationException(
                    "The technician database changes are missing in the database this app is connected to. " +
                    "Run Database/Technician_DB_Setup.sql and then Database/Step2_Technician_StoredProcedures.sql on it. " +
                    "(SQL said: " + ex.Message + ")", ex);
            }
        }

        public List<TestItem> GetTests() => GetRequests().SelectMany(r => r.Tests).ToList();

        public LabRequest GetRequest(string reqNum) => GetRequests().FirstOrDefault(r => r.ReqNum == reqNum);

        public TestItem GetTest(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var all = GetTests();
            var byId = all.FirstOrDefault(t => t.TestId == id);
            if (byId != null) return byId;
            var sameRequest = all.Where(t => t.ReqNum == id).ToList();      // a request number works when it has one test
            return sameRequest.Count == 1 ? sameRequest[0] : null;
        }

        public HashSet<string> GetAssignedTestNames(string technicianId)
        {
            if (_sample) return TechnicianSampleData.TestNames();
            var tech = Tech(technicianId);
            if (tech == null) return new HashSet<string>();
            using var db = Open();
            var names = db.Query<string>("dbo.sp_Tech_GetAssignedTestNames", new { TechnicianID = tech },
                                         commandType: CommandType.StoredProcedure);
            return new HashSet<string>(names);
        }

        private List<LabRequest> Load()
        {
            List<RequestRow> reqRows; List<TubeRow> tubeRows; List<ItemRow> itemRows; List<ValueRow> valueRows;
            List<ReviewRow> reviewRows; List<EventRow> eventRows; List<ConditionRow> condRows;
            List<AllergyRow> allergyRows; List<MedRow> medRows; List<TechRow> techRows;

            using (var db = Open())
            using (var g = db.QueryMultiple("dbo.sp_Tech_LoadAll", commandType: CommandType.StoredProcedure))
            {
                reqRows = g.Read<RequestRow>().ToList();
                tubeRows = g.Read<TubeRow>().ToList();
                itemRows = g.Read<ItemRow>().ToList();
                valueRows = g.Read<ValueRow>().ToList();
                reviewRows = g.Read<ReviewRow>().ToList();
                eventRows = g.Read<EventRow>().ToList();
                condRows = g.Read<ConditionRow>().ToList();
                allergyRows = g.Read<AllergyRow>().ToList();
                medRows = g.Read<MedRow>().ToList();
                techRows = g.Read<TechRow>().ToList();
            }

            TechnicianNames.Set(techRows.ToDictionary(t => t.TechnicianID.ToString(), t => $"{t.FirstName} {t.LastName}"));

            var now = DateTime.Now;
            var me = CurrentUser.Id;

            var conds = condRows.ToLookup(x => x.PatientID, x => x.ConditionName);
            var allergies = allergyRows.ToLookup(x => x.PatientID);
            var meds = medRows.ToLookup(x => x.PatientID);
            var valuesByItem = valueRows.ToLookup(v => v.RequestItemID);
            var reviewsByItem = reviewRows.ToLookup(v => v.RequestItemID);
            var tubeRowsByReq = tubeRows.ToLookup(t => t.RequestID);

            var patients = new Dictionary<int, PatientInfo>();
            var requests = new List<LabRequest>();
            var reqById = new Dictionary<int, LabRequest>();
            var tubeById = new Dictionary<int, SampleTube>();
            var itemById = new Dictionary<int, TestItem>();
            var rowById = new Dictionary<int, ItemRow>();

            // ---- requests, patients, tubes
            foreach (var rr in reqRows)
            {
                if (!patients.TryGetValue(rr.PatientID, out var patient))
                {
                    patient = BuildPatient(rr, conds[rr.PatientID], allergies[rr.PatientID], meds[rr.PatientID]);
                    patients[rr.PatientID] = patient;
                }

                var req = new LabRequest
                {
                    RequestId = rr.RequestID,
                    ReqNum = rr.RequestNumber,
                    Patient = patient,
                    RequestingDoctor = rr.DoctorLast == null ? "Not recorded" : $"Dr {rr.DoctorFirst} {rr.DoctorLast}",
                    Ward = string.IsNullOrWhiteSpace(rr.Ward) ? "Not recorded" : rr.Ward,
                    ClinicalNotes = rr.ClinicalNotes,
                    IsUrgent = rr.Urgency == "Urgent" || rr.Urgency == "STAT",
                    Priority = string.IsNullOrWhiteSpace(rr.Urgency) ? "Routine" : rr.Urgency,
                    RequestedAt = rr.RequestDate,
                    ReleasedBy = rr.ReleasedBy?.ToString(),
                    ReleasedAt = rr.ReleasedDate ?? (rr.RequestStatus == "Released" ? rr.RequestDate : (DateTime?)null)
                };
                requests.Add(req);
                reqById[req.RequestId] = req;

                foreach (var tr in tubeRowsByReq[rr.RequestID])
                {
                    var status = tr.ReceivedDate == null ? TubeStatus.NotReceived
                               : tr.VerificationStatus == "Rejected" ? TubeStatus.Rejected
                               : tr.VerificationStatus == "Verified" ? TubeStatus.Verified
                               : TubeStatus.AwaitingVerification;
                    var tube = new SampleTube
                    {
                        BarcodeId = tr.BarcodeID,
                        TubeId = tr.BarcodeNumber,
                        Type = TubeType(tr.SampleTypeName),
                        SampleType = tr.SampleTypeName,
                        Status = status,
                        Condition = tr.SampleCondition ?? (status == TubeStatus.Verified ? "Acceptable" : "Not yet verified"),
                        CollectedAt = tr.CollectionDate,
                        ReceivedAt = tr.ReceivedDate ?? DateTime.MinValue,
                        VerifiedBy = tr.VerifiedBy?.ToString(),
                        VerifiedAt = tr.VerifiedDate,
                        RejectReason = tr.RejectReason,
                        Request = req
                    };
                    req.Tubes.Add(tube);
                    tubeById[tube.BarcodeId] = tube;
                }
            }

            // ---- tests
            var placeholders = new Dictionary<int, SampleTube>();      // for a test that has no tube on record
            foreach (var ir in itemRows)
            {
                if (!reqById.TryGetValue(ir.RequestID, out var req)) continue;

                SampleTube tube = null;
                if (ir.BarcodeID.HasValue) tubeById.TryGetValue(ir.BarcodeID.Value, out tube);
                if (tube == null)
                {
                    if (!placeholders.TryGetValue(req.RequestId, out tube))
                    {
                        tube = new SampleTube
                        {
                            TubeId = req.ReqNum + "-?", Type = "No tube on record", SampleType = "Unknown",
                            Status = TubeStatus.NotReceived, Request = req, CollectedAt = req.Tubes.FirstOrDefault()?.CollectedAt ?? DateTime.MinValue
                        };
                        placeholders[req.RequestId] = tube;
                    }
                }

                var item = new TestItem
                {
                    TestId = ir.RequestItemID.ToString(),
                    ItemId = ir.RequestItemID,
                    TestTypeId = ir.TestTypeID,
                    Request = req,
                    Sample = tube,
                    Test = ir.TestName,
                    Stage = ToStage(ir, tube),
                    AssignedToMe = ir.AssignedTo.HasValue && ir.AssignedTo.Value.ToString() == me,
                    AssignedTo = ir.AssignedTo?.ToString(),
                    TargetMinutes = ir.TurnaroundTimeMinutes,
                    PreviousResult = ir.PrevValue.HasValue
                        ? $"{ir.TestName} {ir.PrevValue.Value.ToString("0.##", Inv)} {ir.UnitofMeasurement} ({ir.PrevDate:d MMM yyyy})".Replace("  ", " ")
                        : "None on record",
                    CapturedBy = ir.CompletionDateTime.HasValue ? ir.AssignedTo?.ToString() : null,
                    CapturedAt = ir.CompletionDateTime,
                    VerifiedBy = ir.VerifiedBy?.ToString(),
                    VerifiedAt = ir.VerificationDateTime,
                    CaptureNote = ir.ResultNotes
                };

                // ---- returned attempts: the n-th "Returned" review belongs to the n-th capture
                var returns = reviewsByItem[ir.RequestItemID].Where(x => x.Action == "Returned").ToList();
                var rows = valuesByItem[ir.RequestItemID].ToList();
                for (var k = 0; k < returns.Count; k++)
                {
                    var attemptRows = rows.Where(v => v.Attempt == k + 1).ToList();
                    item.History.Add(new ReturnedAttempt
                    {
                        Note = returns[k].ReviewNotes,
                        CapturedBy = attemptRows.FirstOrDefault()?.CapturedBy?.ToString(),
                        CapturedAt = attemptRows.FirstOrDefault()?.CapturedDate,
                        Results = attemptRows.Select(ToLine).ToList()
                    });
                }

                // ---- current results: this attempt's rows; a returned test shows the returned values until re-entered
                var current = rows.Where(v => v.Attempt == returns.Count + 1).ToList();
                if (current.Count == 0 && item.Stage == TestStage.ToBeReviewed)
                    current = rows.Where(v => v.Attempt == returns.Count).ToList();
                item.Results = current.Count > 0 ? current.Select(ToLine).ToList() : SingleResult(ir);
                if (item.Stage == TestStage.ToBeReviewed && returns.Count > 0)
                    item.ReviewNote = returns[^1].ReviewNotes;

                SetTat(item, ir, now);

                req.Tests.Add(item);
                tube.Tests.Add(item);
                itemById[item.ItemId] = item;
                rowById[item.ItemId] = ir;
            }

            // ---- stored history (audit trail)
            foreach (var er in eventRows)
            {
                var scope = Enum.TryParse<EventScope>(er.Scope, true, out var sc) ? sc : EventScope.Test;
                var tone = Enum.TryParse<EventTone>(er.Tone, true, out var tn) ? tn : EventTone.Neutral;
                List<ValueChange> changes = null;
                if (!string.IsNullOrWhiteSpace(er.Changes))
                {
                    try { changes = JsonSerializer.Deserialize<List<ValueChange>>(er.Changes, Json); }
                    catch (JsonException) { changes = null; }
                }
                var ev = TestEvent.Create(er.Action, er.PerformedBy?.ToString(), scope, tone, er.Detail, er.Attempt, er.EventDate, changes);

                switch (scope)
                {
                    case EventScope.Request:
                        if (reqById.TryGetValue(er.RequestID, out var r1)) r1.Events.Add(ev);
                        break;
                    case EventScope.Sample:
                        if (er.BarcodeID.HasValue && tubeById.TryGetValue(er.BarcodeID.Value, out var t1)) t1.Events.Add(ev);
                        break;
                    default:
                        if (er.RequestItemID.HasValue && itemById.TryGetValue(er.RequestItemID.Value, out var i1)) i1.Events.Add(ev);
                        break;
                }
            }

            // ---- rows that were created by other parts of the system have no stored history yet: build a simple one from their dates
            foreach (var req in requests)
            {
                if (req.Events.Count == 0)
                {
                    var rr = reqRows.First(x => x.RequestID == req.RequestId);
                    req.Log("Request submitted", null, at: rr.RequestDate);
                    if (req.IsReleased) req.Log("Results released", req.ReleasedBy, EventTone.Good, at: req.ReleasedAt);
                }
                foreach (var tube in req.Tubes)
                {
                    if (tube.Events.Count > 0 || tube.Status == TubeStatus.NotReceived) continue;
                    tube.Log("Sample received", null, detail: $"{tube.Type} · {tube.SampleType}", at: tube.ReceivedAt);
                    if (tube.Status == TubeStatus.Verified) tube.Log("Sample verified", tube.VerifiedBy, EventTone.Good, at: tube.VerifiedAt);
                    if (tube.Status == TubeStatus.Rejected) tube.Log("Sample rejected", tube.VerifiedBy, EventTone.Bad, tube.RejectReason, tube.VerifiedAt);
                }
            }
            foreach (var item in itemById.Values)
            {
                if (item.Events.Count > 0) continue;
                var ir = rowById[item.ItemId];
                if (ir.AssignedTo.HasValue && ir.StartDateTime.HasValue)
                    item.Log("Test picked up", ir.AssignedTo.Value.ToString(), at: ir.StartDateTime);
                if (ir.CompletionDateTime.HasValue)
                    item.Log("Result captured", item.CapturedBy, detail: TestItem.Summarise(item.Results), attempt: 1, at: ir.CompletionDateTime);
                if (item.Stage == TestStage.Verified && ir.VerificationDateTime.HasValue)
                    item.Log("Result verified", item.VerifiedBy, tone: EventTone.Good, attempt: item.Attempt, at: ir.VerificationDateTime);
            }

            return requests;
        }

        // =====================================================================================
        // Writing. Each action is one stored procedure that runs in its own transaction.
        // false = the state had already changed (somebody else got there first).
        // =====================================================================================

        public bool ReceiveSample(SampleTube tube, string by, TestEvent ev)
            => _sample ? TechnicianSampleData.Receive(tube) : Run("dbo.sp_Tech_ReceiveSample", new
            {
                BarcodeID = tube.BarcodeId,
                RequestID = tube.Request.RequestId,
                By = Tech(by),
                EventsJson = EventsJson(new[] { ev })
            });

        public bool AcceptSample(SampleTube tube, string by, TestEvent ev)
            => _sample ? TechnicianSampleData.Accept(tube, by) : Run("dbo.sp_Tech_AcceptSample", new
            {
                BarcodeID = tube.BarcodeId,
                RequestID = tube.Request.RequestId,
                By = Tech(by),
                EventsJson = EventsJson(new[] { ev })
            });

        public bool RejectSample(SampleTube tube, string by, string condition, string reason, TestEvent ev)
            => _sample ? TechnicianSampleData.Reject(tube, by, condition, reason) : Run("dbo.sp_Tech_RejectSample", new
            {
                BarcodeID = tube.BarcodeId,
                RequestID = tube.Request.RequestId,
                By = Tech(by),
                Condition = Trunc(condition, 50),
                Reason = Trunc(reason, 500),
                EventsJson = EventsJson(new[] { ev })
            });

        // One atomic UPDATE inside the procedure: if two technicians click at once, only one of them wins.
        public bool PickUpTest(TestItem item, string by, TestEvent ev)
            => _sample ? TechnicianSampleData.PickUp(item, by) : Run("dbo.sp_Tech_PickUpTest", new
            {
                RequestItemID = item.ItemId,
                RequestID = item.Request.RequestId,
                BarcodeID = BarcodeOf(item),
                By = Tech(by),
                EventsJson = EventsJson(new[] { ev })
            });

        public bool ReturnToPool(TestItem item, string by, TestEvent ev)
            => _sample ? TechnicianSampleData.ReturnToPool(item, by) : Run("dbo.sp_Tech_ReturnToPool", new
            {
                RequestItemID = item.ItemId,
                RequestID = item.Request.RequestId,
                BarcodeID = BarcodeOf(item),
                By = Tech(by),
                EventsJson = EventsJson(new[] { ev })
            });

        // A draft is not logged. It saves this attempt's values and counts as starting the test.
        public bool SaveDraft(TestItem item, string by, int attempt, string captureNote)
            => _sample ? TechnicianSampleData.SaveDraft(item) : Run("dbo.sp_Tech_SaveDraft", new
            {
                RequestItemID = item.ItemId,
                By = Tech(by),
                Attempt = attempt,
                Note = Trunc(captureNote, 1000),
                ValuesJson = ValuesJson(item.Results)
            });

        public bool SubmitResults(TestItem item, string by, int attempt, string captureNote, IEnumerable<TestEvent> events)
        {
            if (_sample) return TechnicianSampleData.Submit(item, by);
            decimal? primary = null;
            foreach (var l in item.Results)
            {
                if (TryNumber(l.Value, out var v)) { primary = v; break; }
            }
            return Run("dbo.sp_Tech_SubmitResults", new
            {
                RequestItemID = item.ItemId,
                RequestID = item.Request.RequestId,
                BarcodeID = BarcodeOf(item),
                By = Tech(by),
                Attempt = attempt,
                Note = Trunc(captureNote, 1000),
                Primary = primary,
                Abnormal = item.Results.Any(l => l.IsAbnormal),
                ValuesJson = ValuesJson(item.Results),
                EventsJson = EventsJson(events)
            });
        }

        // The technician behind a logged-in user (Users.UserID). Null when that user is not a lab technician.
        public (string Id, string Name, string Email)? GetTechnicianByUser(int userId)
        {
            if (_sample) return ("1", "Sipho Ndlovu", "sipho.ndlovu@nmbhlab.co.za");
            using var db = Open();
            var r = db.QueryFirstOrDefault("dbo.sp_Tech_GetTechnicianByUser", new { UserID = userId }, commandType: CommandType.StoredProcedure);
            if (r == null) return null;
            return (((int)r.TechnicianID).ToString(), $"{r.FirstName} {r.LastName}", (string)r.Email);
        }

        // The technician behind a signed-in e-mail address (the group's login keeps it in the session).
        public (string Id, string Name, string Email)? GetTechnicianByEmail(string email)
        {
            if (_sample) return ("1", "Sipho Ndlovu", "sipho.ndlovu@nmbhlab.co.za");
            using var db = Open();
            var r = db.QueryFirstOrDefault("dbo.sp_Tech_GetTechnicianByEmail", new { Email = email }, commandType: CommandType.StoredProcedure);
            if (r == null) return null;
            return (((int)r.TechnicianID).ToString(), $"{r.FirstName} {r.LastName}", (string)r.Email);
        }

        // The consumables each waiting tube needs against the lab manager's stock, keyed by BarcodeID.
        // A tube with no consumables linked is simply absent from the result (the screens say so).
        public Dictionary<int, ConsumableCheck> GetConsumableChecks()
        {
            var result = new Dictionary<int, ConsumableCheck>();
            if (_sample) return result;
            try
            {
                using var db = Open();
                using var multi = db.QueryMultiple("dbo.sp_Tech_GetTubeConsumables", commandType: CommandType.StoredProcedure);
                var needs = multi.Read<ConsumableRow>().ToList();
                // an older copy of the procedure returns only one result set: read the second one only when it is there
                var missing = multi.IsConsumed ? new List<MissingRow>() : multi.Read<MissingRow>().ToList();

                foreach (var g in needs.GroupBy(r => r.BarcodeID))
                    result[g.Key] = new ConsumableCheck
                    {
                        Needs = g.Select(r => new ConsumableNeed { Name = r.Name, Needed = r.Needed, OnHand = r.OnHand }).ToList()
                    };
                foreach (var g in missing.GroupBy(r => r.BarcodeID))
                {
                    if (!result.TryGetValue(g.Key, out var check)) result[g.Key] = check = new ConsumableCheck();
                    check.MissingTests = g.Select(r => r.TestName).Distinct().ToList();
                }
                return result;
            }
            catch (SqlException)
            {
                return new Dictionary<int, ConsumableCheck>();   // procedure or tables not there yet
            }
        }

        private class MissingRow
        {
            public int BarcodeID { get; set; }
            public string TestName { get; set; }
        }

        private class ConsumableRow
        {
            public int BarcodeID { get; set; }
            public string Name { get; set; }
            public int Needed { get; set; }
            public int OnHand { get; set; }
        }

        // Releases a request whose every test is verified. The procedure checks that again and releases only once.
        public bool ReleaseRequest(LabRequest req, string by, TestEvent ev)
            => _sample ? TechnicianSampleData.Release(req) : Run("dbo.sp_Tech_ReleaseRequest", new
            {
                RequestID = req.RequestId,
                By = Tech(by),
                EventsJson = EventsJson(new[] { ev })
            });

        // The e-mail address of the doctor who requested the tests (null if none is on record).
        public string GetDoctorEmail(LabRequest req)
        {
            if (_sample) return null;
            using var db = Open();
            return db.QueryFirstOrDefault<string>("dbo.sp_Tech_GetDoctorEmail", new { RequestID = req.RequestId },
                                                  commandType: CommandType.StoredProcedure);
        }

        // Second technician verifies. A technician can never verify their own capture (the procedure checks it).
        public bool ApproveResult(TestItem item, string by, TestEvent ev)
            => _sample ? TechnicianSampleData.Approve(item, by) : Run("dbo.sp_Tech_ApproveResult", new
            {
                RequestItemID = item.ItemId,
                RequestID = item.Request.RequestId,
                BarcodeID = BarcodeOf(item),
                By = Tech(by),
                EventsJson = EventsJson(new[] { ev })
            });

        // Not verified: back to the technician who performed it, with a note. Stage becomes To Be Reviewed.
        public bool ReturnResult(TestItem item, string by, string note, TestEvent ev)
            => _sample ? TechnicianSampleData.Return(item, by, note) : Run("dbo.sp_Tech_ReturnResult", new
            {
                RequestItemID = item.ItemId,
                RequestID = item.Request.RequestId,
                BarcodeID = BarcodeOf(item),
                By = Tech(by),
                Note = Trunc(note, 1000),
                EventsJson = EventsJson(new[] { ev })
            });

        // =====================================================================================
        // Profile (LabTechnicians + Users)
        // =====================================================================================

        public TechProfileViewModel GetProfile(string technicianId)
        {
            if (_sample)
            {
                var sp = TechnicianSampleData.Profile;
                return new TechProfileViewModel
                {
                    FirstName = sp.FirstName, LastName = sp.LastName, Email = sp.Email, IDNumber = sp.IDNumber,
                    EmployeeNumber = sp.EmployeeNumber, ContactNumber = sp.ContactNumber, RegistrationDate = sp.RegistrationDate,
                    TestTypes = TechnicianSampleData.TestNames().OrderBy(x => x).ToList()
                };
            }
            var tech = Tech(technicianId);
            if (tech == null) return null;
            using var db = Open();
            var p = db.QueryFirstOrDefault<TechProfileViewModel>("dbo.sp_Tech_GetProfile", new { TechnicianID = tech },
                                                                 commandType: CommandType.StoredProcedure);
            if (p == null) return null;
            p.TestTypes = GetAssignedTestNames(technicianId).OrderBy(x => x).ToList();
            return p;
        }

        public bool UpdateProfile(string technicianId, string firstName, string lastName, string contactNumber)
        {
            if (_sample)
            {
                TechnicianSampleData.Profile.FirstName = firstName.Trim();
                TechnicianSampleData.Profile.LastName = lastName.Trim();
                TechnicianSampleData.Profile.ContactNumber = contactNumber?.Trim();
                TechnicianSampleData.Names["1"] = $"{firstName.Trim()} {lastName.Trim()}";
                return true;
            }
            var tech = Tech(technicianId);
            if (tech == null) return false;
            var ok = Run("dbo.sp_Tech_UpdateProfile", new
            {
                TechnicianID = tech,
                FirstName = Trunc(firstName.Trim(), 50),
                LastName = Trunc(lastName.Trim(), 50),
                ContactNumber = Trunc(contactNumber?.Trim(), 20)
            });
            return ok;      // Run() also clears the cache: technician names are part of the cached graph
        }

        // Checks the current password (BCrypt, as the rest of the system) before saving the new one.
        // Returns null on success, otherwise the message to show.
        public string ChangePassword(string technicianId, string currentPassword, string newPassword)
        {
            if (_sample) return null;      // demo mode: nothing is stored
            var tech = Tech(technicianId);
            if (tech == null) return "Your account could not be found.";
            using var db = Open();
            var row = db.QueryFirstOrDefault<LoginRow>("dbo.sp_Tech_GetLogin", new { TechnicianID = tech },
                                                       commandType: CommandType.StoredProcedure);
            if (row == null) return "Your account could not be found.";

            bool ok;
            try { ok = BCrypt.Net.BCrypt.Verify(currentPassword, row.PasswordHash); }
            catch { ok = false; }
            if (!ok) return "Your current password is incorrect.";

            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            db.ExecuteScalar<int>("dbo.sp_Tech_SetPassword", new { UserID = row.UserID, PasswordHash = hash },
                                  commandType: CommandType.StoredProcedure);
            return null;
        }

        // =====================================================================================
        // Helpers
        // =====================================================================================

        // Runs a write procedure that ends with "SELECT 1 AS Ok" (done) or "SELECT 0 AS Ok" (already changed).
        private bool Run(string procedure, object args)
        {
            using var db = Open();
            var ok = db.ExecuteScalar<int>(procedure, args, commandType: CommandType.StoredProcedure) == 1;
            if (ok) _cache = null;
            return ok;
        }

        // The history events of one action, as the JSON the procedures read (sp_Tech_InsertEvents).
        private static string EventsJson(IEnumerable<TestEvent> events)
        {
            var list = (events ?? Enumerable.Empty<TestEvent>())
                .Where(e => e != null)
                .Select(e => new
                {
                    scope = e.Scope.ToString(),
                    action = Trunc(e.Action, 100),
                    detail = Trunc(e.Detail, 1000),
                    attempt = e.Attempt,
                    tone = e.Tone.ToString(),
                    changes = e.Changes != null && e.Changes.Count > 0 ? JsonSerializer.Serialize(e.Changes) : null,
                    by = Tech(e.By),
                    at = e.At.ToString("yyyy-MM-ddTHH:mm:ss.fff", Inv)
                }).ToList();
            return list.Count == 0 ? null : JsonSerializer.Serialize(list);
        }

        // The result lines of one attempt, as the JSON the procedures read (sp_Tech_SaveDraft / sp_Tech_SubmitResults).
        private static string ValuesJson(IEnumerable<ResultLine> lines)
        {
            var list = new List<object>();
            foreach (var l in lines ?? Enumerable.Empty<ResultLine>())
            {
                var numeric = TryNumber(l.Value, out var v);
                ParseRange(l.Range, out var low, out var high);
                list.Add(new
                {
                    name = Trunc(l.Parameter, 100),
                    value = numeric ? v : (decimal?)null,
                    unit = Trunc(l.Unit, 20),
                    low,
                    high,
                    flag = Trunc(l.Flag, 20),
                    comment = numeric ? null : Trunc(l.Value, 500)      // a text result (e.g. a comment) is kept in Comment
                });
            }
            return list.Count == 0 ? null : JsonSerializer.Serialize(list);
        }

        private class LoginRow { public int UserID { get; set; } public string PasswordHash { get; set; } }

        private static int? Tech(string id) => int.TryParse(id, out var n) ? n : (int?)null;

        private static int? BarcodeOf(TestItem item) => item.Sample != null && item.Sample.BarcodeId > 0 ? item.Sample.BarcodeId : (int?)null;

        private static string Trunc(string s, int max) => s == null ? null : (s.Length <= max ? s : s.Substring(0, max));

        private static bool TryNumber(string s, out decimal v)
            => decimal.TryParse(s, NumberStyles.Float, Inv, out v);

        private static readonly Regex RangeRx = new(@"(-?\d+(?:\.\d+)?)\s*[–—-]\s*(-?\d+(?:\.\d+)?)", RegexOptions.Compiled);

        private static void ParseRange(string text, out decimal? low, out decimal? high)
        {
            low = null; high = null;
            if (string.IsNullOrWhiteSpace(text)) return;
            var m = RangeRx.Match(text);
            if (!m.Success) return;
            if (decimal.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out var a) &&
                decimal.TryParse(m.Groups[2].Value, NumberStyles.Float, Inv, out var b))
            { low = a; high = b; }
        }

        private static string Num(decimal? v) => v.Value.ToString("0.####", Inv);

        private static ResultLine ToLine(ValueRow v) => new()
        {
            Parameter = v.ParameterName,
            Value = v.ResultValue.HasValue ? v.ResultValue.Value.ToString("0.####", Inv) : v.Comment,
            Unit = v.Unit,
            Range = v.ReferenceLow.HasValue && v.ReferenceHigh.HasValue ? $"{Num(v.ReferenceLow)} – {Num(v.ReferenceHigh)}" : null,
            Flag = v.Flag
        };

        // Tests from before we stored values per parameter only have one number: show it as one result line.
        private static List<ResultLine> SingleResult(ItemRow i)
        {
            if (!i.ResultValue.HasValue) return new List<ResultLine>();
            var v = i.ResultValue.Value;
            return new List<ResultLine>
            {
                new()
                {
                    Parameter = i.TestName,
                    Value = v.ToString("0.####", Inv),
                    Unit = i.UnitofMeasurement,
                    Range = $"{Num(i.NormalRangeMin)} – {Num(i.NormalRangeMax)}",
                    Flag = v < i.NormalRangeMin ? "Low" : v > i.NormalRangeMax ? "High" : "Normal"
                }
            };
        }

        private static TestStage? Map(string stage) => stage switch
        {
            "Awaiting Sample Receipt" => TestStage.AwaitingSampleReceipt,
            "Awaiting Sample Verification" => TestStage.AwaitingSampleVerification,
            "Sample Rejected" => TestStage.SampleRejected,
            "Awaiting Assignment" => TestStage.AwaitingAssignment,
            "Assigned" => TestStage.Assigned,
            "In Progress" => TestStage.InProgress,
            "Complete" => TestStage.Complete,
            "To Be Reviewed" => TestStage.ToBeReviewed,
            "Verified" => TestStage.Verified,
            _ => null
        };

        // Uses our Stage column; when it is empty (a test added by another part of the system) it is worked out
        // from your group's ItemStatus and the tube, with the same rules as the set-up script.
        private static TestStage ToStage(ItemRow i, SampleTube tube)
        {
            var mapped = Map(i.Stage);
            if (mapped.HasValue) return mapped.Value;

            if (i.ItemStatus == "Verified") return TestStage.Verified;
            if (i.ItemStatus == "In Progress")
                return i.ResultValue.HasValue ? TestStage.Complete
                     : i.StartDateTime.HasValue ? TestStage.InProgress
                     : TestStage.Assigned;

            return tube.Status switch
            {
                TubeStatus.NotReceived => TestStage.AwaitingSampleReceipt,
                TubeStatus.Rejected => TestStage.SampleRejected,
                TubeStatus.Verified => TestStage.AwaitingAssignment,
                _ => TestStage.AwaitingSampleVerification
            };
        }

        // The turnaround clock starts when the tube is received. Due text looks like "Due in 35m" / "Overdue by 1h 20m".
        private static void SetTat(TestItem t, ItemRow row, DateTime now)
        {
            switch (t.Stage)
            {
                case TestStage.Verified: t.Tat = TatStatus.OnTrack; t.DueText = "Completed"; return;
                case TestStage.SampleRejected: t.Tat = TatStatus.OnTrack; t.DueText = "Recollection needed"; return;
                case TestStage.AwaitingSampleReceipt: t.Tat = TatStatus.OnTrack; t.DueText = "Waiting for the sample"; return;
            }

            var due = t.Sample.ReceivedAt.AddMinutes(row.TurnaroundTimeMinutes);
            var left = due - now;
            var warn = TimeSpan.FromMinutes(Math.Max(15, row.TurnaroundTimeMinutes * 0.25));

            if (left < TimeSpan.Zero) { t.Tat = TatStatus.Overdue; t.DueText = "Overdue by " + HoursMinutes(-left); }
            else if (left <= warn) { t.Tat = TatStatus.NearingTat; t.DueText = "Due in " + HoursMinutes(left); }
            else { t.Tat = TatStatus.OnTrack; t.DueText = "Due in " + HoursMinutes(left); }
        }

        private static string HoursMinutes(TimeSpan s)
        {
            if (s.TotalHours < 1) return $"{Math.Max(1, (int)s.TotalMinutes)}m";
            if (s.TotalHours < 24) return s.Minutes > 0 ? $"{(int)s.TotalHours}h {s.Minutes}m" : $"{(int)s.TotalHours}h";
            return s.Hours > 0 ? $"{(int)s.TotalDays}d {s.Hours}h" : $"{(int)s.TotalDays}d";
        }

        private static string TubeType(string sampleTypeName)
        {
            var n = sampleTypeName ?? "";
            if (n.Contains("EDTA", StringComparison.OrdinalIgnoreCase)) return "EDTA (purple top)";
            if (n.Contains("Citrate", StringComparison.OrdinalIgnoreCase)) return "Citrate (blue top)";
            if (n.Contains("Serum", StringComparison.OrdinalIgnoreCase)) return "Serum (gold top)";
            return n;
        }

        // The Patients table has no sex column, so it is read from the South African ID number (digits 7 to 10: 5000 and up = male).
        private static string SexFromId(string idNumber)
            => idNumber != null && idNumber.Length >= 10 && int.TryParse(idNumber.Substring(6, 4), out var g)
                ? (g >= 5000 ? "Male" : "Female")
                : "Not recorded";

        private static PatientInfo BuildPatient(RequestRow r, IEnumerable<string> conditions,
                                                IEnumerable<AllergyRow> allergies, IEnumerable<MedRow> meds)
        {
            var conditionList = conditions.ToList();
            var allergyList = allergies.ToList();
            var p = new PatientInfo
            {
                PatientId = "PT-" + r.PatientID,
                FirstName = r.FirstName,
                Surname = r.LastName,
                DateOfBirth = r.DateOfBirth,
                Sex = SexFromId(r.IDNumber),
                Conditions = conditionList.Count > 0 ? string.Join("; ", conditionList) : "None recorded",
                Allergies = allergyList.Count > 0
                    ? string.Join("; ", allergyList.Select(a => string.IsNullOrWhiteSpace(a.Severity) ? a.AllergyName : $"{a.AllergyName} ({a.Severity})"))
                    : "None known",
                IsPregnant = null,
                FastingStatus = "Not required",
                Precautions = "None flagged"
            };
            foreach (var m in meds)
            {
                p.Medications.Add(new Medication
                {
                    Name = m.MedicationName,
                    Dose = string.Join(" ", new[] { m.Dosage, m.Frequency }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    LabNote = m.Notes
                });
            }
            return p;
        }
    }

    // ---- row shapes for Dapper (the property names are the column names) ----

    internal sealed class RequestRow
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; }
        public int PatientID { get; set; }
        public DateTime RequestDate { get; set; }
        public string Urgency { get; set; }
        public string ClinicalNotes { get; set; }
        public string RequestStatus { get; set; }
        public string Ward { get; set; }
        public DateTime? ReleasedDate { get; set; }
        public int? ReleasedBy { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string IDNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string DoctorFirst { get; set; }
        public string DoctorLast { get; set; }
    }

    internal sealed class TubeRow
    {
        public int BarcodeID { get; set; }
        public int RequestID { get; set; }
        public string BarcodeNumber { get; set; }
        public int SampleTypeID { get; set; }
        public string SampleTypeName { get; set; }
        public DateTime CollectionDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string SampleCondition { get; set; }
        public string VerificationStatus { get; set; }
        public int? VerifiedBy { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public string RejectReason { get; set; }
    }

    internal sealed class ItemRow
    {
        public int RequestItemID { get; set; }
        public int RequestID { get; set; }
        public int TestTypeID { get; set; }
        public string ItemStatus { get; set; }
        public string Stage { get; set; }
        public int? AssignedTo { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }
        public int? VerifiedBy { get; set; }
        public DateTime? VerificationDateTime { get; set; }
        public decimal? ResultValue { get; set; }
        public string ResultNotes { get; set; }
        public bool? IsAbnormal { get; set; }
        public int? BarcodeID { get; set; }
        public string TestName { get; set; }
        public string UnitofMeasurement { get; set; }
        public decimal NormalRangeMin { get; set; }
        public decimal NormalRangeMax { get; set; }
        public int TurnaroundTimeMinutes { get; set; }
        public decimal? PrevValue { get; set; }
        public DateTime? PrevDate { get; set; }
    }

    internal sealed class ValueRow
    {
        public int RequestItemID { get; set; }
        public int Attempt { get; set; }
        public string ParameterName { get; set; }
        public decimal? ResultValue { get; set; }
        public string Unit { get; set; }
        public decimal? ReferenceLow { get; set; }
        public decimal? ReferenceHigh { get; set; }
        public string Flag { get; set; }
        public string Comment { get; set; }
        public int? CapturedBy { get; set; }
        public DateTime CapturedDate { get; set; }
    }

    internal sealed class ReviewRow
    {
        public int RequestItemID { get; set; }
        public int ReviewedBy { get; set; }
        public DateTime ReviewDate { get; set; }
        public string ReviewNotes { get; set; }
        public string Action { get; set; }
    }

    internal sealed class EventRow
    {
        public int RequestID { get; set; }
        public int? BarcodeID { get; set; }
        public int? RequestItemID { get; set; }
        public string Scope { get; set; }
        public string Action { get; set; }
        public string Detail { get; set; }
        public int? Attempt { get; set; }
        public string Tone { get; set; }
        public string Changes { get; set; }
        public int? PerformedBy { get; set; }
        public DateTime EventDate { get; set; }
    }

    internal sealed class ConditionRow
    {
        public int PatientID { get; set; }
        public string ConditionName { get; set; }
    }

    internal sealed class AllergyRow
    {
        public int PatientID { get; set; }
        public string AllergyName { get; set; }
        public string Severity { get; set; }
    }

    internal sealed class TechRow
    {
        public int TechnicianID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    internal sealed class MedRow
    {
        public int PatientID { get; set; }
        public string MedicationName { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Notes { get; set; }
    }
}
