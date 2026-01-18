namespace Domain.ValueObject;

public sealed record UserId
{
    public Guid Value { get; }

    private UserId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(value));
        Value = value;
    }

    public static UserId Create(Guid value) => new(value);
    public static UserId NewId() => new(Guid.NewGuid());
}