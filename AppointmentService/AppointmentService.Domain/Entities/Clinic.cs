namespace AppointmentService.Domain.Entities;

public class Clinic
{
    public Guid ClinicId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;

    // Used by EF Core when loading database records.
    private Clinic()
    {
    }

    // Used by our application when creating a new valid clinic.
    public Clinic(string name, string location, string address)
    {
        ClinicId = Guid.NewGuid();
        Update(name, location, address);
    }

    /// <summary>
    /// Creates a clinic with a known, fixed id instead of a random one. Used by the
    /// Infrastructure layer's demo seed data, so a presentation can reference clinics/
    /// veterinarians/slots by a stable id instead of having to search for them first.
    /// </summary>
    public static Clinic Seed(Guid clinicId, string name, string location, string address)
    {
        var clinic = new Clinic(name, location, address) { ClinicId = clinicId };
        return clinic;
    }

    public void Update(string name, string location, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Clinic name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Clinic location is required.", nameof(location));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Clinic address is required.", nameof(address));
        }

        Name = name.Trim();
        Location = location.Trim();
        Address = address.Trim();
    }
}
