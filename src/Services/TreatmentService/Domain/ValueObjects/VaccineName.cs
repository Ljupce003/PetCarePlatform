using TreatmentAndNotificationService.Domain.Common;

namespace TreatmentAndNotificationService.Domain.ValueObjects;

public sealed record VaccineName
{
    public const int MaximumLength = 150;
    public string Value { get; }

    private VaccineName(string value) => Value = value;

    public static VaccineName Create(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new DomainValidationException("Vaccine name is required.");
        if (normalized.Length > MaximumLength)
            throw new DomainValidationException($"Vaccine name cannot exceed {MaximumLength} characters.");
        return new VaccineName(normalized);
    }

    public override string ToString() => Value;
}
