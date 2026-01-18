using TodoApp.Application.Dtos.User;
using TodoApp.Application.Exceptions;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Primitives;
using TodoApp.Domain.ValueObject;

namespace TodoApp.Application.UseCases.User;

public class DeleteUserUseCase(IUserRepository userRepository)
{
    public async Task<Result<bool, AppException>> ExecuteAsync(DeleteUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserId.Create(request.UserId);

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<bool, AppException>.Fail(new NotFoundException("User", request.UserId));

            await userRepository.DeleteAsync(userId, cancellationToken);

            return Result<bool, AppException>.Ok(true);
        }
        catch (AppException ex)
        {
            return Result<bool, AppException>.Fail(ex);
        }
        catch (ArgumentException ex)
        {
            return Result<bool, AppException>.Fail(new ValidationException(ex.Message));
        }
        catch (Exception ex)
        {
            return Result<bool, AppException>.Fail(new InfraException("An unexpected error occurred", ex));
        }
    }
}