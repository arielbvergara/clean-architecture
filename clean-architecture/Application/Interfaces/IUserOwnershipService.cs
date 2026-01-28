using Application.Dtos.User;
using Application.Exceptions;

namespace Application.Interfaces;

public interface IUserOwnershipService
{
    /// <summary>
    /// Ensures that the current caller either owns the specified user resource or has
    /// administrative privileges. Returns <c>null</c> when access is allowed; otherwise
    /// returns an <see cref="AppException"/> (typically a <see cref="NotFoundException"/>)
    /// to support anti-enumeration semantics.
    /// </summary>
    Task<AppException?> EnsureOwnerOrAdminAsync(
        Domain.Entities.User targetUser,
        CurrentUserContext? currentUser,
        Guid requestUserId,
        CancellationToken cancellationToken);
}
