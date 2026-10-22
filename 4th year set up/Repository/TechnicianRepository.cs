using _4th_year_set_up.DataAccess;
using _4th_year_set_up.Models;

namespace _4th_year_set_up.Repository
{
    // Same pattern as AdminRepository: the controller talks to the repository,
    // the repository talks to the data access class, which calls the stored procedures.
    public class TechnicianRepository
    {
        private readonly TechnicianDataAccess _dataAccess;

        public TechnicianRepository(string connectionString, bool useSampleData = false)
        {
            _dataAccess = new TechnicianDataAccess(connectionString, useSampleData);
        }

        public bool IsSampleMode => _dataAccess.IsSampleMode;

        // ── READ ──────────────────────────────────────────────────────
        public List<LabRequest> GetRequests() => _dataAccess.GetRequests();
        public List<TestItem> GetTests() => _dataAccess.GetTests();
        public LabRequest GetRequest(string reqNum) => _dataAccess.GetRequest(reqNum);
        public TestItem GetTest(string id) => _dataAccess.GetTest(id);
        public HashSet<string> GetAssignedTestNames(string technicianId) => _dataAccess.GetAssignedTestNames(technicianId);

        // ── SAMPLES ───────────────────────────────────────────────────
        public bool AcceptSample(SampleTube tube, string by, TestEvent ev) => _dataAccess.AcceptSample(tube, by, ev);
        public bool RejectSample(SampleTube tube, string by, string condition, string reason, TestEvent ev) => _dataAccess.RejectSample(tube, by, condition, reason, ev);

        // ── TESTS ─────────────────────────────────────────────────────
        public bool PickUpTest(TestItem item, string by, TestEvent ev) => _dataAccess.PickUpTest(item, by, ev);
        public bool ReturnToPool(TestItem item, string by, TestEvent ev) => _dataAccess.ReturnToPool(item, by, ev);

        // ── RESULTS ───────────────────────────────────────────────────
        public bool SaveDraft(TestItem item, string by, int attempt, string captureNote) => _dataAccess.SaveDraft(item, by, attempt, captureNote);
        public bool SubmitResults(TestItem item, string by, int attempt, string captureNote, IEnumerable<TestEvent> events) => _dataAccess.SubmitResults(item, by, attempt, captureNote, events);
        public bool ApproveResult(TestItem item, string by, TestEvent ev) => _dataAccess.ApproveResult(item, by, ev);
        public bool ReturnResult(TestItem item, string by, string note, TestEvent ev) => _dataAccess.ReturnResult(item, by, note, ev);

        // ── PROFILE ───────────────────────────────────────────────────
        public TechProfileViewModel GetProfile(string technicianId) => _dataAccess.GetProfile(technicianId);
        public bool UpdateProfile(string technicianId, string firstName, string lastName, string contactNumber) => _dataAccess.UpdateProfile(technicianId, firstName, lastName, contactNumber);
        public string ChangePassword(string technicianId, string currentPassword, string newPassword) => _dataAccess.ChangePassword(technicianId, currentPassword, newPassword);
    }
}
