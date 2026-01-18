namespace TodoApp.Application.Dtos.User;

public record UserResponse(
    Guid Id,
    string Email,
    string Name,
    string ExternalAuthId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
