using TreatmentAndNotificationService.Domain.Common;

namespace TreatmentAndNotificationService.Domain.ValueObjects;

public sealed record SourceEventId
{
    public const int MaximumLength = 200;
    public string Value { get; }

    private SourceEventId(string value) => Value = value;

    public static SourceEventId Create(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new DomainValidationException("Notification source event id is required.");
        if (normalized.Length > MaximumLength)
            throw new DomainValidationException($"Notification source event id cannot exceed {MaximumLength} characters.");
        return new SourceEventId(normalized);
    }

    public override string ToString() => Value;
}
