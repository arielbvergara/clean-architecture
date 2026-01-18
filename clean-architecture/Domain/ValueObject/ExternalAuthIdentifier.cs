namespace Domain.ValueObject;

public sealed record ExternalAuthIdentifier
{
    public string Value { get; }

    private ExternalAuthIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("External auth identifier cannot be empty", nameof(value));
        if (value.Length > 255)
            throw new ArgumentException("External auth identifier cannot exceed 255 characters", nameof(value));

        Value = value.Trim();
    }

    public static ExternalAuthIdentifier Create(string value) => new(value);
}
