namespace Application.Dtos.User;

public sealed record DeleteUserRequest(
    Guid UserId,
    CurrentUserContext? CurrentUser
);
