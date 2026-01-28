using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.User;

public class DeleteUserUseCase(IUserRepository userRepository)
{
    public async Task<Result<bool, AppException>> ExecuteAsync(DeleteUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserId.Create(request.UserId);

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result<bool, AppException>.Fail(new NotFoundException("User", request.UserId));
            }

            if (request.CurrentUser is not null &&
                !string.Equals(request.CurrentUser.Role, UserRoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            {
                var currentUserExternalId = ExternalAuthIdentifier.Create(request.CurrentUser.UserId);
                var currentUser = await userRepository.GetByExternalAuthIdAsync(currentUserExternalId, cancellationToken);

                if (currentUser is null || currentUser.Id != user.Id)
                {
                    // Anti-enumeration: behave as if the target user does not exist when
                    // the caller is not the owner and not an administrator.
                    return Result<bool, AppException>.Fail(new NotFoundException("User", request.UserId));
                }
            }

            await userRepository.DeleteAsync(userId, cancellationToken);

            return Result<bool, AppException>.Ok(true);
        }
        catch (AppException ex)
        {
            return Result<bool, AppException>.Fail(ex);
        }
        catch (ArgumentException ex)
        {
            return Result<bool, AppException>.Fail(new ValidationException(ex.Message));
        }
        catch (Exception ex)
        {
            return Result<bool, AppException>.Fail(new InfraException("An unexpected error occurred", ex));
        }
    }
}
