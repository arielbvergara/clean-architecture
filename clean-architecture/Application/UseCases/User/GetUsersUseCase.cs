using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;

namespace Application.UseCases.User;

public class GetUsersUseCase(IUserRepository userRepository)
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<Result<PagedUsersResponse, AppException>> ExecuteAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (pageNumber, pageSize) = NormalizePagination(request.PageNumber, request.PageSize);

            var criteria = new UserQueryCriteria(
                request.Search,
                request.OrderBy,
                request.SortDirection,
                pageNumber,
                pageSize,
                request.IsDeletedFilter);

            var (items, totalCount) = await userRepository.GetPagedAsync(criteria, cancellationToken);

            var userResponses = items
                .Select(user => user.ToUserResponse())
                .ToArray();

            var totalPages = CalculateTotalPages(totalCount, pageSize);

            var pagination = new PaginationMetadata(
                totalCount,
                pageNumber,
                pageSize,
                totalPages);

            var response = new PagedUsersResponse(userResponses, pagination);

            return Result<PagedUsersResponse, AppException>.Ok(response);
        }
        catch (AppException ex)
        {
            return Result<PagedUsersResponse, AppException>.Fail(ex);
        }
        catch (ArgumentException ex)
        {
            return Result<PagedUsersResponse, AppException>.Fail(new ValidationException(ex.Message));
        }
        catch (Exception ex)
        {
            return Result<PagedUsersResponse, AppException>.Fail(
                new InfraException("An unexpected error occurred while retrieving users.", ex));
        }
    }

    private static (int PageNumber, int PageSize) NormalizePagination(int requestedPageNumber, int requestedPageSize)
    {
        var pageNumber = requestedPageNumber < DefaultPageNumber
            ? DefaultPageNumber
            : requestedPageNumber;

        var pageSize = requestedPageSize <= 0
            ? DefaultPageSize
            : Math.Min(requestedPageSize, MaximumPageSize);

        return (pageNumber, pageSize);
    }

    private static int CalculateTotalPages(int totalItems, int pageSize)
    {
        if (totalItems == 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(totalItems / (double)pageSize);
    }
}
