using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging;

/// <summary>
/// Observability implementation that writes to the standard ASP.NET Core
/// logging pipeline and, when Sentry is configured, forwards errors and
/// selected warnings to Sentry.
///
/// This type lives in the Infrastructure layer so that the Application layer
/// depends only on the IObservabilityService abstraction and is unaware of
/// Sentry-specific details.
/// </summary>
public sealed class SentryObservabilityService(ILogger<SentryObservabilityService> logger)
    : IObservabilityService
{
    private const string ErrorScopeMessageTagName = "observability.error_message";

    public Task CaptureErrorAsync(
        Exception exception,
        string? message = null,
        IReadOnlyDictionary<string, object?>? context = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedMessage = string.IsNullOrWhiteSpace(message)
            ? "Unhandled error captured by observability service."
            : message;

        logger.LogError(exception, "{Message} {@Context}", resolvedMessage, context);

        if (SentrySdk.IsEnabled)
        {
            SentrySdk.CaptureException(exception, scope =>
            {
                scope.Level = SentryLevel.Error;
                scope.SetTag(ErrorScopeMessageTagName, resolvedMessage);

                if (context is not null)
                {
                    foreach (var pair in context)
                    {
                        scope.SetExtra(pair.Key, pair.Value);
                    }
                }
            });
        }

        return Task.CompletedTask;
    }

    public Task CaptureWarningAsync(
        string message,
        IReadOnlyDictionary<string, object?>? context = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning("{Message} {@Context}", message, context);

        if (SentrySdk.IsEnabled)
        {
            // Lightweight warning capture without additional scope data; callers
            // can encode important context in the message or via tags/extras
            // using CaptureErrorAsync when needed.
            SentrySdk.CaptureMessage(message, SentryLevel.Warning);
        }

        return Task.CompletedTask;
    }
}
