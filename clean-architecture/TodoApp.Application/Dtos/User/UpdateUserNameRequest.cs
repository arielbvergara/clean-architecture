namespace TodoApp.Application.Dtos.User;

public record UpdateUserNameRequest(Guid UserId, string NewName);