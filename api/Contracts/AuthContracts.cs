namespace ListingsApi.Contracts;

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthUserResponse(int Id, string Email);
public record AuthResponse(string Token, DateTime ExpiresAt, AuthUserResponse User);