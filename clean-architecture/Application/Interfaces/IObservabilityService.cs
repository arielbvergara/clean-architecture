namespace Application.Interfaces;

/// <summary>
/// Application-level abstraction for capturing observability signals such as
/// errors and warnings without depending on any specific monitoring provider.
///
/// Implementations are expected to be non-throwing and to degrade gracefully
/// when the underlying monitoring backend is unavailable.
/// </summary>
public interface IObservabilityService
{
    Task CaptureErrorAsync(
        Exception exception,
        string? message = null,
        IReadOnlyDictionary<string, object?>? context = null,
        CancellationToken cancellationToken = default);

    Task CaptureWarningAsync(
        string message,
        IReadOnlyDictionary<string, object?>? context = null,
        CancellationToken cancellationToken = default);
}
