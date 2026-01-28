using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Constants;
using Domain.ValueObject;

namespace Application.Services;

internal sealed class UserOwnershipService(IUserRepository userRepository) : IUserOwnershipService
{
    public async Task<AppException?> EnsureOwnerOrAdminAsync(
        Domain.Entities.User targetUser,
        CurrentUserContext? currentUser,
        Guid requestUserId,
        CancellationToken cancellationToken)
    {
        // If there is no current user context, or the caller is an admin, allow by default.
        if (currentUser is null ||
            string.Equals(currentUser.Role, UserRoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var currentUserExternalAuthId = ExternalAuthIdentifier.Create(currentUser.UserId);
        var currentUserEntity = await userRepository.GetByExternalAuthIdAsync(
            currentUserExternalAuthId,
            cancellationToken);

        if (currentUserEntity is null || currentUserEntity.Id != targetUser.Id)
        {
            // Anti-enumeration: behave as if the target user does not exist when
            // the caller is not the owner and not an administrator.
            return new NotFoundException("User", requestUserId);
        }

        return null;
    }
}
