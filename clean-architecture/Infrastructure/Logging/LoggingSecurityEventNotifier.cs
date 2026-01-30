using Application.Interfaces;

namespace Infrastructure.Logging;

/// <summary>
/// Default implementation of <see cref="ISecurityEventNotifier"/> that emits
/// structured log entries which can be picked up by centralized logging and
/// alerting systems.
///
/// When appropriate, selected failure events are also forwarded to the
/// application-level observability service so they can be surfaced to
/// monitoring providers such as Sentry without leaking provider details into
/// controllers or use cases.
/// </summary>
public sealed class LoggingSecurityEventNotifier(IObservabilityService observabilityService)
    : ISecurityEventNotifier
{
    public async Task NotifyAsync(
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

        // Forward selected high-value failure events to the observability
        // service so that they can be surfaced to monitoring providers such as
        // Sentry without coupling controllers directly to those providers.
        var isFailure = string.Equals(outcome, SecurityEventOutcomes.Failure, StringComparison.OrdinalIgnoreCase);
        var isHighValueFailure = IsHighValueFailure(eventName);

        if (isFailure && isHighValueFailure)
        {
            var observabilityContext = new Dictionary<string, object?>(eventProperties.Count);
            foreach (var pair in eventProperties)
            {
                observabilityContext[pair.Key] = pair.Value;
            }

            var message = $"Security event {eventName} {outcome} for subject {subjectId}";
            await observabilityService.CaptureWarningAsync(message, observabilityContext, cancellationToken);
        }
    }

    private static bool IsHighValueFailure(string eventName)
    {
        return eventName is
            SecurityEventNames.UserCreateFailed or
            SecurityEventNames.UserUpdateFailed or
            SecurityEventNames.UserDeleteFailed or
            SecurityEventNames.AdminEndpointAccessDenied;
    }
}
