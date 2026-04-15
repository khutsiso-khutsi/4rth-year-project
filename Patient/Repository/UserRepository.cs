using Patient.DataAccess;
using Patient.Models;

namespace Patient.Repository
{
    public class UserRepository
    {
        private readonly UserDataAccess _dataAccess;

        public UserRepository(string connectionString)
        {
            _dataAccess = new UserDataAccess(connectionString);
        }

        public (UserSession? user, string? passwordHash) GetUserLoginData(string username)
        {
            return _dataAccess.GetUserLoginData(username);
        }

        public void UpdateLastLogin(int userId)
        {
            _dataAccess.UpdateLastLogin(userId);
        }
        public string RegisterUser(string username, string email, string passwordHash, int roleId)
        {
            return _dataAccess.RegisterUser(username, email, passwordHash, roleId);
        }
        public List<(int Id, string Name)> GetAllRoles()
        {
            return _dataAccess.GetAllRoles();
        }
        public bool SaveResetToken(string email, string token, DateTime expiry)
    => _dataAccess.SaveResetToken(email, token, expiry);

        public bool ValidateResetToken(string token)
            => _dataAccess.ValidateResetToken(token);

        public bool ResetPassword(string token, string newPassword)
            => _dataAccess.ResetPassword(token, newPassword);
        public bool ResetPasswordByEmail(string email, string newPassword)
    => _dataAccess.ResetPasswordByEmail(email, newPassword);
        public List<TestRequest> GetPatientTestRequests(int patientId)
    => _dataAccess.GetPatientTestRequests(patientId);

        public List<TestRequestItem> GetTestRequestItems(int requestId)
            => _dataAccess.GetTestRequestItems(requestId);
        public int? GetPatientIdByUserId(int userId)
    => _dataAccess.GetPatientIdByUserId(userId);

        public MedicalHistoryViewModel GetMedicalHistory(int patientId)
    => _dataAccess.GetMedicalHistory(patientId);

        public void AddPatientCondition(int patientId, int conditionId, DateTime? diagnosedDate, string? notes)
            => _dataAccess.AddPatientCondition(patientId, conditionId, diagnosedDate, notes);

        public void RemovePatientCondition(int patientConditionId)
            => _dataAccess.RemovePatientCondition(patientConditionId);

        public void AddPatientAllergy(int patientId, int allergyId, string? severity, string? notes)
            => _dataAccess.AddPatientAllergy(patientId, allergyId, severity, notes);

        public void RemovePatientAllergy(int patientId, int allergyId)
            => _dataAccess.RemovePatientAllergy(patientId, allergyId);

        public void AddPatientMedication(int patientId, int medicationId, string? dosage, string? frequency, DateTime? startDate, DateTime? endDate, string? notes)
            => _dataAccess.AddPatientMedication(patientId, medicationId, dosage, frequency, startDate, endDate, notes);

        public void RemovePatientMedication(int patientId, int medicationId)
            => _dataAccess.RemovePatientMedication(patientId, medicationId);
        public void LogActivity(string action, string performedBy)
    => _dataAccess.LogActivity(action, performedBy);
    }
}