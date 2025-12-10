using DatabaseDemo.Models;
using DatabaseDemo.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON options for camelCase
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Add HTTP client for OpenAI
builder.Services.AddHttpClient<LlmAgent>();

// Add custom services
builder.Services.AddScoped<LlmAgent>();
builder.Services.AddScoped<SqlSandbox>();
builder.Services.AddScoped<SqlExecutor>();
builder.Services.AddScoped<DatabaseInitializer>();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Disable HTTPS redirection in development for easier testing
    // app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseStaticFiles(); // Enable static files from wwwroot

// Database schema endpoint for debugging
app.MapGet("/schema", async (SqlExecutor sqlExecutor, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Schema endpoint called");
        var schema = await sqlExecutor.GetDatabaseSchemaAsync();
        logger.LogInformation("Schema retrieved successfully, length: {Length}", schema?.Length ?? 0);
        logger.LogInformation("Schema preview: {Preview}", schema?.Substring(0, Math.Min(200, schema?.Length ?? 0)));
        
        var result = new { Schema = schema };
        var jsonResult = System.Text.Json.JsonSerializer.Serialize(result);
        logger.LogInformation("JSON result length: {Length}", jsonResult.Length);
        
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in schema endpoint: {Error}", ex.Message);
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("GetDatabaseSchema")
.WithOpenApi();

// Chat endpoint
app.MapPost("/chat", async (HttpContext context, LlmAgent llmAgent, SqlSandbox sqlSandbox, SqlExecutor sqlExecutor, IConfiguration configuration, ILogger<Program> logger) =>
{
    // Read and log the raw request body
    context.Request.EnableBuffering();
    var rawBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    context.Request.Body.Position = 0;
    logger.LogInformation("Raw request body: {Body}", rawBody);
    
    var request = await context.Request.ReadFromJsonAsync<ChatRequest>();
    logger.LogInformation("Parsed ChatRequest - Query: {Query}, IsAgentMode: {IsAgentMode}", request?.Query, request?.IsAgentMode);
    
    if (request == null)
    {
        return Results.BadRequest(new { Error = "Invalid request" });
    }
    
    var response = new ChatResponse
    {
        OriginalQuery = request.Query
    };

    try
    {
        logger.LogInformation("=== CHAT REQUEST DEBUG ===");
        logger.LogInformation("Chat request received. IsAgentMode: {IsAgentMode}, Query: {Query}", request.IsAgentMode, request.Query);
        logger.LogInformation("Request object type: {Type}", request.GetType().FullName);
        logger.LogInformation("IsAgentMode property value: {Value}", request.IsAgentMode);
        logger.LogInformation("========================");
        
        // Update database schema in configuration if not set
        var currentSchema = configuration["Database:Schema"];
        if (string.IsNullOrEmpty(currentSchema) || currentSchema.Contains("auto-detected"))
        {
            var schema = await sqlExecutor.GetDatabaseSchemaAsync();
            configuration["Database:Schema"] = schema;
        }

        if (request.IsAgentMode)
        {
            logger.LogInformation("🤖 Running ReAct agent loop...");
            response = await llmAgent.RunReActLoopAsync(request.Query);
            logger.LogInformation("✅ ReAct loop finished. Has reasoning trace: {HasTrace}, Trace length: {Length}", 
                !string.IsNullOrEmpty(response.ReasoningTrace), 
                response.ReasoningTrace?.Length ?? 0);
        }
        else
        {
            // Generate SQL query using LLM
            response.GeneratedSql = await llmAgent.GenerateSqlQueryAsync(request.Query);

            // Validate SQL query
            var (isValid, validationMessage) = sqlSandbox.ValidateQuery(response.GeneratedSql);
            response.IsSqlValid = isValid;
            response.ValidationMessage = validationMessage;

            if (response.IsSqlValid)
            {
                // Execute SQL query
                response.ExecutionResult = await sqlExecutor.ExecuteQueryAsync(response.GeneratedSql);

                // Generate natural language response
                response.FinalAnswer = await llmAgent.GenerateNaturalLanguageResponseAsync(
                    request.Query, response.ExecutionResult);
            }
            else
            {
                response.FinalAnswer = $"Query validation failed: {response.ValidationMessage}";
            }
        }
    }
    catch (Exception ex)
    {
        response.ErrorMessage = ex.Message;
        response.FinalAnswer = "An error occurred while processing your request.";
    }

    return Results.Ok(response);
})
.WithName("Chat")
.WithOpenApi();

// Health check endpoint
app.MapGet("/health", async (SqlExecutor sqlExecutor, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Health endpoint called");
        var dbConnection = await sqlExecutor.TestConnectionAsync();
        var result = new { 
            Database = dbConnection ? "Connected" : "Disconnected",
            Timestamp = DateTime.UtcNow,
            Status = dbConnection
        };
        logger.LogInformation("Health check result: {Result}", System.Text.Json.JsonSerializer.Serialize(result));
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in health endpoint: {Error}", ex.Message);
        return Results.Ok(new { 
            Database = "Error",
            Timestamp = DateTime.UtcNow,
            Status = false,
            Error = ex.Message
        });
    }
})
.WithName("Health")
.WithOpenApi();

// Database initialization endpoint
app.MapPost("/api/init-database", async (DatabaseInitializer dbInitializer, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Database initialization endpoint called");
        var (success, message) = await dbInitializer.InitializeDatabase();
        
        if (success)
        {
            logger.LogInformation("Database initialization completed successfully");
            return Results.Ok(new { Success = true, Message = message });
        }
        else
        {
            logger.LogWarning("Database initialization failed: {Message}", message);
            return Results.BadRequest(new { Success = false, Message = message });
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in database initialization endpoint: {Error}", ex.Message);
        return Results.BadRequest(new { Success = false, Message = $"❌ Error: {ex.Message}" });
    }
})
.WithName("InitializeDatabase")
.WithOpenApi();

app.Run();
