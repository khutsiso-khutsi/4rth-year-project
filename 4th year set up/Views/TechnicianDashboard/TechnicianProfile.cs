// Put this file in your Models folder.
// Sample data below is FICTIONAL and held in memory (like TestData) until the user table is connected.
// Replace TechnicianProfileStore.Get / Update with database calls.
using System;
using System.Collections.Generic;
using System.Linq;

namespace _4th_year_set_up.Models
{
    public class RecentWorkItem
    {
        public string ReqNum { get; set; }
        public string Test { get; set; }
        public string Patient { get; set; }
        public string Action { get; set; }
        public DateTime At { get; set; }
    }

    public class TechnicianProfileViewModel
    {
        // ---- Editable by the technician
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CellphoneNumber { get; set; }

        // ---- Account (read-only)
        public string EmployeeNumber { get; set; }
        public string IdNumber { get; set; }
        public string Email { get; set; }

        // ---- Laboratory details (set by the laboratory manager, read-only here)
        public string Department { get; set; }
        public string Supervisor { get; set; }
        public string RegistrationNumber { get; set; }   // professional registration (e.g. HPCSA)
        public string Shift { get; set; }
        public DateTime DateJoined { get; set; }

        // ---- Authorisations (come from CurrentUser)
        public bool IsSenior { get; set; }
        public bool CanVerifySamples { get; set; }
        public List<string> VerifiableTests { get; set; } = new();

        // ---- Activity (worked out from TestData)
        public int InQueueCount { get; set; }
        public int CapturedTodayCount { get; set; }
        public int ResultsVerifiedTodayCount { get; set; }
        public int SamplesVerifiedTodayCount { get; set; }
        public int AwaitingMyVerificationCount { get; set; }
        public List<RecentWorkItem> Recent { get; set; } = new();

        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Role => IsSenior ? "Senior Laboratory Technician" : "Laboratory Technician";

        // Shows only the last 4 digits of the ID number.
        public string MaskedIdNumber
        {
            get
            {
                var id = (IdNumber ?? "").Trim();
                return id.Length <= 4 ? id : new string('•', id.Length - 4) + id.Substring(id.Length - 4);
            }
        }
    }

    public static class TechnicianProfileStore
    {
        static readonly TechnicianProfileViewModel stored = new()
        {
            FirstName = "Zodwa",
            LastName = "Maseko",
            CellphoneNumber = "082 000 0000",
            EmployeeNumber = CurrentUser.Id,
            IdNumber = "0000000000000",
            Email = "technician@example.com",
            Department = "Haematology",
            Supervisor = "Laboratory Manager",
            RegistrationNumber = "MT 0000000",
            Shift = "Day shift (07:00 – 16:00)",
            DateJoined = new DateTime(2024, 2, 1)
        };

        public static TechnicianProfileViewModel Get(string email)
        {
            var p = stored;
            if (!string.IsNullOrWhiteSpace(email)) p.Email = email;

            p.IsSenior = CurrentUser.IsSenior;
            p.CanVerifySamples = CurrentUser.CanVerifySample;
            p.VerifiableTests = CurrentUser.VerifiableTests.OrderBy(x => x).ToList();

            var today = DateTime.Today;
            var all = TestData.All;
            p.InQueueCount = all.Count(t => t.IsInQueue && t.AssignedToMe);
            p.CapturedTodayCount = all.Count(t => t.CapturedBy == CurrentUser.Id && t.CapturedAt?.Date == today);
            p.ResultsVerifiedTodayCount = all.Count(t => t.VerifiedBy == CurrentUser.Id && t.VerifiedAt?.Date == today);
            p.SamplesVerifiedTodayCount = all.Count(t => t.SampleVerifiedBy == CurrentUser.Id && t.SampleVerifiedAt?.Date == today);
            p.AwaitingMyVerificationCount = all.Count(CurrentUser.CanVerifyResult);

            var recent = new List<RecentWorkItem>();
            foreach (var t in all)
            {
                if (t.CapturedBy == CurrentUser.Id && t.CapturedAt.HasValue)
                    recent.Add(new RecentWorkItem { ReqNum = t.ReqNum, Test = t.Test, Patient = t.Patient, Action = "Captured result", At = t.CapturedAt.Value });
                if (t.VerifiedBy == CurrentUser.Id && t.VerifiedAt.HasValue)
                    recent.Add(new RecentWorkItem { ReqNum = t.ReqNum, Test = t.Test, Patient = t.Patient, Action = "Verified result", At = t.VerifiedAt.Value });
                if (t.SampleVerifiedBy == CurrentUser.Id && t.SampleVerifiedAt.HasValue)
                    recent.Add(new RecentWorkItem { ReqNum = t.ReqNum, Test = t.Test, Patient = t.Patient, Action = "Verified sample", At = t.SampleVerifiedAt.Value });
            }
            p.Recent = recent.OrderByDescending(r => r.At).Take(8).ToList();
            return p;
        }

        public static void Update(string firstName, string lastName, string cellphoneNumber)
        {
            stored.FirstName = firstName;
            stored.LastName = lastName;
            stored.CellphoneNumber = cellphoneNumber;
        }
    }
}
