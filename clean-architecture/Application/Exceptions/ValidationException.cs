namespace Application.Exceptions;

public class ValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base(ExceptionType.Validation, "One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message)
        : base(ExceptionType.Validation, message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, IDictionary<string, string[]> errors)
        : base(ExceptionType.Validation, message)
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base(ExceptionType.Validation, "One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}
