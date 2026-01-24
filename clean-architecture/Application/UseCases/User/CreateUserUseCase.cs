using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.User;

public class CreateUserUseCase(
    IUserRepository userRepository)
{
    public async Task<Result<UserResponse, AppException>> ExecuteAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = Email.Create(request.Email);
            var name = UserName.Create(request.Name);
            var externalAuthId = ExternalAuthIdentifier.Create(request.ExternalAuthId);

            var existingUserByEmail = await userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUserByEmail is not null)
                return Result<UserResponse, AppException>.Fail(
                    new ConflictException($"User with email '{request.Email}' already exists"));

            var existingUserByExternalAuthId = await userRepository.GetByExternalAuthIdAsync(externalAuthId, cancellationToken);
            if (existingUserByExternalAuthId is not null)
                return Result<UserResponse, AppException>.Fail(
                    new ConflictException($"User with external auth ID already exists"));

            var user = Domain.Entities.User.Create(email, name, externalAuthId);

            await userRepository.AddAsync(user, cancellationToken);

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
