using System;
using System.Collections.Generic;
using System.Linq;
using _4th_year_set_up.Repository;

namespace _4th_year_set_up.Models
{
    // Shape of the data
    //
    //   LabRequest  (REQ-0153)        patient, doctor, ward, clinical notes. One request is released as a whole.
    //     SampleTube  (REQ-0153-T1)   one physical tube. Received and verified once per tube.
    //       TestItem  (REQ-0153-FBC)  one test on a tube. Has its own stage, technician, results and attempts.
    //
    // A request with a single test is simply one request, one tube, one test.
    //
    // Lifecycle of a test:
    // Sample received -> AwaitingSampleVerification -> (SampleRejected | AwaitingAssignment)
    //   -> Assigned -> InProgress -> Complete (results captured)
    //   -> Verified (second technician) | ToBeReviewed (returned to the technician with a note)
    // When every test on a request is Verified, the request can be released (all together).
    public enum TestStage
    {
        AwaitingSampleReceipt,          // the tube has not reached the lab yet (the Receive Samples screen handles that)
        AwaitingSampleVerification,
        SampleRejected,
        AwaitingAssignment,
        Assigned,
        InProgress,
        Complete,
        ToBeReviewed,
        Verified
    }

    public enum TatStatus { OnTrack, NearingTat, Overdue }

    public enum TubeStatus { NotReceived, AwaitingVerification, Rejected, Verified }

    public class Medication
    {
        public string Name { get; set; }
        public string Dose { get; set; }
        public string LabNote { get; set; }   // why it matters to the lab (interference / expected effect)
    }

