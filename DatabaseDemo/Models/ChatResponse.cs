namespace DatabaseDemo.Models
{
    public class ChatResponse
    {
        public string OriginalQuery { get; set; } = string.Empty;
        public string GeneratedSql { get; set; } = string.Empty;
        public bool IsSqlValid { get; set; }
        public string ValidationMessage { get; set; } = string.Empty;
        public List<Dictionary<string, object>>? ExecutionResult { get; set; }
        public string FinalAnswer { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        
        // Нові поля для retry логіки
        public int AttemptCount { get; set; } = 1;
        public List<string> Errors { get; set; } = new();
        public bool IsRetrySuccess => AttemptCount > 1 && string.IsNullOrEmpty(ErrorMessage);
        public TimeSpan TotalProcessingTime { get; set; }
    }
}