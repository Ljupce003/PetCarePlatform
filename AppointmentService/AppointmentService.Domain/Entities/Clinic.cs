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
