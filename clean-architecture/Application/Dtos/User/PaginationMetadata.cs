namespace Application.Dtos.User;

public sealed record PaginationMetadata(
    int TotalItems,
    int PageNumber,
    int PageSize,
    int TotalPages
);
