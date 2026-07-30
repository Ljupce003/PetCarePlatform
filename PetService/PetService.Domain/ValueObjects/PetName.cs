namespace PetService.Domain.ValueObjects;

public record PetName
{
    private PetName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PetName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Pet name is required.", nameof(value));
        }

        return new PetName(value.Trim());
    }

    public override string ToString() => Value;
}
