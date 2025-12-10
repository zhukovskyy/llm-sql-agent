using DatabaseDemo.Models;
using DatabaseDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Chat endpoint with retry logic
app.MapPost("/chat", async (ChatRequest request, LlmAgent llmAgent, SqlSandbox sqlSandbox, SqlExecutor sqlExecutor, IConfiguration configuration, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Chat endpoint called with retry agent for query: {Query}", request.Query);
        
        // Update database schema in configuration if not set
        var currentSchema = configuration["Database:Schema"];
        if (string.IsNullOrEmpty(currentSchema) || currentSchema.Contains("auto-detected"))
        {
            var schema = await sqlExecutor.GetDatabaseSchemaAsync();
            configuration["Database:Schema"] = schema;
        }

        // Use the new retry agent with real SqlSandbox and SqlExecutor
        var response = await llmAgent.GenerateSqlWithRetryAsync(request.Query, sqlSandbox, sqlExecutor);
        
        logger.LogInformation("Chat endpoint completed. Attempts: {Attempts}, Success: {Success}", 
            response.AttemptCount, string.IsNullOrEmpty(response.ErrorMessage));
            
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Critical error in chat endpoint: {Error}", ex.Message);
        return Results.Ok(new ChatResponse
        {
            OriginalQuery = request.Query,
            ErrorMessage = $"Critical system error: {ex.Message}",
            FinalAnswer = "A critical error occurred while processing your request.",
            AttemptCount = 1,
            Errors = new List<string> { $"System error: {ex.Message}" }
        });
    }
})
.WithName("ChatWithRetry")
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
