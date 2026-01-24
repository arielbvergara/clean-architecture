using Application.Dtos.User;
using Application.UseCases.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers;

/// <summary>
/// Exposes user management and self-service endpoints.
///
/// Includes `/api/User/me` operations that act on the current authenticated user,
/// as well as id- and email-based endpoints that are protected by ownership and
/// admin authorization policies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController(
    CreateUserUseCase createUserUseCase,
    GetUserByIdUseCase getUserByIdUseCase,
    GetUserByEmailUseCase getUserByEmailUseCase,
    GetUserByExternalAuthIdUseCase getUserByExternalAuthIdUseCase,
    UpdateUserNameUseCase updateUserNameUseCase,
    DeleteUserUseCase deleteUserUseCase,
    GetUsersUseCase getUsersUseCase,
    IAuthorizationService authorizationService,
    ILogger<UserController> logger)
    : ControllerBase
{
    // User creation is intentionally anonymous to allow initial provisioning of a user record
    // for a newly authenticated identity. Ownership and further operations still require auth.
    /// <summary>
    /// Gets a paginated list of users. Restricted to administrators.
    /// </summary>
    /// <remarks>
    /// Supports searching across email, name, and id, as well as ordering by email, name,
    /// and creation timestamp in both ascending and descending directions.
    /// </remarks>
    [HttpGet]
    [Authorize(Policy = AuthorizationPoliciesConstants.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] UserSortField? orderBy,
        [FromQuery] SortDirection? sortDirection,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isDeleted = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GetUsersRequest(
            search,
            orderBy ?? UserSortField.CreatedAt,
            sortDirection ?? SortDirection.Descending,
            pageNumber,
            pageSize,
            isDeleted);

        var result = await getUsersUseCase.ExecuteAsync(request, cancellationToken);

        return result.Match(
            onSuccess: Ok,
            onFailure: error => error switch
            {
                Application.Exceptions.ValidationException =>
                    BadRequest(new { error.Message }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
            });
    }

    /// <summary>
    /// Creates a new user record for the given external identity.
    /// </summary>
    /// <remarks>
    /// This endpoint is typically called once after a user has authenticated with the external
    /// identity provider (for example, Firebase). The <c>externalAuthId</c> should match the
    /// subject/UID from the access token used by that provider.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createUserUseCase.ExecuteAsync(request, cancellationToken);

        return result.Match(
            onSuccess: user => CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user),
            onFailure: error =>
            {
                logger.LogError(error.InnerException, "Failed to create user: {Message}", error.Message);
                return error switch
                {
                    Application.Exceptions.ConflictException => Conflict(new { error.Message }),
                    Application.Exceptions.ValidationException => BadRequest(new { error.Message }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
                };
            });
    }

    /// <summary>
    /// Gets the profile of the current authenticated user.
    /// </summary>
    /// <remarks>
    /// The user is resolved from the external authentication identifier (for example, the
    /// Firebase UID in the JWT <c>sub</c> claim). Clients do not need to provide an id or email.
    /// </remarks>
    [HttpGet("me")]
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

        var result = await updateUserNameUseCase.ExecuteAsync(
            new UpdateUserNameRequest(currentUser!.Id, dto.NewName),
            cancellationToken);

        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error =>
            {
                logger.LogError(error.InnerException, "Failed to update current user name: {Message}",
                    error.Message);
                return error switch
                {
                    Application.Exceptions.NotFoundException => NotFound(new { error.Message }),
                    Application.Exceptions.ValidationException => BadRequest(new { error.Message }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
                };
            });
    }

    /// <summary>
    /// Deletes the current authenticated user.
    /// </summary>
    /// <remarks>
    /// The user to delete is determined from the caller's access token. This endpoint is
    /// intended for self-service account removal scenarios.
    /// </remarks>
    [HttpDelete("me")]
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

        var result = await deleteUserUseCase.ExecuteAsync(new DeleteUserRequest(currentUser!.Id), cancellationToken);

        if (result.IsSuccess)
            return NoContent();

        var error = result.Error!;
        logger.LogError(error.InnerException, "Failed to delete current user: {Message}", error.Message);
        return error switch
        {
            Application.Exceptions.NotFoundException => NotFound(new { error.Message }),
            Application.Exceptions.ConflictException => Conflict(new { error.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
        };
    }

    /// <summary>
    /// Gets a user by internal identifier.
    /// </summary>
    /// <remarks>
    /// Normal users can only access their own user id. Administrators can access
    /// any user. Access is enforced via authorization and ownership checks.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(User, id, AuthorizationPoliciesConstants.OwnsUser);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        var result = await getUserByIdUseCase.ExecuteAsync(new GetUserByIdRequest(id), cancellationToken);

        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error =>
            {
                logger.LogError(error.InnerException, "Failed to get user: {Message}", error.Message);
                return error switch
                {
                    Application.Exceptions.NotFoundException => NotFound(new { error.Message }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
                };
            });
    }

    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    /// <remarks>
    /// After resolving the user by email, the same ownership and admin rules
    /// as the id-based endpoint are applied. Normal users can only retrieve
    /// their own record; administrators can retrieve any user.
    /// </remarks>
    [HttpGet("email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
        var result = await getUserByEmailUseCase.ExecuteAsync(new GetUserByEmailRequest(email), cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to get user by email: {Message}", error.Message);
            return error switch
            {
                Application.Exceptions.NotFoundException => NotFound(new { error.Message }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
            };
        }

        var user = result.Value!;

        var authorizationResult = await authorizationService.AuthorizeAsync(User, user.Id, AuthorizationPoliciesConstants.OwnsUser);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        return Ok(user);
    }

    /// <summary>
    /// Updates the display name of a user identified by id.
    /// </summary>
    /// <remarks>
    /// Normal users can only update their own name. Administrators can update
    /// the name of any user, subject to authorization policies.
    /// </remarks>
    [HttpPut("{id:guid}/name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserName(Guid id, [FromBody] UpdateUserNameDto dto,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(User, id, AuthorizationPoliciesConstants.OwnsUser);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        var result =
            await updateUserNameUseCase.ExecuteAsync(new UpdateUserNameRequest(id, dto.NewName), cancellationToken);

        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error =>
            {
                logger.LogError(error.InnerException, "Failed to update user name: {Message}", error.Message);
                return error switch
                {
                    Application.Exceptions.NotFoundException => NotFound(new { error.Message }),
                    Application.Exceptions.ValidationException => BadRequest(new { error.Message }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
                };
            });
    }

    /// <summary>
    /// Deletes a user identified by id.
    /// </summary>
    /// <remarks>
    /// Normal users can only delete their own account. Administrators can delete
    /// any user, subject to authorization policies.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(User, id, AuthorizationPoliciesConstants.OwnsUser);
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        var result = await deleteUserUseCase.ExecuteAsync(new DeleteUserRequest(id), cancellationToken);

        if (result.IsSuccess)
            return NoContent();

        var error = result.Error!;
        logger.LogError(error.InnerException, "Failed to delete user: {Message}", error.Message);
        return error switch
        {
            Application.Exceptions.NotFoundException => NotFound(new { error.Message }),
            Application.Exceptions.ConflictException => Conflict(new { error.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
        };
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
