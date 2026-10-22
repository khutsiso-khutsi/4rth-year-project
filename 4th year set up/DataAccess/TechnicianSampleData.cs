using System;
using System.Collections.Generic;
using System.Linq;
using _4th_year_set_up.Models;

namespace _4th_year_set_up.DataAccess
{
    // OFFLINE DEMO DATA for the Technician screens.
    //
    // Used only when appsettings has  "Technician": { "UseSampleData": true }.
    // Nothing here touches the database. The patients, requests and results below are FICTIONAL and exist so the
    // screens can be viewed and clicked through when the database is not reachable (no VPN, login problems).
    // Actions (accept a sample, pick up a test, capture, verify, return) change this in-memory copy, and it
    // resets when the app restarts. Delete this file and the "UseSampleData" switch before the real hand-in.
    internal static class TechnicianSampleData
    {
        private static readonly object Gate = new();
        private static List<LabRequest> _graph;

        public static readonly Dictionary<string, string> Names = new()
        {
            ["1"] = "Sipho Ndlovu",
            ["2"] = "Lerato Khumalo",
            ["3"] = "Pieter Botha"
        };

        public static List<LabRequest> Graph
        {
            get
            {
                lock (Gate)
                {
                    _graph ??= Build();
                    Refresh(_graph);
                    return _graph;
                }
            }
        }

        public static HashSet<string> TestNames()
            => new(Graph.SelectMany(r => r.Tests).Select(t => t.Test));

        public static TechProfileViewModel Profile { get; } = new()
        {
            FirstName = "Sipho",
            LastName = "Ndlovu",
            Email = "sipho.ndlovu@nmbhlab.co.za",
            IDNumber = "9001015800087",
            EmployeeNumber = "EMP-0001",
            ContactNumber = "0821234567",
            RegistrationDate = new DateTime(2026, 1, 12)
        };

        // The turnaround clock starts when the tube is received (same rule as the real data access).
        private static void Refresh(IEnumerable<LabRequest> requests)
        {
            var now = DateTime.Now;
            foreach (var t in requests.SelectMany(r => r.Tests))
            {
                switch (t.Stage)
                {
                    case TestStage.Verified: t.Tat = TatStatus.OnTrack; t.DueText = "Completed"; continue;
                    case TestStage.SampleRejected: t.Tat = TatStatus.OnTrack; t.DueText = "Recollection needed"; continue;
                    case TestStage.AwaitingSampleReceipt: t.Tat = TatStatus.OnTrack; t.DueText = "Waiting for the sample"; continue;
                }
                var left = t.Sample.ReceivedAt.AddMinutes(t.TargetMinutes) - now;
                var warn = TimeSpan.FromMinutes(Math.Max(15, t.TargetMinutes * 0.25));
                if (left < TimeSpan.Zero) { t.Tat = TatStatus.Overdue; t.DueText = "Overdue by " + Span(-left); }
                else if (left <= warn) { t.Tat = TatStatus.NearingTat; t.DueText = "Due in " + Span(left); }
                else { t.Tat = TatStatus.OnTrack; t.DueText = "Due in " + Span(left); }
            }
        }

        private static string Span(TimeSpan s)
        {
            if (s.TotalHours < 1) return $"{Math.Max(1, (int)s.TotalMinutes)}m";
            if (s.TotalHours < 24) return s.Minutes > 0 ? $"{(int)s.TotalHours}h {s.Minutes}m" : $"{(int)s.TotalHours}h";
            return s.Hours > 0 ? $"{(int)s.TotalDays}d {s.Hours}h" : $"{(int)s.TotalDays}d";
        }

        // ------------------------------------------------------------------ building the sample graph

        private static int _nextId = 1000;
        private static int _nextTube = 500;

