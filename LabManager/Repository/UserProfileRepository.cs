using Dapper;
using LabManager.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LabManager.Repository
{
    public sealed class UserProfileRepository : IUserProfileRepository
    {
        private readonly string _connectionString;

        public UserProfileRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("conn")
                ?? throw new InvalidOperationException("A database connection string is required.");
        }

        public async Task<UserProfileViewModel?> GetProfileByEmail(string email)
        {
            await using var connection = new SqlConnection(_connectionString);
            var columns = await GetUserColumns(connection);
            var idColumn = FindColumn(columns, "UserID", "Id", "UserId");
            var emailColumn = FindColumn(columns, "Email", "EmailAddress");
            if (idColumn == null || emailColumn == null)
                throw new InvalidOperationException("The dbo.Users table must contain an ID column (UserID or Id) and an email column (Email or EmailAddress).");

            var sql = $@"
SELECT TOP (1)
    {Quote(idColumn)} AS [UserID],
    {Quote(emailColumn)} AS [Email],
    {SelectColumn(columns, "PasswordHash", "nvarchar(512)")},
    {SelectColumn(columns, "FirstName", "nvarchar(256)")},
    {SelectColumn(columns, "LastName", "nvarchar(256)")},
    {SelectColumn(columns, "IDNumber", "nvarchar(64)")},
    {SelectColumn(columns, "DateOfBirth", "datetime2")},
    {SelectColumn(columns, "CellphoneNumber", "nvarchar(64)")},
    {SelectColumn(columns, "HomeAddress", "nvarchar(512)")},
    {SelectColumn(columns, "RegistrationDate", "datetime2")}
FROM dbo.Users
WHERE {Quote(emailColumn)} = @Email;";

            var row = await connection.QuerySingleOrDefaultAsync<ProfileRow>(sql, new { Email = email });
            if (row == null) return null;

            var fallbackName = email.Split('@', 2)[0].Replace('.', ' ').Replace('_', ' ');
            var nameParts = fallbackName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return new UserProfileViewModel
            {
                UserID = row.UserID,
                Email = row.Email,
                PasswordHash = row.PasswordHash,
                FirstName = row.FirstName ?? (nameParts.FirstOrDefault() ?? "Lab"),
                LastName = row.LastName ?? (nameParts.Length > 1 ? string.Join(' ', nameParts.Skip(1)) : "Manager"),
                IDNumber = row.IDNumber ?? string.Empty,
                DateOfBirth = row.DateOfBirth ?? DateTime.Today,
                CellphoneNumber = row.CellphoneNumber ?? string.Empty,
                HomeAddress = row.HomeAddress ?? string.Empty,
                RegistrationDate = row.RegistrationDate ?? DateTime.Today
            };
        }

        public async Task<bool> UpdateProfile(UserProfileViewModel model)
        {
            await using var connection = new SqlConnection(_connectionString);
            var columns = await GetUserColumns(connection);
            var idColumn = FindColumn(columns, "UserID", "Id", "UserId");
            var emailColumn = FindColumn(columns, "Email", "EmailAddress");
            if (idColumn == null || emailColumn == null) return false;
            var fields = new[] { "FirstName", "LastName", "IDNumber", "DateOfBirth", "CellphoneNumber", "HomeAddress" }
                .Where(columns.Contains)
                .ToArray();
            if (fields.Length == 0) return false;

            var sql = $@"UPDATE dbo.Users SET {string.Join(", ", fields.Select(c => $"{Quote(c)} = @{c}"))}
WHERE {Quote(idColumn)} = @UserID AND {Quote(emailColumn)} = @Email;";
            var affected = await connection.ExecuteAsync(sql, model);
            return affected > 0;
        }

        public async Task<string?> GetPasswordHash(string email)
        {
            await using var connection = new SqlConnection(_connectionString);
            var columns = await GetUserColumns(connection);
            var emailColumn = FindColumn(columns, "Email", "EmailAddress");
            if (emailColumn == null || !columns.Contains("PasswordHash")) return null;
            var sql = $"SELECT [PasswordHash] FROM dbo.Users WHERE {Quote(emailColumn)} = @Email;";
            return await connection.QuerySingleOrDefaultAsync<string?>(sql, new { Email = email });
        }

        public async Task<bool> UpdatePassword(string email, string passwordHash)
        {
            await using var connection = new SqlConnection(_connectionString);
            var columns = await GetUserColumns(connection);
            var emailColumn = FindColumn(columns, "Email", "EmailAddress");
            if (emailColumn == null || !columns.Contains("PasswordHash")) return false;
            var sql = $"UPDATE dbo.Users SET [PasswordHash] = @PasswordHash WHERE {Quote(emailColumn)} = @Email;";
            return await connection.ExecuteAsync(sql, new { Email = email, PasswordHash = passwordHash }) > 0;
        }

        private static async Task<HashSet<string>> GetUserColumns(SqlConnection connection)
        {
            const string sql = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Users';";
            var columns = await connection.QueryAsync<string>(sql);
            return new HashSet<string>(columns, StringComparer.OrdinalIgnoreCase);
        }

        private static string SelectColumn(HashSet<string> columns, string name, string sqlType) =>
            FindColumn(columns, name) is { } column ? $"{Quote(column)} AS {Quote(name)}" : $"CAST(NULL AS {sqlType}) AS {Quote(name)}";

        private static string? FindColumn(HashSet<string> columns, params string[] candidates) =>
            candidates.FirstOrDefault(columns.Contains);

        private static string Quote(string name) => $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";

        private sealed class ProfileRow
        {
            public int UserID { get; init; }
            public string Email { get; init; } = string.Empty;
            public string? PasswordHash { get; init; }
            public string? FirstName { get; init; }
            public string? LastName { get; init; }
            public string? IDNumber { get; init; }
            public DateTime? DateOfBirth { get; init; }
            public string? CellphoneNumber { get; init; }
            public string? HomeAddress { get; init; }
            public DateTime? RegistrationDate { get; init; }
        }
    }
}
