using TreatmentAndNotificationService.Domain.Common;

namespace TreatmentAndNotificationService.Domain.ValueObjects;

public sealed record TreatmentPlan
{
    public const int MaximumLength = 2000;
    public string Value { get; }

    private TreatmentPlan(string value) => Value = value;

    public static TreatmentPlan Create(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new DomainValidationException("Treatment plan is required.");
        if (normalized.Length > MaximumLength)
            throw new DomainValidationException($"Treatment plan cannot exceed {MaximumLength} characters.");
        return new TreatmentPlan(normalized);
    }

    public override string ToString() => Value;
}
