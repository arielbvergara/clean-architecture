using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs;

public record CreateAdminUserDto(
    [Required] [EmailAddress] string Email,
    [Required] string DisplayName,
    [Required] [MinLength(12)] string Password);