        private static List<LabRequest> Build()
        {
            var now = DateTime.Now;
            var list = new List<LabRequest>();

            var thandi = Patient("PT-1", "Thandi", "Mokoena", 41, "Female", "Hypertension; Type 2 diabetes",
                "Penicillin (Severe); Latex (Moderate)", null,
                new Medication { Name = "Metformin", Dose = "500 mg twice daily", LabNote = "May lower vitamin B12 over time" },
                new Medication { Name = "Amlodipine", Dose = "5 mg daily" });
            var pieter = Patient("PT-2", "Pieter", "van Wyk", 67, "Male", "Atrial fibrillation", "None known", null,
                new Medication { Name = "Warfarin", Dose = "5 mg daily", LabNote = "Prolongs prothrombin time (expected)" });
            var nomsa = Patient("PT-3", "Nomsa", "Dlamini", 29, "Female", "Iron-deficiency anaemia", "Sulfonamides (Mild)", true,
                new Medication { Name = "Ferrous sulphate", Dose = "200 mg daily" });
            var ayanda = Patient("PT-4", "Ayanda", "Khoza", 54, "Male", "Chronic kidney disease (stage 3)", "None known", null,
                new Medication { Name = "Aspirin", Dose = "75 mg daily", LabNote = "Affects platelet function" });
            var sarah = Patient("PT-5", "Sarah", "Jacobs", 36, "Female", "None recorded", "None known", false);

            // 1. Urgent. Tube just arrived: a senior technician must verify it.
            var r1 = Request("REQ-20261020-901", thandi, "Dr A. Naidoo", "Fatigue and pallor. Rule out anaemia.", "Urgent", now.AddMinutes(-95));
            var t1 = Tube(r1, "EDTA (purple top)", "EDTA", TubeStatus.AwaitingVerification, now.AddMinutes(-40));
            Test(r1, t1, "Haemoglobin", TestStage.AwaitingSampleVerification, 180);
            Test(r1, t1, "White cell count", TestStage.AwaitingSampleVerification, 240);

            // 2. Citrate tube nearly at its limit.
            var r2 = Request("REQ-20261020-902", pieter, "Dr M. Pillay", "Warfarin monitoring.", "Routine", now.AddHours(-5));
            var t2 = Tube(r2, "Citrate (blue top)", "Citrate", TubeStatus.AwaitingVerification, now.AddMinutes(-220));
            Test(r2, t2, "Prothrombin time", TestStage.AwaitingSampleVerification, 240);
            Test(r2, t2, "Activated partial thromboplastin time", TestStage.AwaitingSampleVerification, 240);

            // 3. Verified tube, three tests waiting for somebody to pick them up.
            var r3 = Request("REQ-20261020-903", nomsa, "Dr A. Naidoo", "Antenatal booking bloods.", "Routine", now.AddHours(-3));
            var t3 = Tube(r3, "EDTA (purple top)", "EDTA", TubeStatus.Verified, now.AddMinutes(-70), "2", now.AddMinutes(-50));
            Test(r3, t3, "Haemoglobin", TestStage.AwaitingAssignment, 180);
            Test(r3, t3, "Platelet count", TestStage.AwaitingAssignment, 240);
            Test(r3, t3, "Mean cell volume", TestStage.AwaitingAssignment, 240);

            // 4. STAT from two days ago: overdue.
            var r4 = Request("REQ-20261018-904", ayanda, "Dr S. Govender", "Bleeding gums. Check platelets.", "STAT", now.AddDays(-2).AddHours(-1));
            var t4 = Tube(r4, "EDTA (purple top)", "EDTA", TubeStatus.Verified, now.AddDays(-2), "2", now.AddDays(-2).AddMinutes(20));
            Test(r4, t4, "White cell count", TestStage.AwaitingAssignment, 240);
            Test(r4, t4, "Erythrocyte sedimentation rate", TestStage.AwaitingAssignment, 180);

            // 5. Sipho's own work in progress.
            var r5 = Request("REQ-20261020-905", sarah, "Dr M. Pillay", "Routine check.", "Urgent", now.AddHours(-4));
            var t5 = Tube(r5, "EDTA (purple top)", "EDTA", TubeStatus.Verified, now.AddMinutes(-150), "2", now.AddMinutes(-130));
            var i5a = Test(r5, t5, "Haemoglobin", TestStage.Assigned, 180);
            var i5b = Test(r5, t5, "Haematocrit", TestStage.InProgress, 180);
            Hold(i5a, "1", now.AddMinutes(-100));
            Hold(i5b, "1", now.AddMinutes(-90));

            // 6. Captured by Lerato, waiting for Sipho to verify.
            var r6 = Request("REQ-20261019-906", thandi, "Dr A. Naidoo", "Follow-up on diabetes.", "Routine", now.AddDays(-1).AddHours(-2));
            var t6 = Tube(r6, "EDTA (purple top)", "EDTA", TubeStatus.Verified, now.AddHours(-26), "1", now.AddHours(-25));
            var i6a = Test(r6, t6, "Haemoglobin", TestStage.Complete, 180);
            var i6b = Test(r6, t6, "White cell count", TestStage.Complete, 240);
            Captured(i6a, "2", now.AddHours(-22), Line("Haemoglobin", "9.4", "g/dL", "12.0 – 15.5", "Low"));
            Captured(i6b, "2", now.AddHours(-22), Line("White cell count", "7.8", "x10^9/L", "4.0 – 11.0", "Normal"));

            // 7. Returned to Sipho by the verifier, with a history.
            var r7 = Request("REQ-20261017-907", pieter, "Dr M. Pillay", "INR out of range last week.", "Urgent", now.AddDays(-3).AddHours(-2));
            var t7 = Tube(r7, "Citrate (blue top)", "Citrate", TubeStatus.Verified, now.AddDays(-3), "1", now.AddDays(-3).AddMinutes(25));
            var i7a = Test(r7, t7, "Prothrombin time", TestStage.ToBeReviewed, 240);
            var i7b = Test(r7, t7, "Activated partial thromboplastin time", TestStage.Verified, 240);
            Hold(i7a, "1", now.AddDays(-3).AddMinutes(60));
            i7a.Results = new List<ResultLine> { Line("Prothrombin time", "41.0", "s", "11.0 – 13.5", "Critical high") };
            i7a.CapturedBy = "1"; i7a.CapturedAt = now.AddDays(-2).AddHours(-6);
            i7a.History.Add(new ReturnedAttempt
            {
                Note = "Value looks too high even for warfarin. Please re-run before it is verified.",
                CapturedBy = "1",
                CapturedAt = now.AddDays(-2).AddHours(-6),
                Results = new List<ResultLine> { Line("Prothrombin time", "41.0", "s", "11.0 – 13.5", "Critical high") }
            });
            i7a.ReviewNote = i7a.History[0].Note;
            Captured(i7b, "1", now.AddDays(-3).AddHours(2), Line("Activated partial thromboplastin time", "34.0", "s", "25.0 – 36.0", "Normal"));
            Verified(i7b, "2", now.AddDays(-3).AddHours(3));

            // 8. Everything verified: ready for the doctor to release.
            var r8 = Request("REQ-20261016-908", nomsa, "Dr A. Naidoo", "Follow-up.", "Routine", now.AddDays(-4).AddHours(-3));
            var t8 = Tube(r8, "EDTA (purple top)", "EDTA", TubeStatus.Verified, now.AddDays(-4), "1", now.AddDays(-4).AddMinutes(30));
            var i8a = Test(r8, t8, "Haemoglobin", TestStage.Verified, 180);
            var i8b = Test(r8, t8, "Platelet count", TestStage.Verified, 240);
            Captured(i8a, "1", now.AddDays(-4).AddHours(2), Line("Haemoglobin", "11.2", "g/dL", "12.0 – 15.5", "Low"));
            Captured(i8b, "1", now.AddDays(-4).AddHours(2), Line("Platelet count", "268", "x10^9/L", "150 – 400", "Normal"));
            Verified(i8a, "2", now.AddDays(-4).AddHours(3));
            Verified(i8b, "2", now.AddDays(-4).AddHours(3));

            // 9. Already released by the doctor.
            var r9 = Request("REQ-20261010-909", sarah, "Dr S. Govender", "Annual screen.", "Routine", now.AddDays(-10));
            var t9 = Tube(r9, "EDTA (purple top)", "EDTA", TubeStatus.Verified, now.AddDays(-10).AddHours(1), "1", now.AddDays(-10).AddHours(2));
            var i9 = Test(r9, t9, "Haemoglobin", TestStage.Verified, 180);
            Captured(i9, "2", now.AddDays(-10).AddHours(4), Line("Haemoglobin", "13.1", "g/dL", "12.0 – 15.5", "Normal"));
            Verified(i9, "1", now.AddDays(-10).AddHours(5));
            r9.ReleasedBy = "Doctor";
            r9.ReleasedAt = now.AddDays(-9);

            list.AddRange(new[] { r1, r2, r3, r4, r5, r6, r7, r8, r9 });

            // a plain audit trail for every request, tube and test, built from the dates above
            foreach (var r in list)
            {
                r.Log("Request submitted", null, at: r.RequestedAt);
                foreach (var tube in r.Tubes)
                {
                    if (tube.Status == TubeStatus.NotReceived) continue;
                    tube.Log("Sample received", null, detail: $"{tube.Type} · {tube.SampleType}", at: tube.ReceivedAt);
                    if (tube.Status == TubeStatus.Verified) tube.Log("Sample verified", tube.VerifiedBy, EventTone.Good, "All checks passed", tube.VerifiedAt);
                }
                foreach (var t in r.Tests)
                {
                    if (t.AssignedTo != null && t.Stage != TestStage.AwaitingAssignment)
                        t.Log("Test picked up", t.AssignedTo, at: t.Sample.ReceivedAt.AddMinutes(40));
                    foreach (var h in t.History)
                        t.Log("Result returned to technician", "2", tone: EventTone.Warn, detail: h.Note, attempt: 1, at: h.CapturedAt?.AddHours(1));
                    if (t.CapturedAt.HasValue)
                        t.Log("Result captured", t.CapturedBy, detail: TestItem.Summarise(t.Results), attempt: t.Attempt, at: t.CapturedAt);
                    if (t.Stage == TestStage.Verified && t.VerifiedAt.HasValue)
                        t.Log("Result verified", t.VerifiedBy, tone: EventTone.Good, attempt: t.Attempt, at: t.VerifiedAt);
                }
                if (r.IsReleased) r.Log("Results released", r.ReleasedBy, EventTone.Good, at: r.ReleasedAt);
            }
            return list;
        }

