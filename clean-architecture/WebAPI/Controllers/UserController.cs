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

}
