namespace Application.Dtos.User;

public sealed record GetUserByEmailRequest(
    string Email,
    CurrentUserContext? CurrentUser
);
