using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownePark.Billing.Api.Data.Impl
{
    public class EDWRepository : IEDWRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<EDWRepository> _logger;
        public EDWRepository(string connectionString, ILogger<EDWRepository> logger) {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(
        string sqlOrProcName,
        Dictionary<string, object> parameters = null,
        CommandType commandType = CommandType.Text)
        {
            var results = new List<Dictionary<string, object>>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(sqlOrProcName, connection)
                {
                    CommandType = commandType
                };

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing query: {ex.Message}");
                throw;
            }

            return results;
        }
    }
}
