namespace ListingsApi.Contracts;

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record ChangePasswordRequest(string OldPassword, string NewPassword);
public record AuthUserResponse(int Id, string Email, string Role);
public record AuthResponse(
	string Token,
	DateTime ExpiresAt,
	string RefreshToken,
	DateTime RefreshExpiresAt,
	AuthUserResponse User);
public record RefreshRequest(string RefreshToken);