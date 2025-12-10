namespace DatabaseDemo.Models
{
    public class ChatRequest
    {
        public string Query { get; set; } = string.Empty;
        public bool IsAgentMode { get; set; } = false;
    }
}