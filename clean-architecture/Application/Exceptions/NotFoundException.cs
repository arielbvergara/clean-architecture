namespace Application.Exceptions;

public class NotFoundException : AppException
{
    public string ResourceType { get; }
    public object ResourceId { get; }

    public NotFoundException(string resourceType, object resourceId)
        : base(ExceptionType.NotFound, $"{resourceType} with id '{resourceId}' was not found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string message)
        : base(ExceptionType.NotFound, message)
    {
        ResourceType = string.Empty;
        ResourceId = string.Empty;
    }

    public NotFoundException(string message, Exception innerException)
        : base(ExceptionType.NotFound, message, innerException)
    {
        ResourceType = string.Empty;
        ResourceId = string.Empty;
    }
}
