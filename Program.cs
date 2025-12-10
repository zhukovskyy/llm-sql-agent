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
}

app.UseHttpsRedirection();
app.UseCors();
app.UseStaticFiles(); // Enable static files from wwwroot

// Database schema endpoint for debugging
app.MapGet("/schema", async (SqlExecutor sqlExecutor) =>
{
    try
    {
        var schema = await sqlExecutor.GetDatabaseSchemaAsync();
        return Results.Ok(new { Schema = schema });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
})
.WithName("GetDatabaseSchema")
.WithOpenApi();

// Chat endpoint
app.MapPost("/chat", async (ChatRequest request, LlmAgent llmAgent, SqlSandbox sqlSandbox, SqlExecutor sqlExecutor, IConfiguration configuration) =>
{
    var response = new ChatResponse
    {
        OriginalQuery = request.Query
    };

    try
    {
        // Update database schema in configuration if not set
        var currentSchema = configuration["Database:Schema"];
        if (string.IsNullOrEmpty(currentSchema) || currentSchema.Contains("auto-detected"))
        {
            var schema = await sqlExecutor.GetDatabaseSchemaAsync();
            configuration["Database:Schema"] = schema;
        }

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
app.MapGet("/health", async (SqlExecutor sqlExecutor) =>
{
    var dbConnection = await sqlExecutor.TestConnectionAsync();
    return Results.Ok(new { 
        Database = dbConnection ? "Connected" : "Disconnected",
        Timestamp = DateTime.UtcNow
    });
})
.WithName("Health")
.WithOpenApi();

app.Run();