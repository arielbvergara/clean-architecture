using System.Security.Claims;
using Application.Dtos.User;
using Application.UseCases.User;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authorization;

/// <summary>
/// Authorization handler that enforces that the current principal either owns the
/// target user resource (by internal user id) or is an administrator.
///
/// This handler relies on the external authentication identifier (e.g. Firebase UID)
/// provided by the identity provider to resolve the current user, then compares
/// its internal id to the target resource id.
/// </summary>
public sealed class OwnsUserHandler(
    GetUserByExternalAuthIdUseCase getUserByExternalAuthIdUseCase,
    ILogger<OwnsUserHandler> logger)
    : AuthorizationHandler<OwnsUserRequirement, Guid>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnsUserRequirement requirement,
        Guid targetUserId)
    {
        // If there is no authenticated identity, fail closed.
        if (context.User?.Identity is not { IsAuthenticated: true })
        {
            return;
        }

        // Administrators are allowed to operate on any user resource.
        if (context.User.IsAdmin())
        {
            context.Succeed(requirement);
            return;
        }

        var externalAuthId = context.User.GetExternalAuthId();
        if (string.IsNullOrWhiteSpace(externalAuthId))
        {
            logger.LogWarning("Authorization failed: missing external auth identifier claim for principal {Name}",
                context.User.Identity?.Name ?? "<unknown>");
            return;
        }

        var result = await getUserByExternalAuthIdUseCase.ExecuteAsync(
            new GetUserByExternalAuthIdRequest(externalAuthId),
            CancellationToken.None);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogWarning(error.InnerException,
                "Authorization failed: unable to resolve current user from external auth ID. Message: {Message}",
                error.Message);
            return;
        }

        var currentUser = result.Value!;
        if (currentUser.Id == targetUserId)
        {
            context.Succeed(requirement);
        }
    }
}
