using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.User;

public class UpdateUserNameUseCase(IUserRepository userRepository, IUserOwnershipService userOwnershipService)
{
    public async Task<Result<UserResponse, AppException>> ExecuteAsync(UpdateUserNameRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserId.Create(request.UserId);
            var newName = UserName.Create(request.NewName);

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result<UserResponse, AppException>.Fail(new NotFoundException("User", request.UserId));
            }

            var ownershipError = await userOwnershipService.EnsureOwnerOrAdminAsync(
                user,
                request.CurrentUser,
                request.UserId,
                cancellationToken);

            if (ownershipError is not null)
            {
                return Result<UserResponse, AppException>.Fail(ownershipError);
            }

            user.UpdateName(newName);

            await userRepository.UpdateAsync(user, cancellationToken);

            return Result<UserResponse, AppException>.Ok(user.ToUserResponse());
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
}
