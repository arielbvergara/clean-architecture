using Microsoft.AspNetCore.Mvc;
using Application.Dtos.User;
using Application.UseCases.User;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(
    CreateUserUseCase createUserUseCase,
    GetUserByIdUseCase getUserByIdUseCase,
    GetUserByEmailUseCase getUserByEmailUseCase,
    UpdateUserNameUseCase updateUserNameUseCase,
    DeleteUserUseCase deleteUserUseCase,
    ILogger<UserController> logger)
    : ControllerBase
{
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
        var result = await getUserByEmailUseCase.ExecuteAsync(new GetUserByEmailRequest(email), cancellationToken);

        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error =>
            {
                logger.LogError(error.InnerException, "Failed to get user by email: {Message}", error.Message);
                return error switch
                {
                    Application.Exceptions.NotFoundException => NotFound(new { error.Message }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
                };
            });
    }

    [HttpPut("{id:guid}/name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserName(Guid id, [FromBody] UpdateUserNameDto dto,
        CancellationToken cancellationToken)
    {
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
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