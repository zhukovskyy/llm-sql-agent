namespace DatabaseDemo.Models
{
    public class OpenAIRequest
    {
        public string Model { get; set; } = "gpt-3.5-turbo";
        public List<OpenAIMessage> Messages { get; set; } = new();
        public List<OpenAIFunction>? Functions { get; set; }
        public string? FunctionCall { get; set; }
        public double Temperature { get; set; } = 0.1;
    }

    public class OpenAIMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public OpenAIFunctionCall? FunctionCall { get; set; }
    }

    public class OpenAIFunction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object Parameters { get; set; } = new { };
    }

    public class OpenAIFunctionCall
    {
        public string Name { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
    }

    public class OpenAIResponse
    {
        public List<OpenAIChoice> Choices { get; set; } = new();
    }

    public class OpenAIChoice
    {
        public OpenAIMessage Message { get; set; } = new();
    }
}