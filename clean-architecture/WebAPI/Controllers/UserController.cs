using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Dtos.User;
using Application.UseCases.User;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController(
    CreateUserUseCase createUserUseCase,
    GetUserByIdUseCase getUserByIdUseCase,
    GetUserByEmailUseCase getUserByEmailUseCase,
    UpdateUserNameUseCase updateUserNameUseCase,
    DeleteUserUseCase deleteUserUseCase,
    IAuthorizationService authorizationService,
    ILogger<UserController> logger)
    : ControllerBase
{
    // User creation is intentionally anonymous to allow initial provisioning of a user record
    // for a newly authenticated identity. Ownership and further operations still require auth.
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(User, id, "OwnsUser");
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

        var authorizationResult = await authorizationService.AuthorizeAsync(User, user.Id, "OwnsUser");
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        return Ok(user);
    }

    [HttpPut("{id:guid}/name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserName(Guid id, [FromBody] UpdateUserNameDto dto,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(User, id, "OwnsUser");
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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(User, id, "OwnsUser");
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

    private async Task<(UserResponse? currentUser, IActionResult? errorResult)> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var externalAuthId = GetExternalAuthIdFromClaims();
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

    private string? GetExternalAuthIdFromClaims()
    {
        // Prefer OpenID Connect 'sub' claim, fall back to NameIdentifier if present.
        return User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