        private static PatientInfo Patient(string id, string first, string last, int age, string sex, string conditions,
                                           string allergies, bool? pregnant, params Medication[] meds)
        {
            var p = new PatientInfo
            {
                PatientId = id, FirstName = first, Surname = last, Sex = sex,
                DateOfBirth = DateTime.Today.AddYears(-age).AddDays(-100),
                Conditions = conditions, Allergies = allergies, IsPregnant = pregnant,
                FastingStatus = "Not required", Precautions = "None flagged"
            };
            p.Medications.AddRange(meds);
            return p;
        }

        private static LabRequest Request(string num, PatientInfo patient, string doctor, string notes, string priority, DateTime at)
            => new()
            {
                RequestId = ++_nextId, ReqNum = num, Patient = patient, RequestingDoctor = doctor, Ward = "Not recorded",
                ClinicalNotes = notes, Priority = priority, IsUrgent = priority != "Routine", RequestedAt = at
            };

        private static SampleTube Tube(LabRequest r, string type, string sampleType, TubeStatus status, DateTime received,
                                       string verifiedBy = null, DateTime? verifiedAt = null)
        {
            var t = new SampleTube
            {
                BarcodeId = ++_nextTube, TubeId = "BC-" + _nextTube.ToString("0000"), Type = type, SampleType = sampleType,
                Status = status, CollectedAt = received.AddMinutes(-30), ReceivedAt = received, Request = r,
                Condition = status == TubeStatus.Verified ? "Acceptable" : "Not yet verified",
                VerifiedBy = verifiedBy, VerifiedAt = verifiedAt
            };
            r.Tubes.Add(t);
            return t;
        }

