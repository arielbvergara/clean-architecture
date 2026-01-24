using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.User;

public class UpdateUserNameUseCase(IUserRepository userRepository)
{
    public async Task<Result<UserResponse, AppException>> ExecuteAsync(UpdateUserNameRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserId.Create(request.UserId);
            var newName = UserName.Create(request.NewName);

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<UserResponse, AppException>.Fail(new NotFoundException("User", request.UserId));

            user.UpdateName(newName);

            await userRepository.UpdateAsync(user, cancellationToken);

            return Result<UserResponse, AppException>.Ok(MapToResponse(user));
        }
        catch (AppException ex)
        {
            return Result<UserResponse, AppException>.Fail(ex);
        }
        catch (ArgumentException ex)
        {
            return Result<UserResponse, AppException>.Fail(new ValidationException(ex.Message));
        }
        catch (Exception ex)
        {
            return Result<UserResponse, AppException>.Fail(new InfraException("An unexpected error occurred", ex));
        }
    }

    private static UserResponse MapToResponse(Domain.Entities.User user)
    {
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