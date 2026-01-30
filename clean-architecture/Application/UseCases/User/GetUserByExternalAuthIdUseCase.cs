using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.User;

public class GetUserByExternalAuthIdUseCase(IUserRepository userRepository)
{
    public async Task<Result<UserResponse, AppException>> ExecuteAsync(GetUserByExternalAuthIdRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var externalAuthId = ExternalAuthIdentifier.Create(request.ExternalAuthId);

            var user = await userRepository.GetByExternalAuthIdAsync(externalAuthId, cancellationToken);
            if (user is null)
                return Result<UserResponse, AppException>.Fail(new NotFoundException($"User with external auth ID not found"));

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