        private static TestItem Test(LabRequest r, SampleTube tube, string name, TestStage stage, int targetMinutes)
        {
            var id = ++_nextId;
            var i = new TestItem
            {
                TestId = id.ToString(), ItemId = id, TestTypeId = id, Request = r, Sample = tube, Test = name, Stage = stage,
                TargetMinutes = targetMinutes, PreviousResult = "None on record"
            };
            r.Tests.Add(i);
            tube.Tests.Add(i);
            return i;
        }

        private static void Hold(TestItem t, string tech, DateTime at)
        {
            t.AssignedTo = tech;
            t.AssignedToMe = tech == CurrentUser.Id;
        }

        private static ResultLine Line(string name, string value, string unit, string range, string flag)
            => new() { Parameter = name, Value = value, Unit = unit, Range = range, Flag = flag };

        private static void Captured(TestItem t, string tech, DateTime at, params ResultLine[] lines)
        {
            t.Results = lines.ToList();
            t.CapturedBy = tech;
            t.CapturedAt = at;
            Hold(t, tech, at);
        }

        private static void Verified(TestItem t, string tech, DateTime at)
        {
            t.VerifiedBy = tech;
            t.VerifiedAt = at;
        }

        // ------------------------------------------------------------------ the writes (same rules as the stored procedures)

