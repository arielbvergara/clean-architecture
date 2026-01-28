namespace Application.Dtos.User;

/// <summary>
/// Represents the identity and authorization context of the caller as understood by the
/// application layer. This abstraction allows use cases to perform fine-grained access
/// control checks without depending on ASP.NET Core or specific authentication providers.
/// </summary>
public sealed record CurrentUserContext(
    string UserId,
    string Role
);
