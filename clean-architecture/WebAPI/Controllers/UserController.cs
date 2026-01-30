using Application.Dtos.User;
using Application.Interfaces;
using Application.UseCases.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Authorization;
using WebAPI.DTOs;
using WebAPI.ErrorHandling;
using WebAPI.RateLimiting;

namespace WebAPI.Controllers;

/// <summary>
/// Exposes self-service user endpoints.
///
/// Includes user creation and `/api/User/me` operations that act on the current authenticated user.
/// Administrative user management operations are available in <see cref="AdminUserController"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController(
    CreateUserUseCase createUserUseCase,
    GetUserByExternalAuthIdUseCase getUserByExternalAuthIdUseCase,
    UpdateUserNameUseCase updateUserNameUseCase,
    DeleteUserUseCase deleteUserUseCase,
    ILogger<UserController> logger,
    ISecurityEventNotifier securityEventNotifier)
    : ControllerBase
{
    /// <summary>
    /// Creates a new user record for the current authenticated identity.
    /// </summary>
    /// <remarks>
    /// This endpoint is typically called once after a user has authenticated with the external
    /// identity provider (for example, Firebase). The external authentication identifier is
    /// derived exclusively from the caller's access token (for example, the JWT <c>sub</c>
    /// claim) and is not accepted from the request body.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting(RateLimitingPolicies.Strict)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request,
        CancellationToken cancellationToken)
    {
        var externalAuthId = User.GetExternalAuthId();
        if (string.IsNullOrWhiteSpace(externalAuthId))
        {
            logger.LogWarning("Authenticated principal is missing external auth identifier claim when creating user.");
            return Forbid();
        }

        var appRequest = new CreateUserRequest(request.Email, request.Name, externalAuthId);

        var result = await createUserUseCase.ExecuteAsync(appRequest, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to create user: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.UserCreateFailed,
                null,
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    { "Route", HttpContext.Request.Path },
                    { "ExceptionType", error.Type.ToString() }
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var user = result.Value!;

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.UserCreated,
            user.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                { "Route", HttpContext.Request.Path }
            },
            cancellationToken);

        return CreatedAtAction(nameof(AdminUserController.GetUserById), "AdminUser", new { id = user.Id }, user);
    }

    /// <summary>
    /// Gets the profile of the current authenticated user.
    /// </summary>
    /// <remarks>
    /// The user is resolved from the external authentication identifier (for example, the
    /// Firebase UID in the JWT <c>sub</c> claim). Clients do not need to provide an id or email.
    /// </remarks>
    [HttpGet("me")]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var (currentUser, errorResult) = await GetCurrentUserAsync(cancellationToken);
        if (errorResult is not null)
        {
            return errorResult;
        }

        return Ok(currentUser);
    }

    /// <summary>
    /// Updates the display name of the current authenticated user.
    /// </summary>
    /// <remarks>
    /// The target user is derived from the access token. This endpoint cannot be used to
    /// update another user's name; administrators should use the id-based endpoints instead.
    /// </remarks>
    [HttpPut("me/name")]
    [EnableRateLimiting(RateLimitingPolicies.Strict)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyName([FromBody] UpdateUserNameDto dto, CancellationToken cancellationToken)
    {
        var (currentUser, errorResult) = await GetCurrentUserAsync(cancellationToken);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var currentUserContext = User.ToCurrentUserContext();

        var result = await updateUserNameUseCase.ExecuteAsync(
            new UpdateUserNameRequest(currentUser!.Id, dto.NewName, currentUserContext),
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to update current user name: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.UserUpdateFailed,
                currentUser.Id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    { "Route", HttpContext.Request.Path },
                    { "ExceptionType", error.Type.ToString() }
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var updatedUser = result.Value!;

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.UserUpdated,
            updatedUser.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                { "Route", HttpContext.Request.Path }
            },
            cancellationToken);

        return Ok(updatedUser);
    }

    /// <summary>
    /// Deletes the current authenticated user.
    /// </summary>
    /// <remarks>
    /// The user to delete is determined from the caller's access token. This endpoint is
    /// intended for self-service account removal scenarios.
    /// </remarks>
    [HttpDelete("me")]
    [EnableRateLimiting(RateLimitingPolicies.Strict)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        var (currentUser, errorResult) = await GetCurrentUserAsync(cancellationToken);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var currentUserContext = User.ToCurrentUserContext();

        var result = await deleteUserUseCase.ExecuteAsync(
            new DeleteUserRequest(currentUser!.Id, currentUserContext),
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to delete current user: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.UserDeleteFailed,
                currentUser.Id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    { "Route", HttpContext.Request.Path },
                    { "ExceptionType", error.Type.ToString() }
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.UserDeleted,
            currentUser.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                { "Route", HttpContext.Request.Path }
            },
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Resolves the current authenticated user from the external auth identifier.
    /// </summary>
    /// <remarks>
    /// This helper intentionally lives in <see cref="UserController"/> because its behavior is specific
    /// to user-centric endpoints (e.g. <c>/me</c>) and their error semantics (404 vs 403).
    /// If other controllers need similar behavior in the future, we can promote this to a shared
    /// abstraction (e.g. base controller or ICurrentUser service) once the common requirements are clear.
    /// </remarks>
    private async Task<(UserResponse? currentUser, IActionResult? errorResult)> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var externalAuthId = User.GetExternalAuthId();
        if (externalAuthId is null)
        {
            logger.LogWarning("Authenticated principal is missing external auth identifier claim.");
            return (null, Forbid());
        }

        var currentUserResult = await getUserByExternalAuthIdUseCase.ExecuteAsync(
            new GetUserByExternalAuthIdRequest(externalAuthId),
            cancellationToken);

        if (currentUserResult.IsFailure)
        {
            var error = currentUserResult.Error!;
            logger.LogError(error.InnerException, "Failed to resolve current user from external auth ID: {Message}",
                error.Message);

            IActionResult actionResult = error switch
            {
                Application.Exceptions.NotFoundException => NotFound(new { error.Message }),
                _ => Forbid()
            };

            return (null, actionResult);
        }

        return (currentUserResult.Value!, null);
    }
}
