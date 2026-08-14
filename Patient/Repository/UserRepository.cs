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

        public (string result, int newUserId) RegisterUser(string username, string email,
    string passwordHash, int roleId, string firstName, string lastName,
    string idNumber, DateTime dateOfBirth, string cellphone, string homeAddress)
        {
            return _dataAccess.RegisterUser(username, email, passwordHash, roleId,
                firstName, lastName, idNumber, dateOfBirth, cellphone, homeAddress);
        }


        public void CreatePatientRecord(int userId, string firstName, string lastName,
    string idNumber, DateTime dateOfBirth, string cellphoneNumber, string homeAddress)
        {
            _dataAccess.CreatePatientRecord(userId, firstName, lastName,
                idNumber, dateOfBirth, cellphoneNumber, homeAddress);
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

        public void RemovePatientCondition(int patientId, int conditionId)
    => _dataAccess.RemovePatientCondition(patientId, conditionId);

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

        public ConsentViewModel GetConsentData(int patientId, int selectedDoctorId = 0)
    => _dataAccess.GetConsentData(patientId, selectedDoctorId);

        public void GrantConsent(int patientId, int doctorId)
            => _dataAccess.GrantConsent(patientId, doctorId);

        public void RevokeConsent(int patientId, int doctorId)
            => _dataAccess.RevokeConsent(patientId, doctorId);

        public void GrantTestRequestConsent(int patientId, int doctorId, int requestId)
            => _dataAccess.GrantTestRequestConsent(patientId, doctorId, requestId);

        public void RevokeTestRequestConsent(int patientId, int doctorId, int requestId)
            => _dataAccess.RevokeTestRequestConsent(patientId, doctorId, requestId);
        public ProfileViewModel GetPatientProfile(int patientId)
    => _dataAccess.GetPatientProfile(patientId);

        public void UpdatePatientProfile(int patientId, string firstName, string lastName,
     DateTime dob, string cellphone, string homeAddress, string email)
        {
            _dataAccess.UpdatePatientProfile(patientId, firstName, lastName, dob, cellphone, homeAddress, email);
        }
        public void ChangePatientPassword(int userId, string newPasswordHash)
            => _dataAccess.ChangePatientPassword(userId, newPasswordHash);

        public (int UserId, string PasswordHash)? GetUserById(int userId)
            => _dataAccess.GetUserById(userId);

        public void SaveVerificationCode(string email, string code, DateTime expiry)
        {
            _dataAccess.SaveVerificationCode(email, code, expiry);
        }

        public (bool success, int userId) VerifyCode(string email, string code)
        {
            return _dataAccess.VerifyCode(email, code);
        }

        public void MarkEmailVerified(int userId)
        {
            _dataAccess.MarkEmailVerified(userId);
        }
    }
}