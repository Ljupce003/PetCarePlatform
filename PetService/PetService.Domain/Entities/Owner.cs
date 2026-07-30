using System.Net.Mail;

namespace PetService.Domain.Entities;

public class Owner
{
    public Guid OwnerId { get; private set; }
    public string OwnerName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Address { get; private set; }

    // Used by EF Core when loading database records.
    private Owner()
    {
    }

    // Used by our application when creating a new valid owner.
    public Owner(string ownerName, string email, string phone, string? address)
    {
        OwnerId = Guid.NewGuid();
        Update(ownerName, email, phone, address);
    }

    
    public void Update(string ownerName, string email, string phone, string? address)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
        {
            throw new ArgumentException("Owner name is required.", nameof(ownerName));
        }

        if (!MailAddress.TryCreate(email, out var parsedEmail) ||
            !string.Equals(parsedEmail.Address, email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A valid email is required.", nameof(email));
        }

        if (!IsValidPhone(phone))
        {
            throw new ArgumentException("A valid phone number containing 7 to 15 digits is required.", nameof(phone));
        }

        OwnerName = ownerName.Trim();
        Email = parsedEmail.Address.ToLowerInvariant();
        Phone = phone.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
    }

    private static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var normalized = phone.Trim();
        if (normalized.Any(character =>
                !char.IsDigit(character) && character is not ('+' or ' ' or '-' or '(' or ')')))
        {
            return false;
        }

        var digitCount = normalized.Count(char.IsDigit);
        var plusCount = normalized.Count(character => character == '+');
        return digitCount is >= 7 and <= 15 && (plusCount == 0 || plusCount == 1 && normalized[0] == '+');
    }
}
