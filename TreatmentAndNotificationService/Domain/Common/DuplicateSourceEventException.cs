namespace TreatmentAndNotificationService.Domain.Common;

/// <summary>Raised when an idempotency key has already produced a notification.</summary>
public sealed class DuplicateSourceEventException(string sourceEventId)
    : Exception($"A notification for source event '{sourceEventId}' already exists.");
