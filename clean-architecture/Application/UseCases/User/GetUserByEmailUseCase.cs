using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.User;

public class GetUserByEmailUseCase(IUserRepository userRepository)
{
    public async Task<Result<UserResponse, AppException>> ExecuteAsync(GetUserByEmailRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = Email.Create(request.Email);

            var user = await userRepository.GetByEmailAsync(email, cancellationToken);
            if (user is null)
                return Result<UserResponse, AppException>.Fail(new NotFoundException($"User with email '{request.Email}' not found"));

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
