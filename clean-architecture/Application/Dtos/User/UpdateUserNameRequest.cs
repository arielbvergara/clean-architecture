namespace Application.Dtos.User;

public sealed record UpdateUserNameRequest(
    Guid UserId,
    string NewName,
    CurrentUserContext? CurrentUser
);
