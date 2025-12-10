using System.Text.RegularExpressions;

namespace DatabaseDemo.Services
{
    public class SqlSandbox
    {
        private readonly List<string> _prohibitedKeywords = new()
        {
            "DROP", "TRUNCATE", "ALTER", "CREATE", "INSERT", "EXEC", "EXECUTE", 
            "SP_", "XP_", "OPENROWSET", "BULK", "BACKUP", "RESTORE", "GRANT", "REVOKE",
            "SHUTDOWN", "KILL", "DBCC", "RECONFIGURE", "MERGE", "CALL"
        };

        private readonly ILogger<SqlSandbox> _logger;

        public SqlSandbox(ILogger<SqlSandbox> logger)
        {
            _logger = logger;
        }

        public (bool IsValid, string Message) ValidateQuery(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                return (false, "SQL query cannot be empty");
            }

            _logger.LogInformation("Validating SQL query: {QueryLength} characters", sqlQuery.Length);

            var upperQuery = sqlQuery.ToUpperInvariant();
            var originalQuery = sqlQuery.Trim();

            // Check for prohibited keywords (using word boundaries to avoid false positives)
            foreach (var keyword in _prohibitedKeywords)
            {
                // Use word boundary regex to match only whole words, not substrings
                var pattern = $@"\b{Regex.Escape(keyword)}\b";
                if (Regex.IsMatch(upperQuery, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning("Blocked dangerous keyword: {Keyword} in query: {Query}", keyword, sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length)));
                    return (false, $"? Security Alert: Prohibited operation '{keyword}' detected. Only safe SELECT queries are allowed for data analysis.");
                }
            }

            // Check for UPDATE without WHERE
            if (Regex.IsMatch(upperQuery, @"\bUPDATE\b", RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(upperQuery, @"\bUPDATE\b.*\bWHERE\b", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                {
                    _logger.LogWarning("Blocked UPDATE without WHERE clause: {Query}", sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length)));
                    return (false, "? Security Alert: UPDATE statements must include a WHERE clause to prevent accidental data modification.");
                }
                _logger.LogWarning("Blocked UPDATE statement: {Query}", sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length)));
                return (false, "? Security Alert: Data modification (UPDATE) is not allowed. Use SELECT statements for data analysis only.");
            }

            // Check for DELETE without WHERE
            if (Regex.IsMatch(upperQuery, @"\bDELETE\b", RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(upperQuery, @"\bDELETE\b.*\bWHERE\b", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                {
                    _logger.LogWarning("Blocked DELETE without WHERE clause: {Query}", sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length)));
                    return (false, "? Security Alert: DELETE statements must include a WHERE clause to prevent accidental data loss.");
                }
                _logger.LogWarning("Blocked DELETE statement: {Query}", sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length)));
                return (false, "? Security Alert: Data deletion (DELETE) is not allowed. Use SELECT statements for data analysis only.");
            }

            // Basic SQL injection patterns
            var injectionPatterns = new Dictionary<string, string>
            {
                [@";\s*DROP\b"] = "SQL injection attempt with DROP command",
                [@";\s*DELETE\b"] = "SQL injection attempt with DELETE command", 
                [@";\s*UPDATE\b"] = "SQL injection attempt with UPDATE command",
                [@";\s*INSERT\b"] = "SQL injection attempt with INSERT command",
                [@"'\s*OR\s*'.*'"] = "SQL injection with OR condition",
                [@"'\s*AND\s*'.*'"] = "SQL injection with AND condition",
                [@"\bUNION\b.*\bSELECT\b"] = "SQL injection with UNION SELECT",
                [@"--\s*$"] = "SQL comment injection attempt",
                [@"/\*.*\*/"] = "SQL block comment injection"
            };

            foreach (var pattern in injectionPatterns)
            {
                if (Regex.IsMatch(upperQuery, pattern.Key, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    _logger.LogWarning("Blocked SQL injection pattern '{Pattern}': {Query}", pattern.Value, sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length)));
                    return (false, $"? Security Alert: {pattern.Value} detected. Malicious queries are blocked for security.");
                }
            }

            // Ensure only SELECT queries are allowed
            var trimmedQuery = upperQuery.Trim();
            if (!trimmedQuery.StartsWith("SELECT"))
            {
                _logger.LogWarning("Blocked non-SELECT query: {Query}", sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length)));
                return (false, "? Security Policy: Only SELECT queries are allowed for data analysis. No data modification operations permitted.");
            }

            // Check for multiple statements (semicolon followed by more SQL)
            if (Regex.IsMatch(originalQuery, @";\s*\w+", RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("Blocked multiple SQL statements: {Query}", sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length)));
                return (false, "? Security Alert: Multiple SQL statements detected. Only single SELECT queries are allowed.");
            }

            _logger.LogInformation("SQL query passed validation successfully");
            return (true, "? Query passed security validation - safe to execute");
        }
    }
}