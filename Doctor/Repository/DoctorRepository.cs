using Doctor.DataAccess;
using Doctor.Models;

namespace Doctor.Repository
{
    /// <summary>
    /// Thin pass-through wrapper over DoctorDataAccess, mirroring the
    /// Patient.Repository.UserRepository pattern: the controller depends on
    /// this repository (constructed once via DI with the connection string),
    /// and every method here just forwards to the underlying data-access
    /// class of the same name.
    /// </summary>
    public class DoctorRepository
    {
        private readonly DoctorDataAccess _dataAccess;

        public DoctorRepository(string connectionString)
        {
            _dataAccess = new DoctorDataAccess(connectionString);
        }

        // ---- Identity ---------------------------------------------------

        public (int DoctorID, string DoctorName, string Email)? GetDoctorByUserId(int userId)
            => _dataAccess.GetDoctorByUserId(userId);

        public (int DoctorID, string FirstName, string LastName, string Email)? GetDoctorProfileByUserId(int userId)
            => _dataAccess.GetDoctorProfileByUserId(userId);

        // ---- 1. Manage Patient Records -----------------------------------

        public List<PatientRecordListItem> SearchPatients(string? searchTerm)
            => _dataAccess.SearchPatients(searchTerm);

        public bool PatientIdNumberExists(string idNumber)
            => _dataAccess.PatientIdNumberExists(idNumber);

        public (string Result, int NewUserId, int NewPatientId) RegisterPatientByDoctor(
            string username, string email, string passwordHash,
            string firstName, string lastName, string idNumber,
            DateTime dateOfBirth, string cellphoneNumber)
            => _dataAccess.RegisterPatientByDoctor(
                username, email, passwordHash, firstName, lastName,
                idNumber, dateOfBirth, cellphoneNumber);

        // ---- 2. Create Test Requests --------------------------------------

        public List<TestTypeOption> GetAllTestTypes()
            => _dataAccess.GetAllTestTypes();

        public List<SampleTypeOption> GetAllSampleTypes()
            => _dataAccess.GetAllSampleTypes();

        public List<SampleTypeOption> GetRequiredSamplesForTestTypes(List<int> testTypeIds)
            => _dataAccess.GetRequiredSamplesForTestTypes(testTypeIds);

        public (int RequestID, string RequestNumber) CreateTestRequest(
            int patientId, int doctorId, string urgency, string? clinicalNotes,
            List<int> testTypeIds, List<SampleBarcodeInput> samples)
            => _dataAccess.CreateTestRequest(patientId, doctorId, urgency, clinicalNotes, testTypeIds, samples);

        // ---- 3/4. Track status + View results -----------------------------

        public List<DoctorTestRequestSummary> GetDoctorTestRequests(int doctorId, DateTime? from = null, DateTime? to = null)
            => _dataAccess.GetDoctorTestRequests(doctorId, from, to);

        public DoctorTestRequestSummary? GetTestRequestDetail(int doctorId, int requestId)
            => _dataAccess.GetTestRequestDetail(doctorId, requestId);

        public (bool Success, string Message, string? NotifyDoctorEmail, string? NotifyDoctorName) CancelTestRequest(
            int requestId, string reason, int cancelledByUserId, string cancelledByRole)
            => _dataAccess.CancelTestRequest(requestId, reason, cancelledByUserId, cancelledByRole);

        public (bool Success, string Message) ReleaseResults(int requestId, int doctorId, string releaseNotes)
            => _dataAccess.ReleaseResults(requestId, doctorId, releaseNotes);

        // ---- 5. Alerts -----------------------------------------------------

        public List<AbnormalAlertViewModel> GetAbnormalAlerts(int doctorId, DateTime? from, DateTime? to)
            => _dataAccess.GetAbnormalAlerts(doctorId, from, to);
    }
}