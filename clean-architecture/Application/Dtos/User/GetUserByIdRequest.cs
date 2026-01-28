namespace Application.Dtos.User;

public sealed record GetUserByIdRequest(
    Guid UserId,
    CurrentUserContext? CurrentUser
);
