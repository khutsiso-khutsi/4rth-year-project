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
    }
}