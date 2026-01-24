namespace Application.Dtos.User;

public sealed record PagedUsersResponse(
    IReadOnlyCollection<UserResponse> Items,
    PaginationMetadata Pagination
);
