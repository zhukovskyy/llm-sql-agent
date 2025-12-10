using DatabaseDemo.Models;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace DatabaseDemo.Services
{
    public class LlmAgent
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly ILogger<LlmAgent> _logger;

        public LlmAgent(HttpClient httpClient, IConfiguration configuration, ILogger<LlmAgent> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> GenerateSqlQueryAsync(string naturalLanguageQuery)
        {
            try
            {
                _logger.LogInformation("Universal SQL Agent analyzing query: {Query}", naturalLanguageQuery);
                
                var schemaDescription = _configuration["Database:Schema"] ?? "No schema available";
                
                // Perform intelligent schema analysis
                var schemaAnalysis = AnalyzeSchemaIntelligently(schemaDescription);
                
                var systemPrompt = $@"You are an intelligent SQL Database Agent with analytical reasoning capabilities. Your task is to understand any database schema and generate optimal queries through systematic analysis.

Database Schema:
{schemaDescription}

INTELLIGENT SCHEMA ANALYSIS:
{schemaAnalysis}

ANALYTICAL PROCESS - Think step by step:

1. SCHEMA ANALYSIS:
   - Identify primary keys (usually 'Id', 'ID', or table_name + 'Id')
   - Find foreign key relationships (columns ending with 'Id', '_id', or matching other table primary keys)
   - Locate timestamp columns ('Date', 'Time', 'Created', 'Updated', 'Timestamp')
   - Recognize entity vs relationship tables

2. PATTERN RECOGNITION:
   - User/Account tables: Usually contain 'User', 'Customer', 'Account' in name
   - Transaction/Event tables: Often have timestamps, foreign keys to entities
   - Relationship tables: Connect entities (UserRole, CustomerOrder, ProductCategory)
   - Configuration tables: Settings, Statuses, Types

3. QUERY INTENT ANALYSIS:
   - 'Latest/Recent': Use timestamp columns with ORDER BY DESC
   - 'Most/Top': Use COUNT/SUM with GROUP BY and ORDER BY DESC
   - 'Ownership/Belongs': Find user-entity relationship paths
   - 'Statistics': Aggregate functions (COUNT, AVG, SUM)

4. RELATIONSHIP MAPPING:
   - For A belongs to B: Find foreign key A.B_Id ? B.Id
   - For A has many B: Find B.A_Id ? A.Id
   - For many-to-many: Find intermediate table A_B with A_Id and B_Id

SMART QUERY BUILDING:
- Always use EXACT column names from schema
- Prefer explicit JOINs over subqueries
- Use TOP 10 for safety (T-SQL syntax, NOT LIMIT)
- Include relevant descriptive columns (names, titles, descriptions)
- For temporal queries: ORDER BY most recent timestamp column DESC
- For aggregations: GROUP BY entity identifier, ORDER BY aggregated value DESC
- T-SQL SPECIFIC: Use TOP N, not LIMIT N
- T-SQL SPECIFIC: Use square brackets [column name] for reserved words
- T-SQL SPECIFIC: Use GETDATE() for current date, not NOW()

SAFETY RULES:
- Generate ONLY SELECT statements
- Never guess column names - use schema exactly
- Use TOP for result safety (NOT LIMIT - this is T-SQL/SQL Server)
- Validate table relationships exist before joining
- Always use T-SQL syntax, not MySQL or PostgreSQL

Return ONLY the SQL query without explanations or formatting.";

                var userMessage = $@"ANALYTICAL TASK: {naturalLanguageQuery}

THINK STEP BY STEP:
1. What data am I looking for?
2. Which tables might contain this data?
3. What relationships exist between these tables?
4. What aggregations or sorting do I need?
5. How should I structure the JOIN conditions?

CRITICAL T-SQL REQUIREMENTS:
- Use TOP N syntax (NOT LIMIT N)
- Use square brackets for reserved words: [Order], [User], [Date]
- Use GETDATE() for current date
- Join syntax: FROM TableA a JOIN TableB b ON a.Id = b.TableA_Id

Remember: Analyze the provided schema carefully and use exact column names. Think like a database analyst, not just a code generator.";

                _logger.LogInformation("Schema analysis completed. Query length: {Length}", naturalLanguageQuery.Length);
                _logger.LogInformation("Schema analysis result: {Analysis}", schemaAnalysis);

                var requestBody = new
                {
                    model = "gpt-4o-mini", // Using more capable model for better reasoning
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userMessage }
                    },
                    temperature = 0.1,
                    max_tokens = 600
                };

                var json = JsonConvert.SerializeObject(requestBody, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API error: {Status} - {Content}", response.StatusCode, responseContent);
                    throw new InvalidOperationException($"OpenAI API error: {response.StatusCode}");
                }
                
                var openAIResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                var sqlQuery = openAIResponse?.choices?[0]?.message?.content?.ToString();
                
                if (string.IsNullOrWhiteSpace(sqlQuery))
                {
                    throw new InvalidOperationException("No SQL query generated by Universal Agent");
                }
                
                sqlQuery = CleanSqlResponse(sqlQuery);
                
                _logger.LogInformation("Universal Agent generated SQL: {SQL}", (string)sqlQuery);
                return sqlQuery;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Universal SQL Agent failed");
                throw new InvalidOperationException($"Universal Agent failed: {ex.Message}", ex);
            }
        }

        private string AnalyzeSchemaIntelligently(string schemaDescription)
        {
            var analysis = new StringBuilder();
            analysis.AppendLine("INTELLIGENT SCHEMA ANALYSIS:");
            
            // Extract table names
            var tables = ExtractTableNames(schemaDescription);
            analysis.AppendLine($"Found {tables.Count} tables");
            
            // Identify potential relationships
            var relationships = IdentifyRelationships(schemaDescription, tables);
            analysis.AppendLine("DETECTED RELATIONSHIPS:");
            foreach(var rel in relationships)
            {
                analysis.AppendLine($"   {rel}");
            }
            
            // Identify common patterns
            analysis.AppendLine("COMMON QUERY PATTERNS:");
            if (tables.Any(t => t.Contains("User") || t.Contains("Customer")))
                analysis.AppendLine("   - User/Customer queries available");
            if (HasTimestampColumns(schemaDescription))
                analysis.AppendLine("   - Temporal queries possible (latest, recent)");
            if (HasCountableEntities(schemaDescription, tables))
                analysis.AppendLine("   - Aggregation queries possible (most, top)");
            
            // Identify ownership patterns
            var ownershipTables = tables.Where(t => t.Contains("Role") || t.Contains("Assignment") || t.Contains("Subscription")).ToList();
            if (ownershipTables.Any())
            {
                analysis.AppendLine("   - Ownership/Role relationships detected:");
                foreach(var table in ownershipTables)
                {
                    analysis.AppendLine($"     * {table}");
                }
            }
            
            return analysis.ToString();
        }

        private List<string> ExtractTableNames(string schema)
        {
            var matches = Regex.Matches(schema, @"\[TABLE\]\s+([^\s\n]+)");
            return matches.Cast<Match>().Select(m => m.Groups[1].Value.Trim()).ToList();
        }

        private List<string> IdentifyRelationships(string schema, List<string> tables)
        {
            var relationships = new List<string>();
            
            foreach(var table in tables)
            {
                var tableSection = ExtractTableSection(schema, table);
                if (tableSection != null)
                {
                    var foreignKeys = FindForeignKeyColumns(tableSection, tables);
                    
                    foreach(var fk in foreignKeys)
                    {
                        relationships.Add($"{table}.{fk.Column} -> {fk.ReferencedTable}.Id");
                    }
                }
            }
            
            return relationships;
        }

        private string ExtractTableSection(string schema, string tableName)
        {
            var tablePattern = $@"\[TABLE\]\s+{Regex.Escape(tableName)}(.*?)(?=\[TABLE\]|$)";
            var match = Regex.Match(schema, tablePattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private List<(string Column, string ReferencedTable)> FindForeignKeyColumns(string tableSection, List<string> allTables)
        {
            var foreignKeys = new List<(string Column, string ReferencedTable)>();
            
            if (tableSection == null) return foreignKeys;
            
            var columnPattern = @"([A-Za-z_]+(?:Id|_Id))\s+:";
            var matches = Regex.Matches(tableSection, columnPattern);
            
            foreach (Match match in matches)
            {
                var columnName = match.Groups[1].Value;
                
                // Try to find referenced table
                foreach (var table in allTables)
                {
                    var tableName = table.Split('.').Last();
                    
                    // Check if column name suggests this table
                    if (columnName.ToLower().Contains(tableName.ToLower().Replace("table", "")) ||
                        columnName.ToLower().StartsWith(tableName.ToLower().Replace("table", "")))
                    {
                        foreignKeys.Add((columnName, table));
                        break;
                    }
                }
            }
            
            return foreignKeys;
        }

        private bool HasTimestampColumns(string schema)
        {
            var timestampPatterns = new[] { "timestamp", "date", "time", "created", "updated" };
            return timestampPatterns.Any(pattern => 
                schema.ToLower().Contains(pattern.ToLower()));
        }

        private bool HasCountableEntities(string schema, List<string> tables)
        {
            // Look for tables that represent countable entities
            var entityPatterns = new[] { "order", "product", "device", "user", "customer", "transaction" };
            return tables.Any(table => 
                entityPatterns.Any(pattern => 
                    table.ToLower().Contains(pattern.ToLower())));
        }

        private string CleanSqlResponse(string sqlQuery)
        {
            sqlQuery = sqlQuery.Trim();
            
            // Remove markdown formatting
            if (sqlQuery.StartsWith("```sql", StringComparison.OrdinalIgnoreCase))
                sqlQuery = sqlQuery.Substring(6);
            if (sqlQuery.StartsWith("```"))
                sqlQuery = sqlQuery.Substring(3);
            if (sqlQuery.EndsWith("```"))
                sqlQuery = sqlQuery.Substring(0, sqlQuery.Length - 3);
                
            return sqlQuery.Trim();
        }

        public async Task<string> GenerateNaturalLanguageResponseAsync(string originalQuery, List<Dictionary<string, object>>? queryResult)
        {
            try
            {
                _logger.LogInformation("Generating natural language explanation");
                
                var resultText = queryResult == null || !queryResult.Any() 
                    ? "No results found" 
                    : $"Found {queryResult.Count} results: " + JsonConvert.SerializeObject(queryResult.Take(3), Formatting.Indented);

                var systemMessage = @"You are a data analyst who explains database query results in clear, natural language. 
Provide concise summaries that highlight key insights and patterns in the data.";
                
                var userMessage = $@"Query: {originalQuery}
Results: {resultText}

Please provide a brief, insightful summary of these results.";

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "system", content = systemMessage },
                        new { role = "user", content = userMessage }
                    },
                    temperature = 0.3,
                    max_tokens = 250
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return $"Query executed successfully. Found {queryResult?.Count ?? 0} results.";
                }
                
                var openAIResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                var naturalResponse = openAIResponse?.choices?[0]?.message?.content?.ToString();
                
                return naturalResponse ?? "Query completed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate natural language response");
                return $"Query executed successfully. Found {queryResult?.Count ?? 0} results.";
            }
        }

        public async Task<ChatResponse> GenerateSqlWithRetryAsync(string naturalLanguageQuery, SqlSandbox sqlSandbox, SqlExecutor sqlExecutor)
        {
            const int maxAttempts = 4;
            var startTime = DateTime.UtcNow;
            var attempt = 1;
            var errorHistory = new List<string>();
            ChatResponse response = new() { OriginalQuery = naturalLanguageQuery };

            while (attempt <= maxAttempts)
            {
                try
                {
                    _logger.LogInformation("Retry Agent - Attempt {Attempt}/{MaxAttempts} for query: {Query}", 
                        attempt, maxAttempts, naturalLanguageQuery);

                    // Генеруємо SQL з урахуванням попередніх помилок
                    var sql = await GenerateSqlWithErrorContext(naturalLanguageQuery, errorHistory, attempt);
                    response.GeneratedSql = sql;
                    
                    _logger.LogInformation("Attempt {Attempt} generated SQL: {SQL}", attempt, sql);

                    // Реальна валідація з SqlSandbox
                    var (isValid, validationMessage) = sqlSandbox.ValidateQuery(sql);
                    if (!isValid)
                    {
                        throw new SqlValidationException($"Validation failed: {validationMessage}");
                    }

                    response.IsSqlValid = true;
                    response.ValidationMessage = validationMessage;

                    // Реальне виконання з SqlExecutor
                    response.ExecutionResult = await sqlExecutor.ExecuteQueryAsync(sql);
                    
                    // Генерація природної мови відповіді
                    response.FinalAnswer = await GenerateNaturalLanguageResponseAsync(naturalLanguageQuery, response.ExecutionResult);
                    
                    // Успіх!
                    response.AttemptCount = attempt;
                    response.Errors = errorHistory;
                    response.TotalProcessingTime = DateTime.UtcNow - startTime;
                    
                    _logger.LogInformation("Retry Agent succeeded on attempt {Attempt}", attempt);
                    return response;
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Attempt {attempt}: {ex.GetType().Name} - {ex.Message}";
                    errorHistory.Add(errorMessage);
                    
                    _logger.LogWarning("Attempt {Attempt} failed: {Error}", attempt, ex.Message);
                    
                    if (attempt == maxAttempts)
                    {
                        _logger.LogError("Retry Agent failed after {MaxAttempts} attempts for query: {Query}", 
                            maxAttempts, naturalLanguageQuery);
                        
                        response.ErrorMessage = $"Failed after {maxAttempts} attempts. Last error: {ex.Message}";
                        response.AttemptCount = attempt;
                        response.Errors = errorHistory;
                        response.TotalProcessingTime = DateTime.UtcNow - startTime;
                        response.IsSqlValid = false;
                        response.ValidationMessage = $"Failed validation on attempt {attempt}";
                        
                        return response;
                    }
                    
                    attempt++;
                    
                    // Exponential backoff
                    var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1));
                    _logger.LogInformation("Waiting {Delay}ms before attempt {NextAttempt}", delay.TotalMilliseconds, attempt);
                    await Task.Delay(delay);
                }
            }

            response.ErrorMessage = "Unexpected error in retry loop";
            response.TotalProcessingTime = DateTime.UtcNow - startTime;
            return response;
        }

        private async Task<string> GenerateSqlWithErrorContext(string query, List<string> errors, int attempt)
        {
            var schemaDescription = _configuration["Database:Schema"] ?? "No schema available";
            var schemaAnalysis = AnalyzeSchemaIntelligently(schemaDescription);
            
            var errorContext = errors.Any() ? 
                $"\n\nPREVIOUS ERRORS TO AVOID:\n{string.Join("\n", errors.Select((e, i) => $"{i + 1}. {e}"))}" : 
                "";

            var correctiveActions = GetCorrectiveActions(errors);

            var enhancedPrompt = $@"You are an intelligent SQL Agent. This is attempt {attempt}/4 for generating SQL.

Database Schema:
{schemaDescription}

INTELLIGENT SCHEMA ANALYSIS:
{schemaAnalysis}

QUERY: {query}
{errorContext}

LEARNING FROM ERRORS:
- If previous attempts had column name errors, double-check exact column names in schema
- If syntax errors occurred, ensure T-SQL compatibility (TOP not LIMIT, etc.)
- If relationship errors happened, verify foreign key connections carefully
- If validation failed, ensure only SELECT statements

CORRECTIVE ACTIONS:
{correctiveActions}

CRITICAL T-SQL REQUIREMENTS:
- Use TOP N syntax (NOT LIMIT N)
- Use square brackets for reserved words: [Order], [User], [Date]
- Use GETDATE() for current date
- Join syntax: FROM TableA a JOIN TableB b ON a.Id = b.TableA_Id

Generate improved SQL taking into account all previous errors. Return ONLY the SQL query.";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = enhancedPrompt },
                    new { role = "user", content = $"Generate corrected SQL for attempt {attempt}: {query}" }
                },
                temperature = 0.1 + (attempt * 0.05), // Збільшуємо креативність з кожною спробою
                max_tokens = 600
            };

            var json = JsonConvert.SerializeObject(requestBody, Formatting.Indented);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI API error on attempt {attempt}: {response.StatusCode}");
            }
            
            var openAIResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
            var sqlQuery = openAIResponse?.choices?[0]?.message?.content?.ToString();
            
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                throw new InvalidOperationException($"No SQL query generated by AI on attempt {attempt}");
            }
            
            return CleanSqlResponse(sqlQuery);
        }

        private string GetCorrectiveActions(List<string> errors)
        {
            var actions = new List<string>();
            
            if (errors.Any(e => e.Contains("Invalid column name") || e.Contains("column")))
                actions.Add("- CRITICAL: Verify all column names exactly match the schema - do not guess or abbreviate");
                
            if (errors.Any(e => e.Contains("LIMIT") || e.Contains("limit")))
                actions.Add("- CRITICAL: Use TOP instead of LIMIT for T-SQL - this is SQL Server, not MySQL");
                
            if (errors.Any(e => e.Contains("Invalid object name") || e.Contains("object")))
                actions.Add("- CRITICAL: Check table names and use proper schema prefixes (dbo.)");
                
            if (errors.Any(e => e.Contains("foreign key") || e.Contains("relationship")))
                actions.Add("- CRITICAL: Validate JOIN relationships and foreign key column names");
                
            if (errors.Any(e => e.Contains("syntax") || e.Contains("Syntax")))
                actions.Add("- CRITICAL: Review T-SQL syntax - ensure proper FROM, JOIN, WHERE clause structure");
                
            if (errors.Any(e => e.Contains("Validation") || e.Contains("validation")))
                actions.Add("- CRITICAL: Ensure only SELECT statements - no INSERT, UPDATE, DELETE, DROP allowed");

            return actions.Any() ? string.Join("\n", actions) : "- Carefully review all syntax and schema references";
        }

        private (bool IsValid, string Message) ValidateGeneratedSql(string sql)
        {
            // Базова валідація
            if (string.IsNullOrWhiteSpace(sql))
                return (false, "SQL query cannot be empty");

            var upperSql = sql.ToUpperInvariant().Trim();
            
            if (!upperSql.StartsWith("SELECT"))
                return (false, "Only SELECT statements are allowed");
                
            if (upperSql.Contains("DROP") || upperSql.Contains("DELETE") || upperSql.Contains("INSERT") || upperSql.Contains("UPDATE"))
                return (false, "Dangerous SQL operations detected");
                
            if (upperSql.Contains("LIMIT"))
                return (false, "Use TOP instead of LIMIT for T-SQL");

            return (true, "Query passed basic validation");
        }
    }
}