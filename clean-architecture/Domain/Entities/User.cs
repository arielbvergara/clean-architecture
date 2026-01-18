using Domain.ValueObject;

namespace Domain.Entities;

public sealed class User
{
    public UserId Id { get; }
    public Email Email { get; }
    public UserName Name { get; private set; }
    public ExternalAuthIdentifier ExternalAuthId { get; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private User(UserId id, Email email, UserName name, ExternalAuthIdentifier externalAuthId, DateTime createdAt)
    {
        Id = id;
        Email = email;
        Name = name;
        ExternalAuthId = externalAuthId;
        CreatedAt = createdAt;
    }

    public static User Create(Email email, UserName name, ExternalAuthIdentifier externalAuthId)
    {
        var user = new User(UserId.NewId(), email, name, externalAuthId, DateTime.UtcNow);
        return user;
    }

    public void UpdateName(UserName name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}