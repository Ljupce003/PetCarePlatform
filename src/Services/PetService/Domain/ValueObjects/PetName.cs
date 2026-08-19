namespace PetService.Domain.ValueObjects;

public record PetName : IComparable<PetName>
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

    public int CompareTo(PetName? other) =>
        other is null ? 1 : string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}
