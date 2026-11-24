using System.ComponentModel.DataAnnotations;

namespace DogWalkingApp.Api.Dtos;

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(10), StringLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Role { get; set; } = "owner";
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

public sealed record AuthUserDto(long Id, Guid PublicId, string Name, string Email, string Role);
public sealed record AuthResponse(string AccessToken, AuthUserDto User);
