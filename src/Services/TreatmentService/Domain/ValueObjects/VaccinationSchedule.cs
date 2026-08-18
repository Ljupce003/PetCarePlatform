using TreatmentAndNotificationService.Domain.Common;

namespace TreatmentAndNotificationService.Domain.ValueObjects;

public sealed record VaccinationSchedule
{
    public DateOnly AdministeredOn { get; }
    public DateOnly? NextDueOn { get; }

    private VaccinationSchedule(DateOnly administeredOn, DateOnly? nextDueOn) => (AdministeredOn, NextDueOn) = (administeredOn, nextDueOn);

    public static VaccinationSchedule Create(DateOnly administeredOn, DateOnly? nextDueOn)
    {
        if (administeredOn == default)
            throw new DomainValidationException("Vaccination administration date is required.");
        if (nextDueOn.HasValue && nextDueOn <= administeredOn)
            throw new DomainValidationException("Next vaccine date must be after the administration date.");
        return new VaccinationSchedule(administeredOn, nextDueOn);
    }
}
