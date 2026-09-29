using Doctor.DataAccess;
using Doctor.Models;
using Patient.Models;

namespace Doctor.Repository
{
    public class DoctorRepository
    {
        private readonly DoctorDataAccess _dataAccess;

        public DoctorRepository(string connectionString)
        {
            _dataAccess = new DoctorDataAccess(connectionString);
        }

        // Feature 1: Manage Patient Records
        public (string result, int newUserId, int newPatientId) RegisterPatientByDoctor(
            string username, string email, string passwordHash, string firstName,
            string lastName, string idNumber, DateTime dateOfBirth, string cellphone)
            => _dataAccess.RegisterPatientByDoctor(username, email, passwordHash, firstName,
                lastName, idNumber, dateOfBirth, cellphone);

        // Feature 2: Create Test Requests
        public List<(int SampleTypeID, string SampleTypeName)> GetRequiredSamplesForTestTypes(string testTypeIdsJson)
            => _dataAccess.GetRequiredSamplesForTestTypes(testTypeIdsJson);

        public (string result, int newRequestId) CreateTestRequest(
            int patientId, int doctorId, string urgency, string? clinicalNotes,
            string testTypeIdsJson, string barcodesJson)
            => _dataAccess.CreateTestRequest(patientId, doctorId, urgency, clinicalNotes, testTypeIdsJson, barcodesJson);

        // Feature 3: Track Test Request Status / Cancel
        public (string result, string? notifyDoctorEmail, string? notifyDoctorName) CancelTestRequest(
            int requestId, string cancellationReason, int cancelledByUserId, string cancelledByRole)
            => _dataAccess.CancelTestRequest(requestId, cancellationReason, cancelledByUserId, cancelledByRole);

        public List<TestRequest> GetPatientTestRequests(int patientId)
            => _dataAccess.GetPatientTestRequests(patientId);

        // Feature 4: View Results
        public List<TestResultViewModel> GetTestResultsForDoctor(int requestId, int doctorId)
            => _dataAccess.GetTestResultsForDoctor(requestId, doctorId);

        public string ReleaseTestResults(int requestId, int doctorId, string? releaseNote)
            => _dataAccess.ReleaseTestResults(requestId, doctorId, releaseNote);

        // Feature 5: View Alerts
        public List<AlertViewModel> GetDoctorAlerts(int doctorId, DateTime? fromDate)
            => _dataAccess.GetDoctorAlerts(doctorId, fromDate);

        // Feature 6: Doctor Reports
        public List<TestRequestViewModel> GetDoctorTestRequestsByDateRange(int doctorId, DateTime start, DateTime end)
            => _dataAccess.GetDoctorTestRequestsByDateRange(doctorId, start, end);

        // Shared
        public List<(int DoctorID, string DoctorName, string Email)> GetAllDoctors()
            => _dataAccess.GetAllDoctors();
    }
}