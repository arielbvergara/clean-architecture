namespace Application.Dtos.User;

public sealed record UserQueryCriteria(
    string? SearchTerm,
    UserSortField SortField,
    SortDirection SortDirection,
    int PageNumber,
    int PageSize,
    bool? IsDeletedFilter
);
