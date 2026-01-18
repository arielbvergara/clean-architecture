namespace Application.Exceptions;

public class ConflictException : AppException
{
    public string ResourceType { get; }
    public object? ResourceId { get; }

    public ConflictException(string message)
        : base(ExceptionType.Conflict, message)
    {
        ResourceType = string.Empty;
    }

    public ConflictException(string resourceType, object resourceId, string message)
        : base(ExceptionType.Conflict, $"{resourceType} with id '{resourceId}' {message}")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public ConflictException(string message, Exception innerException)
        : base(ExceptionType.Conflict, message, innerException)
    {
        ResourceType = string.Empty;
    }
}