    public class PatientInfo
    {
        public string PatientId { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Sex { get; set; }
        public string Conditions { get; set; }
        public List<Medication> Medications { get; set; } = new();
        public string Allergies { get; set; }
        public bool? IsPregnant { get; set; }           // null = not applicable
        public string FastingStatus { get; set; }
        public string Precautions { get; set; }

        public string FullName => $"{FirstName} {Surname}";
        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > today.AddYears(-age)) age--;
                return age;
            }
        }
    }

    public class ResultLine
    {
        public string Parameter { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string Range { get; set; }
        public string Flag { get; set; }      // Normal | High | Low | Critical
        public bool IsAbnormal => Flag != null && Flag != "Normal";
        public string FlagCss => Flag switch
        {
            "Normal" => "badge-ontrack",
            "Low" or "High" => "badge-nearing",
            null => "badge-awaiting",
            _ => "badge-overdue"          // Critical low / Critical high
        };
    }

    // ------------------------------------------------------------------ request, tube

    // One physical tube. Sample verification (accept / reject) happens once per tube and applies to every test on it.
    public class SampleTube
    {
        public int BarcodeId { get; set; }                    // SampleBarcodes.BarcodeID
        public string TubeId { get; set; }                    // REQ-0153-T1  (the barcode number once the database is connected)
        public string Type { get; set; }                      // EDTA (purple top)
        public string SampleType { get; set; } = "Whole blood";
        public string Condition { get; set; } = "Not yet verified";
        public TubeStatus Status { get; set; }
        public DateTime CollectedAt { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string RejectReason { get; set; }

        public LabRequest Request { get; set; }
        public List<TestItem> Tests { get; } = new();
        public List<TestEvent> Events { get; } = new();       // events that belong to this tube (shared by its tests)

        public string TestsLabel => string.Join(" + ", Tests.Select(t => t.Test));

        public TestEvent Log(string action, string by, EventTone tone = EventTone.Neutral, string detail = null, DateTime? at = null)
        {
            var e = TestEvent.Create(action, by, EventScope.Sample, tone, detail, null, at, null);
            Events.Add(e);
            return e;
        }
    }

    // The request as it arrived from the ward or clinic. Released as a whole, never test by test.
    public class LabRequest
    {
        public int RequestId { get; set; }                    // TestRequests.RequestID
        public string ReqNum { get; set; }
        public PatientInfo Patient { get; set; }
        public string RequestingDoctor { get; set; }
        public string Ward { get; set; }
        public string ClinicalNotes { get; set; }
        public bool IsUrgent { get; set; }
        public string Priority { get; set; } = "Routine";     // Routine | Urgent | STAT
        public DateTime RequestedAt { get; set; }             // when the doctor created the request

        public List<SampleTube> Tubes { get; } = new();
        public List<TestItem> Tests { get; } = new();
        public List<TestEvent> Events { get; } = new();       // events that belong to the whole request

        public string ReleasedBy { get; set; }
        public DateTime? ReleasedAt { get; set; }

        public bool IsReleased => ReleasedAt.HasValue;
        public bool AllVerified => Tests.Count > 0 && Tests.All(t => t.Stage == TestStage.Verified);
        public int VerifiedCount => Tests.Count(t => t.Stage == TestStage.Verified);
        public bool IsReadyToRelease => !IsReleased && AllVerified;
        public bool IsHeld => !IsReleased && !AllVerified && VerifiedCount > 0;

        // Why a request that has verified results can't be released yet ("ESR was returned for review").
        public string HoldReason
        {
            get
            {
                if (!IsHeld) return null;
                var t = Tests.Where(x => x.Stage != TestStage.Verified)
                             .OrderBy(x => x.Stage == TestStage.ToBeReviewed ? 0 : x.Stage == TestStage.SampleRejected ? 1 : 2)
                             .First();
                var what = t.Stage switch
                {
                    TestStage.ToBeReviewed => "was returned for review",
                    TestStage.Complete => "is waiting to be verified",
                    TestStage.SampleRejected => "needs a new sample",
                    TestStage.AwaitingSampleReceipt => "is waiting for its sample to arrive",
                    TestStage.AwaitingSampleVerification => "is waiting for its sample to be verified",
                    _ => "is not finished yet"
                };
                return $"{t.Test} {what}";
            }
        }

        public TestEvent Log(string action, string by, EventTone tone = EventTone.Neutral, string detail = null, DateTime? at = null)
        {
            var e = TestEvent.Create(action, by, EventScope.Request, tone, detail, null, at, null);
            Events.Add(e);
            return e;
        }
    }

    // ------------------------------------------------------------------ one test on a request

    public class TestItem
    {
        // Identity: TestId is unique (REQ-0153-FBC). ReqNum repeats for every test on the same request.
        public string TestId { get; set; }                    // the RequestItemID as text once the database is connected
        public int ItemId { get; set; }                       // TestRequestItems.RequestItemID
        public int TestTypeId { get; set; }                   // TestTypes.TestTypeID
        public LabRequest Request { get; set; }
        public SampleTube Sample { get; set; }

        public string Test { get; set; }
        public TestStage Stage { get; set; }
        public bool AssignedToMe { get; set; }
        public string AssignedTo { get; set; }                // technician ID of whoever holds the test (null = unassigned)
        public int TargetMinutes { get; set; }                // turnaround target for this test type
        public TatStatus Tat { get; set; }
        public string DueText { get; set; }
        public string PreviousResult { get; set; }

        // Request / sample details live on the request and tube; these keep the existing views working.
        public string ReqNum => Request.ReqNum;
        public PatientInfo PatientDetails => Request.Patient;
        public bool IsUrgent => Request.IsUrgent;
        public string RequestingDoctor => Request.RequestingDoctor;
        public string Ward => Request.Ward;
        public string ClinicalNotes => Request.ClinicalNotes;
        public string SampleType => Sample.SampleType;
        public string Tube => Sample.Type;
        public DateTime CollectedAt => Sample.CollectedAt;
        public DateTime ReceivedAt => Sample.ReceivedAt;
        public string SampleCondition => Sample.Condition;
        public string SampleVerifiedBy => Sample.VerifiedBy;
        public DateTime? SampleVerifiedAt => Sample.VerifiedAt;
        public string SampleRejectReason => Sample.RejectReason;

        // Results (captured by the technician who performed the test)
        public List<ResultLine> Results { get; set; } = new();
        public string CapturedBy { get; set; }
        public DateTime? CapturedAt { get; set; }

        // Result verification (second technician)
        public string VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string ReviewNote { get; set; }          // why the result was returned (To Be Reviewed)
        public string CaptureNote { get; set; }         // optional note from the technician to the verifier
        public List<ReturnedAttempt> History { get; set; } = new();   // earlier results that were returned

        // ---- Tests on the same request
        public bool InMultiTestRequest => Request.Tests.Count > 1;
        public string PositionLabel => InMultiTestRequest ? $"{Request.Tests.IndexOf(this) + 1} of {Request.Tests.Count}" : null;
        public IEnumerable<TestItem> Siblings => Request.Tests.Where(t => t != this);

        // This test is what is stopping a request with verified results from being released.
        public bool IsBlockingRelease => Request.IsHeld && Stage != TestStage.Verified;

        // ---- Audit trail: append-only. Events are only ever added, never edited or removed.
        // Drafts are not logged; only real state changes (received, verified, picked up, captured, returned, released...).
        // Own events are test events; tube and request events are stored on the tube / request and shown here too.
        public List<TestEvent> Events { get; set; } = new();

        // 1 for the first capture; goes up by one each time a verifier returns the result.
        public int Attempt => History.Count + 1;

        // Newest first: this test's events plus its tube's and its request's. Ties keep the order they were logged in.
        public IEnumerable<TestEvent> Timeline =>
            (Request?.Events ?? new List<TestEvent>())
                .Concat(Sample?.Events ?? new List<TestEvent>())
                .Concat(Events)
                .Select((e, i) => (e, i))
                .OrderByDescending(x => x.e.At).ThenByDescending(x => x.i)
                .Select(x => x.e);

        public TestEvent LastEvent => Timeline.FirstOrDefault();

        // Scope decides where the event is stored: Request -> the request, Sample -> the tube, Test -> this test.
        public TestEvent Log(string action, string by, EventScope scope = EventScope.Test, EventTone tone = EventTone.Neutral,
                             string detail = null, int? attempt = null, DateTime? at = null, List<ValueChange> changes = null)
        {
            var e = TestEvent.Create(action, by, scope, tone, detail, attempt, at, changes);
            switch (scope)
            {
                case EventScope.Request when Request != null: Request.Events.Add(e); break;
                case EventScope.Sample when Sample != null: Sample.Events.Add(e); break;
                default: Events.Add(e); break;
            }
            return e;
        }

        // "ESR 120 mm/hr (High)"  — used as the detail line on capture events.
        public static string Summarise(IEnumerable<ResultLine> lines)
            => string.Join(" · ", lines.Select(l => $"{l.Parameter} {l.Value} {l.Unit}" + (l.IsAbnormal ? $" ({l.Flag})" : "")));

        // Which parameters changed between two captures (for the re-entry entry in the history).
        public static List<ValueChange> Diff(IEnumerable<ResultLine> before, IEnumerable<ResultLine> after)
        {
            var old = (before ?? Enumerable.Empty<ResultLine>()).ToDictionary(r => r.Parameter, r => r);
            var changes = new List<ValueChange>();
            foreach (var n in after)
            {
                old.TryGetValue(n.Parameter, out var o);
                if (o == null || o.Value != n.Value)
                    changes.Add(new ValueChange { Parameter = n.Parameter, From = o?.Value, To = n.Value, Unit = n.Unit });
            }
            return changes;
        }

        public string Patient => PatientDetails?.FullName;

        public string StageLabel => Stage switch
        {
            TestStage.AwaitingSampleReceipt => "Awaiting sample",
            TestStage.AwaitingSampleVerification => "Awaiting sample verification",
            TestStage.SampleRejected => "Sample rejected",
            TestStage.AwaitingAssignment => "Awaiting assignment",
            TestStage.Assigned => "Assigned",
            TestStage.InProgress => "In progress",
            TestStage.Complete => "Complete",
            TestStage.ToBeReviewed => "To be reviewed",
            TestStage.Verified => Request.IsReleased ? "Released" : "Verified",
            _ => ""
        };

        public string StageCss => Stage switch
        {
            TestStage.AwaitingSampleReceipt or TestStage.AwaitingSampleVerification => "badge-pending",
            TestStage.SampleRejected => "badge-overdue",
            TestStage.AwaitingAssignment => "badge-awaiting",
            TestStage.Assigned => "badge-assigned",
            TestStage.InProgress => "badge-inprogress",
            TestStage.Complete => "badge-complete",
            TestStage.ToBeReviewed => "badge-review",
            _ => "badge-verified"
        };

        public string TatLabel => Tat switch
        {
            TatStatus.Overdue => "Overdue",
            TatStatus.NearingTat => "Nearing TAT",
            _ => "On track"
        };

        // Tests with a verified sample that technicians are working through
        public bool IsInQueue => Stage is TestStage.AwaitingAssignment or TestStage.Assigned
                                        or TestStage.InProgress or TestStage.ToBeReviewed;

        // Still has a turnaround clock running (not finished and not rejected)
        public bool IsActive => Stage != TestStage.Verified && Stage != TestStage.SampleRejected;
    }

    // An earlier capture that a verifier returned. Kept so the audit trail shows what was entered and why it was returned.
    public class ReturnedAttempt
    {
        public string Note { get; set; }
        public string CapturedBy { get; set; }
        public DateTime? CapturedAt { get; set; }
        public List<ResultLine> Results { get; set; } = new();
    }

    // What the event is about. A request event (received, released) and a sample event (received, verified, rejected)
    // are shared by every test they cover; a test event belongs to one test only.
    public enum EventScope { Request, Sample, Test }

    // Colour of the dot on the timeline.
    public enum EventTone { Neutral, Good, Warn, Bad }

    public class ValueChange
    {
        public string Parameter { get; set; }
        public string From { get; set; }     // null = nothing was entered before
        public string To { get; set; }
        public string Unit { get; set; }
    }

    public class TestEvent
    {
        public DateTime At { get; set; }
        public string By { get; set; }                 // technician ID
        public string Action { get; set; }             // "Result returned to technician"
        public string Detail { get; set; }             // note / reason / values
        public int? Attempt { get; set; }              // which capture attempt this belongs to, if any
        public EventScope Scope { get; set; }
        public EventTone Tone { get; set; }
        public List<ValueChange> Changes { get; set; } = new();

        public string ToneCss => Tone switch
        {
            EventTone.Good => "tl-good",
            EventTone.Warn => "tl-warn",
            EventTone.Bad => "tl-bad",
            _ => "tl-neutral"
        };

        public static TestEvent Create(string action, string by, EventScope scope, EventTone tone, string detail,
                                       int? attempt, DateTime? at, List<ValueChange> changes)
            => new()
            {
                At = at ?? DateTime.Now, By = by, Action = action, Scope = scope, Tone = tone,
                Detail = detail, Attempt = attempt, Changes = changes ?? new List<ValueChange>()
            };
    }

    public static class SampleChecklist
    {
        public const string InsufficientConsumables = "Insufficient consumables";

        public static readonly string[] Items =
        {
            "Label matches the request (two patient identifiers)",
            "Correct tube and sample type",
            "Adequate volume",
            "No clots, haemolysis or lipaemia",
            "Collected within the stability window",
            "Request form is complete and legible"
        };

        public static readonly string[] RejectReasons =
        {
            "Clotted",
            "Haemolysed",
            "Insufficient volume",
            "Wrong tube or sample type",
            "Label does not match request",
            "Unlabelled",
            "Outside stability window",
            InsufficientConsumables,
            "Other (give details)"
        };
    }

    // The signed-in technician. One user for now: Sipho Ndlovu (LabTechnicians.TechnicianID 1).
    // Replace Id / Name with the logged-in user when authentication is connected.
    // VerifiableTests = the test types the laboratory manager has assigned to this technician
    // (TechnicianTestTypes); the controller fills it from the database.
    public static class CurrentUser
    {
        public const string Id = "1";
        public const string Name = "Sipho Ndlovu";
        public static HashSet<string> VerifiableTests = new();

        // There are no technician roles: Sipho, Lerato and Pieter are all the same kind of technician and can do every step
        // (verify samples, capture results, verify results). The only safeguards left are the two-step rule below.
        public static bool CanVerifySample => true;

        // Rules from the specification:
        //  - a technician cannot verify their own results
        public static string WhyCannotVerifyResult(TestItem t)
        {
            if (t.Stage != TestStage.Complete) return "This result is not waiting for verification.";
            if (t.CapturedBy == Id) return "You captured this result, so you can't verify it. A different technician must.";
            return null;
        }

        public static bool CanVerifyResult(TestItem t) => WhyCannotVerifyResult(t) == null;
    }

    // Turns a technician ID ("2") into a name for display. Filled from LabTechnicians every time data is loaded.
    public static class TechnicianNames
    {
        private static Dictionary<string, string> _names = new();

        public static void Set(Dictionary<string, string> names) => _names = names ?? new();
        public static IEnumerable<KeyValuePair<string, string>> All => _names;

        public static string Of(string id)
            => string.IsNullOrEmpty(id) ? "—" : _names.TryGetValue(id, out var n) ? n : $"Technician {id}";
    }

    public class DashboardViewModel
    {
        public int QueueCount { get; set; }
        public int MineCount { get; set; }
        public int AwaitingCount { get; set; }
        public int VerifiedCount { get; set; }
        public int UrgentCount { get; set; }
        public int OverdueCount { get; set; }
        public int NearingCount { get; set; }

        // Tasks every technician can do
        public int SamplesToVerifyCount { get; set; }       // tubes waiting for verification
        public int ResultsToVerifyCount { get; set; }
        public int ReadyToReleaseCount { get; set; }        // requests with every test verified
        public List<TestItem> NewlyAssigned { get; set; } = new();   // tests the lab manager assigned that you have not acknowledged yet

        // All of this technician's tests in the queue for the dashboard card (most pressing first)
        public List<TestItem> MyTestsPreview { get; set; } = new();

        public static DashboardViewModel Build(TechnicianRepository db)
        {
            var all = db.GetTests();
            var requests = db.GetRequests();
            var queue = all.Where(t => t.IsInQueue).ToList();
            return new DashboardViewModel
            {
                MyTestsPreview = queue
                    .Where(t => t.AssignedToMe)
                    .OrderByDescending(t => t.Stage == TestStage.ToBeReviewed)
                    .ThenByDescending(t => t.Tat == TatStatus.Overdue)
                    .ThenByDescending(t => t.Tat == TatStatus.NearingTat)
                    .ThenByDescending(t => t.IsUrgent)
                    .ToList(),
                QueueCount = queue.Count(t => !t.AssignedToMe),     // the pool: your own tests are in My Tests
                MineCount = queue.Count(t => t.AssignedToMe),
                AwaitingCount = all.Count(t => t.Stage == TestStage.AwaitingAssignment),
                VerifiedCount = all.Count(t => t.Stage == TestStage.Verified),
                UrgentCount = queue.Count(t => t.IsUrgent),
                OverdueCount = queue.Count(t => t.Tat == TatStatus.Overdue),
                NearingCount = queue.Count(t => t.Tat == TatStatus.NearingTat),
                SamplesToVerifyCount = requests.SelectMany(r => r.Tubes).Count(t => t.Status == TubeStatus.AwaitingVerification),
                ResultsToVerifyCount = all.Count(CurrentUser.CanVerifyResult),
                ReadyToReleaseCount = requests.Count(r => r.IsReadyToRelease),
            };
        }
    }

    public class TestListViewModel
    {
        public string Mode { get; set; }          // queue | mine | awaiting | verified | samples | results
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string InitialFilter { get; set; } // all | urgent | overdue | nearing
        public List<TestItem> Items { get; set; } = new();
        public DashboardViewModel Counts { get; set; }
    }

    // One row of the Receive Samples table. Every value is already text, so the view has nothing to dereference.
    public class ReceiveRow
    {
        public string Barcode { get; set; } = "";
        public string ReqNum { get; set; } = "—";
        public string Tests { get; set; } = "—";
        public string Sample { get; set; } = "—";
        public string Priority { get; set; } = "Routine";
        public string Waited { get; set; } = "—";
        public bool IsUrgent => Priority != "Routine";
        public List<ConsumableNeed> Needs { get; set; } = new();
        public bool HasShortage => Needs.Any(n => n.IsShort);
    }

    // The Release Results screen: requests that are ready, requests held back, and requests already released.
    public class ReleaseViewModel
    {
        public List<LabRequest> Ready { get; set; } = new();
        public List<LabRequest> Held { get; set; } = new();
        public List<LabRequest> Released { get; set; } = new();

        public static ReleaseViewModel Build(TechnicianRepository db)
        {
            var requests = db.GetRequests();
            return new ReleaseViewModel
            {
                Ready = requests.Where(r => r.IsReadyToRelease).ToList(),
                Held = requests.Where(r => r.IsHeld).ToList(),
                Released = requests.Where(r => r.IsReleased).OrderByDescending(r => r.ReleasedAt).Take(10).ToList()
            };
        }
    }
}
