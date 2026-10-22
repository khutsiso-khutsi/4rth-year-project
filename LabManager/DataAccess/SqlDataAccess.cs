using Dapper;
using  Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.DataAccess
{
    using System.Data;
    using Dapper;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Configuration;

    public class SqlDataAccess : ISqlDataAcess
    {
        private readonly IConfiguration _config;

        public SqlDataAccess(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection CreateConnection(string connectionId)
        {
            string? connectionString =
                _config.GetConnectionString(connectionId);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{connectionId}' was not found.");
            }

            return new SqlConnection(connectionString);
        }

        public async Task<IEnumerable<T>> GetData<T, P>(
            string spName,
            P parameter,
            string connectionId = "conn")
        {
            using IDbConnection connection =
                CreateConnection(connectionId);

            return await connection.QueryAsync<T>(
                spName,
                parameter,
                commandType: CommandType.StoredProcedure);
        }

        public async Task SaveData<T>(
            string spName,
            T parameters,
            string connectionId = "conn")
        {
            using IDbConnection connection =
                CreateConnection(connectionId);

            await connection.ExecuteAsync(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}

