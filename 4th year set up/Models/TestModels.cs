using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace _4th_year_set_up.Models
{
    // Lifecycle of a test:
    // Sample received -> AwaitingSampleVerification -> (SampleRejected | AwaitingAssignment)
    //   -> Assigned -> InProgress -> Complete (results captured)
    //   -> Verified (second technician) | ToBeReviewed (returned to the technician with a note)
    public enum TestStage
    {
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

    public class TestItem
    {
        public string ReqNum { get; set; }
        public PatientInfo PatientDetails { get; set; }
        public string Test { get; set; }
        public bool IsUrgent { get; set; }
        public TestStage Stage { get; set; }
        public bool AssignedToMe { get; set; }
        public TatStatus Tat { get; set; }
        public string DueText { get; set; }

        // Request / sample
        public string RequestingDoctor { get; set; }
        public string Ward { get; set; }
        public string SampleType { get; set; }
        public string Tube { get; set; }
        public DateTime CollectedAt { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string SampleCondition { get; set; }
        public string ClinicalNotes { get; set; }
        public string PreviousResult { get; set; }

        // Sample verification (senior technician, before the test can be picked up)
        public string SampleVerifiedBy { get; set; }
        public DateTime? SampleVerifiedAt { get; set; }
        public string SampleRejectReason { get; set; }

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

        // ---- Audit trail: append-only. Events are only ever added, never edited or removed.
        // Drafts are not logged; only real state changes (received, verified, picked up, captured, returned, released...).
        public List<TestEvent> Events { get; set; } = new();

        // 1 for the first capture; goes up by one each time a verifier returns the result.
        public int Attempt => History.Count + 1;

        // Newest first. Ties keep the order they were logged in.
        public IEnumerable<TestEvent> Timeline => Events
            .Select((e, i) => (e, i))
            .OrderByDescending(x => x.e.At).ThenByDescending(x => x.i)
            .Select(x => x.e);

        public TestEvent LastEvent => Timeline.FirstOrDefault();

        public TestEvent Log(string action, string by, EventScope scope = EventScope.Test, EventTone tone = EventTone.Neutral,
                             string detail = null, int? attempt = null, DateTime? at = null, List<ValueChange> changes = null)
        {
            var e = new TestEvent
            {
                At = at ?? DateTime.Now, By = by, Action = action, Scope = scope, Tone = tone,
                Detail = detail, Attempt = attempt, Changes = changes ?? new List<ValueChange>()
            };
            Events.Add(e);
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
            TestStage.AwaitingSampleVerification => "Awaiting sample verification",
            TestStage.SampleRejected => "Sample rejected",
            TestStage.AwaitingAssignment => "Awaiting assignment",
            TestStage.Assigned => "Assigned",
            TestStage.InProgress => "In progress",
            TestStage.Complete => "Complete",
            TestStage.ToBeReviewed => "To be reviewed",
            TestStage.Verified => "Verified",
            _ => ""
        };

        public string StageCss => Stage switch
        {
            TestStage.AwaitingSampleVerification => "badge-pending",
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

    // What the event is about. A sample (tube) or request event is shared by every test that depends on it;
    // a test event belongs to one test only. Today each request holds one test, so these all sit on the same item.
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
    }

    public static class SampleChecklist
    {
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
            "Other (give details)"
        };
    }

    // STUB for the signed-in user until authentication/roles come from the database.
    // VerifiableTests = the test types the laboratory manager has assigned to this technician.
    public static class CurrentUser
    {
        public const string Id = "T-009";
        public const string Name = "Technician T-009";
        public static bool IsSenior = true;
        public static readonly HashSet<string> VerifiableTests = new()
        {
            "Full Blood Count", "Haemoglobin", "Platelet Count", "ESR"   // not WBC Differential (demo of the rule)
        };

        public static bool CanVerifySample => IsSenior;

        // Rules from the specification:
        //  - a technician cannot verify their own results
        //  - a technician can only verify test types assigned to them by the laboratory manager
        public static string WhyCannotVerifyResult(TestItem t)
        {
            if (!IsSenior) return "Only senior technicians can verify results.";
            if (t.Stage != TestStage.Complete) return "This result is not waiting for verification.";
            if (t.CapturedBy == Id) return "You captured this result, so you can't verify it. A different technician must.";
            if (!VerifiableTests.Contains(t.Test)) return $"{t.Test} is not one of the test types assigned to you by the laboratory manager.";
            return null;
        }

        public static bool CanVerifyResult(TestItem t) => WhyCannotVerifyResult(t) == null;
    }

    // HARDCODED SAMPLE DATA (fictional patients) until the database is connected.
    // Replace TestData.All with a repository / EF query.
    public static class TestData
    {
        static Medication Med(string name, string dose, string note) => new() { Name = name, Dose = dose, LabNote = note };
        static DateTime Today(double hours) => DateTime.Today.AddHours(hours);
        static ResultLine Line(string parameter, string value, string unit, string range, string flag)
            => new() { Parameter = parameter, Value = value, Unit = unit, Range = range, Flag = flag };

        static PatientInfo Pat(string id, string first, string last, DateTime dob, string sex, string conditions,
                               string allergies, string precautions, bool? pregnant, params Medication[] meds)
        {
            var p = new PatientInfo
            {
                PatientId = id, FirstName = first, Surname = last, DateOfBirth = dob, Sex = sex,
                Conditions = conditions, Allergies = allergies, Precautions = precautions,
                IsPregnant = pregnant, FastingStatus = "Not required"
            };
            p.Medications.AddRange(meds);
            return p;
        }

        static TestItem NewTest(string req, string test, bool urgent, TestStage stage, bool mine, TatStatus tat, string due,
                          PatientInfo pt, string doctor, string ward, double collected, double received,
                          string sampleCondition, string notes, string previous)
            => new()
            {
                ReqNum = req, Test = test, IsUrgent = urgent, Stage = stage, AssignedToMe = mine, Tat = tat, DueText = due,
                PatientDetails = pt, RequestingDoctor = doctor, Ward = ward, SampleType = "Whole blood", Tube = "EDTA (purple top)",
                CollectedAt = Today(collected), ReceivedAt = Today(received), SampleCondition = sampleCondition,
                ClinicalNotes = notes, PreviousResult = previous
            };

        public static readonly List<TestItem> All = Build();

        static List<TestItem> Build()
        {
            var list = new List<TestItem>
            {
                // ---- Awaiting sample verification (senior technician) ----
                NewTest("REQ-0150", "Full Blood Count", false, TestStage.AwaitingSampleVerification, false, TatStatus.OnTrack, "Due in 4h",
                    Pat("PT-100511", "Naledi", "Mahlangu", new DateTime(1990, 4, 11), "Female", "Fatigue; query anaemia", "None known", "None flagged", false),
                    "Dr P. Mthembu", "Outpatients", 10.2, 10.5, "Not yet verified", "Tired for 3 months.", "Hb 11.8 g/dL (6 months ago)"),
                NewTest("REQ-0149", "Haemoglobin", true, TestStage.AwaitingSampleVerification, false, TatStatus.NearingTat, "Due in 35m",
                    Pat("PT-100498", "Johan", "Pretorius", new DateTime(1963, 7, 25), "Male", "Upper GI bleed", "None known", "None flagged", null,
                        Med("Rivaroxaban", "20 mg daily", "Anticoagulant; bleeding risk")),
                    "Dr N. Pillay", "Emergency Unit", 10.0, 10.4, "Not yet verified", "Melaena since last night. Phone result.", "Hb 13.2 g/dL (2 months ago)"),
                NewTest("REQ-0151", "Platelet Count", false, TestStage.SampleRejected, false, TatStatus.OnTrack, "Recollection requested",
                    Pat("PT-100520", "Tumelo", "Sithebe", new DateTime(1979, 2, 3), "Male", "Easy bruising", "None known", "None flagged", null),
                    "Dr A. Botha", "Haematology Clinic", 8.9, 9.3, "Clotted", "Query thrombocytopenia.", "None on record"),

                // ---- Awaiting assignment (sample verified) ----
                NewTest("REQ-0148", "Full Blood Count", true, TestStage.AwaitingAssignment, false, TatStatus.Overdue, "Overdue by 1h 20m",
                    Pat("PT-100482", "Thabo", "Nkosi", new DateTime(1981, 3, 14), "Male", "Suspected sepsis; type 2 diabetes", "Penicillin (rash)", "None flagged", null,
                        Med("Metformin", "500 mg BD", "No direct effect on FBC"), Med("Ceftriaxone", "1 g IV", "Started this morning; may affect WBC trend")),
                    "Dr N. Pillay", "Emergency Unit", 7.5, 7.9, "Acceptable", "Fever 39.2°C, tachycardic. Phone results to ED if WBC outside normal range.", "WBC 14.8 ×10³/µL (3 days ago)"),
                NewTest("REQ-0145", "Platelet Count", false, TestStage.AwaitingAssignment, false, TatStatus.NearingTat, "Due in 40m",
                    Pat("PT-100377", "Nomsa", "Ndlovu", new DateTime(1989, 5, 30), "Female", "Immune thrombocytopenia (ITP)", "Sulfa drugs", "None flagged", false,
                        Med("Prednisone", "40 mg daily", "Expected to raise platelets over time")),
                    "Dr A. Botha", "Haematology Clinic", 9.3, 9.6, "Acceptable", "Bruising reported. Confirm any low count with a smear.", "PLT 22 ×10³/µL (last week)"),
                NewTest("REQ-0143", "WBC Differential", false, TestStage.AwaitingAssignment, false, TatStatus.OnTrack, "Due tomorrow",
                    Pat("PT-100390", "Sipho", "Khumalo", new DateTime(2001, 6, 17), "Male", "Recurrent infections; HIV positive (on ART)", "None known", "Standard precautions", null,
                        Med("Tenofovir/Lamivudine/Dolutegravir", "1 tablet daily", "ART; monitor for cytopenias")),
                    "Dr T. Sithole", "Outpatients", 9.8, 10.1, "Acceptable", "Check lymphocyte and neutrophil counts.", "Neutrophils 1.6 ×10³/µL (2 months ago)"),

                // ---- My tests (assigned / in progress / returned) ----
                NewTest("REQ-0147", "Haemoglobin", true, TestStage.InProgress, true, TatStatus.NearingTat, "Due in 25m",
                    Pat("PT-100455", "Zanele", "Dlamini", new DateTime(1994, 8, 2), "Female", "Postpartum haemorrhage (day 1); iron-deficiency anaemia", "None known", "None flagged", false,
                        Med("Ferrous sulphate", "200 mg TDS", "Iron therapy; expect gradual Hb response"), Med("Oxytocin", "Infusion", "Obstetric; no lab interference")),
                    "Dr L. Moyo", "Maternity Ward B", 8.2, 8.5, "Acceptable", "Pallor and dizziness. Transfusion decision depends on this Hb — phone result.", "Hb 7.9 g/dL (yesterday)"),
                NewTest("REQ-0146", "ESR", false, TestStage.Assigned, true, TatStatus.OnTrack, "Due in 3h",
                    Pat("PT-100301", "Lerato", "Mokoena", new DateTime(1967, 11, 21), "Female", "Rheumatoid arthritis", "None known", "None flagged", false,
                        Med("Methotrexate", "15 mg weekly", "May cause cytopenias; interpret counts with care"), Med("Prednisone", "5 mg daily", "Can raise neutrophil count and lower ESR")),
                    "Dr S. Govender", "Outpatients", 9.0, 9.4, "Acceptable", "Routine disease-activity monitoring.", "ESR 38 mm/hr (3 months ago)"),
                NewTest("REQ-0144", "Full Blood Count", false, TestStage.Assigned, true, TatStatus.Overdue, "Overdue by 15m",
                    Pat("PT-100219", "Pieter", "van Wyk", new DateTime(1955, 1, 9), "Male", "Atrial fibrillation; chronic kidney disease stage 3", "None known", "None flagged", null,
                        Med("Warfarin", "5 mg daily", "Anticoagulant; relevant to bleeding risk and any coagulation tests"), Med("Amlodipine", "10 mg daily", "No lab interference")),
                    "Dr R. Naidoo", "Medical Ward 3", 7.0, 7.6, "Acceptable", "Mild anaemia on last visit. Flag Hb below 10 g/dL.", "Hb 10.4 g/dL (2 weeks ago)"),
                NewTest("REQ-0138", "ESR", false, TestStage.ToBeReviewed, true, TatStatus.NearingTat, "Returned for review",
                    Pat("PT-100061", "Dumisani", "Ngcobo", new DateTime(1958, 10, 5), "Male", "Polymyalgia rheumatica", "None known", "None flagged", null,
                        Med("Prednisone", "15 mg daily", "Can lower ESR")),
                    "Dr S. Govender", "Outpatients", 6.8, 7.1, "Acceptable", "Monitoring on steroids.", "ESR 22 mm/hr (1 month ago)"),

                // ---- Complete: waiting for a second technician to verify ----
                NewTest("REQ-0142", "Full Blood Count", false, TestStage.Complete, false, TatStatus.OnTrack, "Captured 09:50",
                    Pat("PT-100533", "Mandla", "Cele", new DateTime(1985, 9, 19), "Male", "Pre-operative screening", "None known", "None flagged", null),
                    "Dr K. Reddy", "Surgical Pre-admission", 7.2, 7.6, "Acceptable", "Elective surgery next week.", "None on record"),
                NewTest("REQ-0139", "Haemoglobin", false, TestStage.Complete, true, TatStatus.OnTrack, "Captured 09:20",
                    Pat("PT-100347", "Karabo", "Molefe", new DateTime(1999, 1, 28), "Female", "Iron-deficiency anaemia", "None known", "None flagged", false,
                        Med("Ferrous sulphate", "200 mg daily", "Iron therapy")),
                    "Dr L. Moyo", "Outpatients", 7.4, 7.8, "Acceptable", "Follow-up Hb.", "Hb 8.6 g/dL (1 month ago)"),
                NewTest("REQ-0152", "WBC Differential", false, TestStage.Complete, false, TatStatus.OnTrack, "Captured 09:05",
                    Pat("PT-100560", "Fatima", "Hendricks", new DateTime(1978, 6, 8), "Female", "Recurrent infections", "None known", "None flagged", false),
                    "Dr T. Sithole", "Outpatients", 7.1, 7.5, "Acceptable", "Query immune deficiency.", "Lymphocytes 1.9 ×10³/µL (3 months ago)"),

                // ---- Verified ----
                NewTest("REQ-0141", "Haemoglobin", false, TestStage.Verified, false, TatStatus.OnTrack, "Verified 09:42",
                    Pat("PT-100122", "Aisha", "Patel", new DateTime(1976, 9, 4), "Female", "Heavy menstrual bleeding", "None known", "None flagged", false,
                        Med("Tranexamic acid", "1 g TDS", "No direct effect on Hb")),
                    "Dr K. Reddy", "Outpatients", 6.9, 7.2, "Acceptable", "Follow-up Hb.", "Hb 10.1 g/dL (1 month ago)"),
                NewTest("REQ-0140", "ESR", false, TestStage.Verified, false, TatStatus.OnTrack, "Verified 08:15",
                    Pat("PT-100098", "Bongani", "Zulu", new DateTime(1972, 12, 12), "Male", "Pulmonary tuberculosis (on treatment)", "None known",
                        "TB — standard + airborne precautions at collection", null,
                        Med("RHZE fixed-dose combination", "4 tablets daily", "TB therapy; can affect liver and counts")),
                    "Dr M. Dube", "Medical Ward 1", 6.5, 6.9, "Acceptable", "Treatment response monitoring.", "ESR 62 mm/hr (1 month ago)"),
            };

            TestItem Get(string req) => list.First(x => x.ReqNum == req);

            void Captured(string req, string by, double hour, params ResultLine[] lines)
            {
                var t = Get(req);
                t.CapturedBy = by; t.CapturedAt = Today(hour); t.Results = lines.ToList();
            }

            Captured("REQ-0142", "T-004", 9.8,
                Line("WBC", "6.8", "×10³/µL", "4.0 – 11.0", "Normal"),
                Line("Haemoglobin", "13.9", "g/dL", "13.0 – 17.0", "Normal"),
                Line("Platelets", "251", "×10³/µL", "150 – 400", "Normal"));
            Captured("REQ-0139", CurrentUser.Id, 9.3,
                Line("Haemoglobin", "9.4", "g/dL", "12.0 – 15.5", "Low"));
            Captured("REQ-0152", "T-004", 9.1,
                Line("Neutrophils", "5.1", "×10³/µL", "1.8 – 7.7", "Normal"),
                Line("Lymphocytes", "2.2", "×10³/µL", "1.0 – 4.8", "Normal"));
            Captured("REQ-0138", CurrentUser.Id, 8.7,
                Line("ESR", "120", "mm/hr", "0 – 20", "High"));
            Get("REQ-0138").ReviewNote = "ESR of 120 mm/hr doesn't fit the clinical picture or the previous result (22). Please repeat on a fresh aliquot.";
            Get("REQ-0138").History.Add(new ReturnedAttempt
            {
                Note = Get("REQ-0138").ReviewNote,
                CapturedBy = CurrentUser.Id,
                CapturedAt = Today(8.7),
                Results = new List<ResultLine> { Line("ESR", "120", "mm/hr", "0 – 15", "High") }
            });

            Captured("REQ-0141", "T-004", 9.4, Line("Haemoglobin", "11.2", "g/dL", "12.0 – 15.5", "Low"));
            Captured("REQ-0140", "T-004", 7.9, Line("ESR", "48", "mm/hr", "0 – 15", "High"));
            foreach (var r in new[] { "REQ-0141", "REQ-0140" })
            {
                Get(r).VerifiedBy = "T-003";
                Get(r).VerifiedAt = r == "REQ-0141" ? Today(9.7) : Today(8.25);
            }

            Get("REQ-0151").SampleRejectReason = "Sample clotted. Recollection requested from Haematology Clinic.";

            // Every test past sample verification was verified by a senior technician earlier today
            foreach (var t in list.Where(x => x.Stage != TestStage.AwaitingSampleVerification && x.Stage != TestStage.SampleRejected))
            {
                t.SampleVerifiedBy = "T-004";
                t.SampleVerifiedAt = t.ReceivedAt.AddMinutes(20);
            }

            foreach (var t in list) SeedHistory(t);
            return list;
        }

        // Builds each test's audit trail from the sample data above so the History card has something real to show.
        // With the database these are simply the stored events.
        static void SeedHistory(TestItem t)
        {
            t.Log("Sample received", "T-003", EventScope.Request, EventTone.Neutral,
                  detail: $"{t.Tube} · {t.SampleType}", at: t.ReceivedAt);

            if (t.Stage == TestStage.SampleRejected)
            {
                t.Log("Sample rejected", "T-004", EventScope.Sample, EventTone.Bad,
                      detail: t.SampleRejectReason, at: t.ReceivedAt.AddMinutes(15));
                return;
            }
            if (t.Stage == TestStage.AwaitingSampleVerification) return;

            t.Log("Sample verified", t.SampleVerifiedBy, EventScope.Sample, EventTone.Good,
                  detail: "All 6 checks passed", at: t.SampleVerifiedAt);
            if (t.Stage == TestStage.AwaitingAssignment) return;

            var who = t.CapturedBy ?? CurrentUser.Id;
            var verifiedAt = t.SampleVerifiedAt ?? t.ReceivedAt;
            var earliest = verifiedAt.AddMinutes(10);
            var wanted = (t.CapturedAt ?? verifiedAt.AddMinutes(60)).AddMinutes(-45);
            t.Log("Test picked up", who, at: wanted > earliest ? wanted : earliest);

            if (!t.CapturedAt.HasValue) return;

            t.Log("Result captured", t.CapturedBy, detail: TestItem.Summarise(t.Results), attempt: 1, at: t.CapturedAt);

            if (t.Stage == TestStage.ToBeReviewed)
                t.Log("Result returned to technician", "T-003", tone: EventTone.Warn,
                      detail: t.ReviewNote, attempt: 1, at: t.CapturedAt.Value.AddMinutes(40));

            if (t.Stage == TestStage.Verified)
                t.Log("Result verified", t.VerifiedBy, tone: EventTone.Good, attempt: 1, at: t.VerifiedAt);
        }
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

        // Senior technician tasks
        public bool IsSenior { get; set; }
        public int SamplesToVerifyCount { get; set; }
        public int ResultsToVerifyCount { get; set; }

        // All of this technician's tests in the queue for the dashboard card (most pressing first)
        public List<TestItem> MyTestsPreview { get; set; } = new();

        public static DashboardViewModel Build()
        {
            var queue = TestData.All.Where(t => t.IsInQueue).ToList();
            return new DashboardViewModel
            {
                MyTestsPreview = queue
                    .Where(t => t.AssignedToMe)
                    .OrderByDescending(t => t.Stage == TestStage.ToBeReviewed)
                    .ThenByDescending(t => t.Tat == TatStatus.Overdue)
                    .ThenByDescending(t => t.Tat == TatStatus.NearingTat)
                    .ThenByDescending(t => t.IsUrgent)
                    .ToList(),
                QueueCount = queue.Count,
                MineCount = queue.Count(t => t.AssignedToMe),
                AwaitingCount = TestData.All.Count(t => t.Stage == TestStage.AwaitingAssignment),
                VerifiedCount = TestData.All.Count(t => t.Stage == TestStage.Verified),
                UrgentCount = queue.Count(t => t.IsUrgent),
                OverdueCount = queue.Count(t => t.Tat == TatStatus.Overdue),
                NearingCount = queue.Count(t => t.Tat == TatStatus.NearingTat),
                IsSenior = CurrentUser.IsSenior,
                SamplesToVerifyCount = TestData.All.Count(t => t.Stage == TestStage.AwaitingSampleVerification),
                ResultsToVerifyCount = TestData.All.Count(CurrentUser.CanVerifyResult),
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
}


/*SELECT t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.IS_NULLABLE
FROM INFORMATION_SCHEMA.TABLES t
JOIN INFORMATION_SCHEMA.COLUMNS c ON c.TABLE_NAME = t.TABLE_NAME
WHERE t.TABLE_TYPE = 'BASE TABLE'
ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION;*/