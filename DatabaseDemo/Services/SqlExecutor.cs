using Microsoft.Data.SqlClient;
using System.Data;

namespace DatabaseDemo.Services
{
    public class SqlExecutor
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlExecutor> _logger;

        public SqlExecutor(IConfiguration configuration, ILogger<SqlExecutor> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Database connection string not configured");
            _logger = logger;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sqlQuery)
        {
            var results = new List<Dictionary<string, object>>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(sqlQuery, connection);
                command.CommandTimeout = 30;

                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[columnName] = value ?? DBNull.Value;
                    }
                    results.Add(row);
                }

                _logger.LogInformation("Successfully executed query, returned {RowCount} rows", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL execution failed for query: {Query}", sqlQuery);
                throw new InvalidOperationException($"SQL execution failed: {ex.Message}", ex);
            }

            return results;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Database connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return false;
            }
        }

        public async Task<string> GetDatabaseSchemaAsync()
        {
            try
            {
                _logger.LogInformation("Starting to retrieve database schema...");
                
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Database connection opened successfully for schema retrieval");

                // Simplified query first - just get table names
                var schemaQuery = @"
                SELECT 
                    TABLE_SCHEMA,
                    TABLE_NAME,
                    TABLE_TYPE
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME";

                using var command = new SqlCommand(schemaQuery, connection);
                command.CommandTimeout = 30;
                using var reader = await command.ExecuteReaderAsync();

                var tables = new List<string>();
                
                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString("TABLE_SCHEMA");
                    var tableName = reader.GetString("TABLE_NAME");
                    tables.Add($"{schema}.{tableName}");
                }

                reader.Close();

                if (!tables.Any())
                {
                    _logger.LogWarning("No tables found in database");
                    return "No tables found in the database";
                }

                _logger.LogInformation("Found {TableCount} tables", tables.Count);

                // Now get detailed column information
                var schemaDescription = $"Database Schema (Found {tables.Count} tables):\n\n";
                schemaDescription += "=== KEY RELATIONSHIPS ===\n";
                schemaDescription += "- SensorData.Wm6_DeviceSettingId ? DeviceSettings.Id (device reference)\n";
                schemaDescription += "- DeviceSettings.Id = Primary Key for devices\n";
                schemaDescription += "- DeviceSettings.IMEI = Unique device identifier\n";
                schemaDescription += "- UserTable.Id ? Various tables via UserId\n";
                schemaDescription += "- Orders.CustomerId ? UserTable.Id\n\n";
                schemaDescription += "=== TABLES ===\n\n";
                
                foreach (var table in tables)
                {
                    schemaDescription += $"[TABLE] {table}\n";
                    
                    try
                    {
                        var columnQuery = @"
                        SELECT 
                            COLUMN_NAME,
                            DATA_TYPE,
                            IS_NULLABLE,
                            COLUMN_DEFAULT,
                            CHARACTER_MAXIMUM_LENGTH
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA + '.' + TABLE_NAME = @TableName
                        ORDER BY ORDINAL_POSITION";

                        using var colCommand = new SqlCommand(columnQuery, connection);
                        colCommand.Parameters.AddWithValue("@TableName", table);
                        
                        using var colReader = await colCommand.ExecuteReaderAsync();
                        var columns = new List<string>();
                        
                        while (await colReader.ReadAsync())
                        {
                            var colName = colReader.GetString("COLUMN_NAME");
                            var dataType = colReader.GetString("DATA_TYPE");
                            var nullable = colReader.GetString("IS_NULLABLE");
                            var maxLength = colReader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? "" : $"({colReader.GetValue("CHARACTER_MAXIMUM_LENGTH")})";
                            var defaultVal = colReader.IsDBNull("COLUMN_DEFAULT") ? "" : $" DEFAULT {colReader.GetValue("COLUMN_DEFAULT")}";
                            
                            var nullableText = nullable == "YES" ? " NULL" : " NOT NULL";
                            columns.Add($"  - {colName} : {dataType}{maxLength}{nullableText}{defaultVal}");
                        }
                        
                        colReader.Close();
                        
                        if (columns.Any())
                        {
                            schemaDescription += string.Join("\n", columns) + "\n\n";
                        }
                        else
                        {
                            schemaDescription += "  (No columns found)\n\n";
                        }
                    }
                    catch (Exception colEx)
                    {
                        _logger.LogError(colEx, "Error retrieving columns for table {TableName}", table);
                        schemaDescription += "  (Error retrieving columns)\n\n";
                    }
                }

                _logger.LogInformation("Successfully retrieved database schema");
                return schemaDescription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve database schema: {Error}", ex.Message);
                return $"Unable to retrieve database schema: {ex.Message}";
            }
        }
    }
}