        private static bool IsMe(string by) => by == CurrentUser.Id;

        public static bool Accept(SampleTube tube, string by)
        {
            lock (Gate)
            {
                if (tube.Status != TubeStatus.AwaitingVerification) return false;
                tube.Status = TubeStatus.Verified; tube.Condition = "Acceptable"; tube.VerifiedBy = by; tube.VerifiedAt = DateTime.Now;
                foreach (var t in tube.Tests.Where(x => x.Stage == TestStage.AwaitingSampleVerification)) t.Stage = TestStage.AwaitingAssignment;
                return true;
            }
        }

        public static bool Reject(SampleTube tube, string by, string condition, string reason)
        {
            lock (Gate)
            {
                if (tube.Status != TubeStatus.AwaitingVerification) return false;
                tube.Status = TubeStatus.Rejected; tube.Condition = condition; tube.RejectReason = reason; tube.VerifiedBy = by; tube.VerifiedAt = DateTime.Now;
                foreach (var t in tube.Tests.Where(x => x.Stage == TestStage.AwaitingSampleVerification)) t.Stage = TestStage.SampleRejected;
                return true;
            }
        }

        public static bool PickUp(TestItem item, string by)
        {
            lock (Gate)
            {
                if (item.Stage != TestStage.AwaitingAssignment || item.AssignedTo != null) return false;
                item.Stage = TestStage.Assigned; item.AssignedTo = by; item.AssignedToMe = IsMe(by);
                return true;
            }
        }

        public static bool ReturnToPool(TestItem item, string by)
        {
            lock (Gate)
            {
                if (item.Stage != TestStage.Assigned || item.AssignedTo != by) return false;
                item.Stage = TestStage.AwaitingAssignment; item.AssignedTo = null; item.AssignedToMe = false;
                return true;
            }
        }

        public static bool SaveDraft(TestItem item)
        {
            lock (Gate)
            {
                if (item.Stage is not (TestStage.Assigned or TestStage.InProgress or TestStage.ToBeReviewed)) return false;
                if (item.Stage != TestStage.ToBeReviewed) item.Stage = TestStage.InProgress;
                return true;
            }
        }

        public static bool Submit(TestItem item, string by)
        {
            lock (Gate)
            {
                if (item.Stage is not (TestStage.Assigned or TestStage.InProgress or TestStage.ToBeReviewed)) return false;
                item.Stage = TestStage.Complete; item.AssignedTo = by; item.AssignedToMe = IsMe(by);
                item.CapturedBy = by; item.CapturedAt = DateTime.Now;
                return true;
            }
        }

        public static bool Approve(TestItem item, string by)
        {
            lock (Gate)
            {
                if (item.Stage != TestStage.Complete || item.AssignedTo == by) return false;
                item.Stage = TestStage.Verified; item.VerifiedBy = by; item.VerifiedAt = DateTime.Now;
                return true;
            }
        }

        public static bool Return(TestItem item, string by, string note)
        {
            lock (Gate)
            {
                if (item.Stage != TestStage.Complete) return false;
                item.History.Add(new ReturnedAttempt
                {
                    Note = note, CapturedBy = item.CapturedBy, CapturedAt = item.CapturedAt,
                    Results = item.Results.Select(l => new ResultLine { Parameter = l.Parameter, Value = l.Value, Unit = l.Unit, Range = l.Range, Flag = l.Flag }).ToList()
                });
                item.Stage = TestStage.ToBeReviewed; item.ReviewNote = note; item.VerifiedBy = null; item.VerifiedAt = null;
                return true;
            }
        }
    }
}
