namespace TreatmentAndNotificationService.Domain.Entities;

public class Vaccination
{
    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid VeterinarianId { get; private set; }
    public string VaccineName { get; private set; } = string.Empty;
    public DateOnly AdministeredOn { get; private set; }
    public DateOnly? NextDueOn { get; private set; }
    public string? BatchNumber { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    
    
    private Vaccination() { }

    public Vaccination(Guid petId, Guid ownerId, Guid veterinarianId, string vaccineName,
        DateOnly administeredOn, DateOnly? nextDueOn, string? batchNumber)
    {
        
        if (petId == Guid.Empty || ownerId == Guid.Empty || veterinarianId == Guid.Empty)
            throw new ArgumentException("Pet, owner and veterinarian are required.");
        
        if (string.IsNullOrWhiteSpace(vaccineName)) throw new ArgumentException("Vaccine name is required.");
        
        if (nextDueOn.HasValue && nextDueOn.Value <= administeredOn)
            throw new ArgumentException("Next vaccine date must be after the administration date.");
        
        Id = Guid.NewGuid();
        PetId = petId;
        OwnerId = ownerId;
        VeterinarianId = veterinarianId;
        VaccineName = vaccineName.Trim();
        AdministeredOn = administeredOn;
        NextDueOn = nextDueOn;
        BatchNumber = string.IsNullOrWhiteSpace(batchNumber) ? null : batchNumber.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }
    
}