namespace DogWalkingApp.Api.Dtos;

public record RegisterRequest(string Email, string Password, string FullName, string Role);
public record LoginRequest(string Email, string Password);
public record AuthResponse(long UserId, string Email, string FullName, string Role, string Token);