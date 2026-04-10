using System.Data;
using Microsoft.Data.SqlClient;
using Patient.Models;

namespace Patient.DataAccess
{
    public class UserDataAccess
    {
        private readonly string _connectionString;
        
        public UserDataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        public (UserSession? user, string? passwordHash) GetUserLoginData(string username)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_LoginUser", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                var passwordHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                var user = new UserSession
                {
                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    RoleID = reader.GetInt32(reader.GetOrdinal("RoleID")),
                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                };
                return (user, passwordHash);
            }

            return (null, null);
        }

        public void UpdateLastLogin(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET LastLoginDate = GETDATE() WHERE UserID = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }
        public string RegisterUser(string username, string email, string passwordHash, int roleId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RegisterUser", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@RoleID", roleId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return reader.GetString(reader.GetOrdinal("Result"));
            return "ERROR";
        }
        public List<(int Id, string Name)> GetAllRoles()
        {
            var roles = new List<(int, string)>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT RoleID, RoleName FROM Roles ORDER BY RoleName", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                roles.Add((reader.GetInt32(0), reader.GetString(1)));
            return roles;
        }
        public bool SaveResetToken(string email, string token, DateTime expiry)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET ResetToken = @Token, ResetTokenExpiry = @Expiry WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@Expiry", expiry);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ValidateResetToken(string token)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Users WHERE ResetToken = @Token AND ResetTokenExpiry > GETDATE()", conn);
            cmd.Parameters.AddWithValue("@Token", token);
            conn.Open();
            return (int)cmd.ExecuteScalar() > 0;
        }

        public bool ResetPassword(string token, string newPassword)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET PasswordHash = @Hash, ResetToken = NULL, ResetTokenExpiry = NULL WHERE ResetToken = @Token AND ResetTokenExpiry > GETDATE()", conn);
            cmd.Parameters.AddWithValue("@Hash", hashed);
            cmd.Parameters.AddWithValue("@Token", token);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
        public bool ResetPasswordByEmail(string email, string newPassword)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE Users SET PasswordHash = @Hash WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Hash", hashed);
            cmd.Parameters.AddWithValue("@Email", email);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}