namespace Application.Dtos.User;

public static class UserExtensions
{
    public static UserResponse ToUserResponse(this Domain.Entities.User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserResponse(
            user.Id.Value,
            user.Email.Value,
            user.Name.Value,
            user.ExternalAuthId.Value,
            user.CreatedAt,
            user.UpdatedAt,
            user.IsDeleted
        );
    }
}
