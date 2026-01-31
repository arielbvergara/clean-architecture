using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.User;

public class CreateAdminUserUseCase(
    IUserRepository userRepository,
    IIdentityProviderService identityProviderService)
{
    public async Task<Result<UserResponse, AppException>> ExecuteAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = Email.Create(request.Email);

            // Check if user already exists
            var existingUser = await userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUser is not null)
            {
                // If user exists, ensure they have admin claims in identity provider
                await identityProviderService.EnsureAdminUserAsync(
                    request.Email,
                    request.Password,
                    request.Name,
                    cancellationToken);

                return Result<UserResponse, AppException>.Ok(existingUser.ToUserResponse());
            }

            // Create user in identity provider
            var externalId = await identityProviderService.EnsureAdminUserAsync(
                request.Email,
                request.Password,
                request.Name,
                cancellationToken);

            var name = UserName.Create(request.Name);
            var externalAuthId = ExternalAuthIdentifier.Create(externalId);

            // Create admin user in domain
            var adminUser = Domain.Entities.User.CreateAdmin(email, name, externalAuthId);

            await userRepository.AddAsync(adminUser, cancellationToken);

            return Result<UserResponse, AppException>.Ok(adminUser.ToUserResponse());
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
            return Result<UserResponse, AppException>.Fail(new InfraException("An unexpected error occurred while creating admin user", ex));
        }
    }
}
