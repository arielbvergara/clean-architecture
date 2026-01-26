using WebAPI.Authorization;

namespace WebAPI.DTOs;

/// <summary>
/// Request payload for creating a new user via the WebAPI.
///
/// The external authentication identifier is *not* supplied by the client; it is derived from
/// the authenticated principal's token claims (see <see cref="ClaimsPrincipalExtensions.GetExternalAuthId"/>).
/// </summary>
public sealed record CreateUserDto(string Email, string Name);
