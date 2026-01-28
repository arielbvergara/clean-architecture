namespace Application.Interfaces;

/// <summary>
/// Central place for well-known security event names so they do not appear
/// as magic strings across the application and WebAPI layers.
/// </summary>
public static class SecurityEventNames
{
    public const string UserCreated = "user.created";
    public const string UserCreateFailed = "user.create.failed";
    public const string UserUpdated = "user.updated";
    public const string UserUpdateFailed = "user.update.failed";
    public const string UserDeleted = "user.deleted";
    public const string UserDeleteFailed = "user.delete.failed";
}

/// <summary>
/// Centralizes allowed outcome values for security events.
/// </summary>
public static class SecurityEventOutcomes
{
    public const string Success = "Success";
    public const string Failure = "Failure";
}

/// <summary>
/// Abstraction for publishing security-relevant events such as user lifecycle
/// changes or authorization failures to logging and alerting pipelines.
/// </summary>
public interface ISecurityEventNotifier
{
    /// <summary>
    /// Notify observers about a security-relevant event. Implementations are
    /// expected to be non-throwing and to avoid leaking sensitive data.
    /// </summary>
    Task NotifyAsync(
        string eventName,
        string? subjectId,
        string outcome,
        string? correlationId,
        IReadOnlyDictionary<string, string?>? properties = null,
        CancellationToken cancellationToken = default);
}
