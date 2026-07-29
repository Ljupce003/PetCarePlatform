using PetService.Domain.Exceptions;

namespace PetService.Domain.ValueObjects;

public record MicrochipNumber
{
    private MicrochipNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MicrochipNumber? Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length is < 8 or > 30 || normalized.Any(character => !char.IsLetterOrDigit(character)))
        {
            throw new InvalidMicrochipException(value);
        }

        return new MicrochipNumber(normalized);
    }

    public override string ToString() => Value;
}
