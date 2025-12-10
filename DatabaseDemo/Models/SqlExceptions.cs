namespace DatabaseDemo.Models
{
    public class SqlValidationException : Exception
    {
        public SqlValidationException(string message) : base(message) { }
        public SqlValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public class SqlExecutionException : Exception
    {
        public SqlExecutionException(string message) : base(message) { }
        public SqlExecutionException(string message, Exception innerException) : base(message, innerException) { }
    }
}