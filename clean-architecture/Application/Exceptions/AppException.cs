namespace Application.Exceptions;

public abstract class AppException : Exception
{
    public ExceptionType Type { get; }

    protected AppException(ExceptionType type, string message)
        : base(message)
    {
        Type = type;
    }

    protected AppException(ExceptionType type, string message, Exception innerException)
        : base(message, innerException)
    {
        Type = type;
    }
}

public enum ExceptionType
{
    Validation,
    NotFound,
    Conflict,
    Infrastructure,
    General
}
