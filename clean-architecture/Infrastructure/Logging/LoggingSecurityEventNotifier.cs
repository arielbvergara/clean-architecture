using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging;

/// <summary>
/// Default implementation of <see cref="ISecurityEventNotifier"/> that emits
/// structured log entries which can be picked up by centralized logging and
/// alerting systems.
/// </summary>
public sealed class LoggingSecurityEventNotifier(ILogger<LoggingSecurityEventNotifier> logger)
    : ISecurityEventNotifier
{
    public Task NotifyAsync(
        string eventName,
        string? subjectId,
        string outcome,
        string? correlationId,
        IReadOnlyDictionary<string, string?>? properties = null,
        CancellationToken cancellationToken = default)
    {
        var eventProperties = properties is null
            ? new Dictionary<string, string?>()
            : new Dictionary<string, string?>(properties);

        eventProperties["EventName"] = eventName;
        eventProperties["SubjectId"] = subjectId;
        eventProperties["Outcome"] = outcome;
        eventProperties["CorrelationId"] = correlationId;

        logger.LogInformation(
            "Security event {EventName} {Outcome} for subject {SubjectId} with correlation {CorrelationId} {@Properties}",
            eventName,
            outcome,
            subjectId ?? "<none>",
            correlationId ?? "<none>",
            eventProperties);

        return Task.CompletedTask;
    }
}
