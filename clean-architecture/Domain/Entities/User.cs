using Domain.ValueObject;

namespace Domain.Entities;

public sealed class User
{
    public UserId Id { get; }
    public Email Email { get; }
    public UserName Name { get; private set; }
    public ExternalAuthIdentifier ExternalAuthId { get; }
    public string Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private User(
        UserId id,
        Email email,
        UserName name,
        ExternalAuthIdentifier externalAuthId,
        string role,
        DateTime createdAt,
        bool isDeleted,
        DateTime? deletedAt)
    {
        Id = id;
        Email = email;
        Name = name;
        ExternalAuthId = externalAuthId;
        Role = role;
        CreatedAt = createdAt;
        IsDeleted = isDeleted;
        DeletedAt = deletedAt;
    }

    public static User Create(Email email, UserName name, ExternalAuthIdentifier externalAuthId)
    {
        var user = new User(
            UserId.NewId(),
            email,
            name,
            externalAuthId,
            role: "User",
            createdAt: DateTime.UtcNow,
            isDeleted: false,
            deletedAt: null);
        return user;
    }

    public void UpdateName(UserName name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDeleted()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
