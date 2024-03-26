namespace OCIRegistry.Models
{
    public class ErrorResponse
    {
        public List<Error> Errors { get; set; } = new();


        static ErrorResponse FromError(string code, string message)
        {
            return new ErrorResponse()
            {
                Errors = new List<Error>() { new Error() { Code = code, Message = message } }
            };
        }
    }

    public class Error
    {
        public required string Code { get; set; }
        public required string Message { get; set; }
        public string? Detail { get; set; }
    }
}
