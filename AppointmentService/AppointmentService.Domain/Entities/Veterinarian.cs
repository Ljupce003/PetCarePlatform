namespace AppointmentService.Domain.Entities;

public class Veterinarian
{
    public Guid VeterinarianId { get; private set; }
    public Guid ClinicId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Specialization { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;

    // Whether this veterinarian is currently taking new appointments at all (independent of
    // any specific AvailabilitySlot, e.g. on leave). Defaults to available.
    public bool IsAvailable { get; private set; } = true;

    // Used by EF Core when loading database records.
    private Veterinarian()
    {
    }

    // Used by our application when creating a new valid veterinarian.
    public Veterinarian(Guid clinicId, string fullName, string specialization, string licenseNumber)
    {
        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic is required.", nameof(clinicId));
        }

        VeterinarianId = Guid.NewGuid();
        ClinicId = clinicId;
        Update(fullName, specialization, licenseNumber);
    }

    public void Update(string fullName, string specialization, string licenseNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Veterinarian name is required.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(specialization))
        {
            throw new ArgumentException("Specialization is required.", nameof(specialization));
        }

        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            throw new ArgumentException("License number is required.", nameof(licenseNumber));
        }

        FullName = fullName.Trim();
        Specialization = specialization.Trim();
        LicenseNumber = licenseNumber.Trim();
    }

    public void MarkUnavailable() => IsAvailable = false;

    public void MarkAvailable() => IsAvailable = true;
}
