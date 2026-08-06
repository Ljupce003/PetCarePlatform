using TreatmentAndNotificationService.Domain.Common;

namespace TreatmentAndNotificationService.Domain.ValueObjects;

public sealed record Diagnosis
{
    public const int MaximumLength = 500;
    public string Value { get; }

    private Diagnosis(string value) => Value = value;

    public static Diagnosis Create(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new DomainValidationException("Diagnosis is required.");
        if (normalized.Length > MaximumLength)
            throw new DomainValidationException($"Diagnosis cannot exceed {MaximumLength} characters.");
        return new Diagnosis(normalized);
    }

    public override string ToString() => Value;
}
