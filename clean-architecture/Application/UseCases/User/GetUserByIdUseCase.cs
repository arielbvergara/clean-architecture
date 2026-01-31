using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.User;

public class GetUserByIdUseCase(IUserRepository userRepository)
{
    public async Task<Result<UserResponse, AppException>> ExecuteAsync(GetUserByIdRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserId.Create(request.UserId);

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result<UserResponse, AppException>.Fail(new NotFoundException("User", request.UserId));
            }

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
