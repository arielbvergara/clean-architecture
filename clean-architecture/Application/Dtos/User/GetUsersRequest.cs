namespace Application.Dtos.User;

public sealed record GetUsersRequest(
    string? Search,
    UserSortField OrderBy,
    SortDirection SortDirection,
    int PageNumber,
    int PageSize,
    bool? IsDeletedFilter
);
