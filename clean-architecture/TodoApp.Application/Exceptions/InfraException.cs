namespace TodoApp.Application.Exceptions;

public class InfraException : AppException
{
    public string? ServiceName { get; }

    public InfraException(string message)
        : base(ExceptionType.Infrastructure, message)
    {
    }

    public InfraException(string serviceName, string message)
        : base(ExceptionType.Infrastructure, $"Infrastructure error in {serviceName}: {message}")
    {
        ServiceName = serviceName;
    }

    public InfraException(string message, Exception innerException)
        : base(ExceptionType.Infrastructure, message, innerException)
    {
    }

    public InfraException(string serviceName, string message, Exception innerException)
        : base(ExceptionType.Infrastructure, $"Infrastructure error in {serviceName}: {message}", innerException)
    {
        ServiceName = serviceName;
    }
}