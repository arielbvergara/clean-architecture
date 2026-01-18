namespace TodoApp.Domain.ValueObject;

public sealed record UserName
{
    public string Value { get; }

    private UserName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("User name cannot be empty", nameof(value));
        if (value.Length < 2)
            throw new ArgumentException("User name must be at least 2 characters", nameof(value));
        if (value.Length > 100)
            throw new ArgumentException("User name cannot exceed 100 characters", nameof(value));

        Value = value.Trim();
    }

    public static UserName Create(string value) => new(value);
}