namespace TodoApp.Application.Dtos.User;

public record CreateUserRequest(string Email, string Name, string ExternalAuthId);
