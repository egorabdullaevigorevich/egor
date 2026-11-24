using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ListingsApi.Contracts;
using ListingsApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ListingsApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async Task<Results<Created<AuthResponse>, BadRequest<string>, Conflict<string>>>
            (RegisterRequest request, AuthService service) =>
        {
            var email = request.Email?.Trim() ?? "";
            if (email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
                return TypedResults.BadRequest("Введите корректный email.");
            if (request.Password is null || request.Password.Length is < 8 or > 100)
                return TypedResults.BadRequest("Пароль должен содержать от 8 до 100 символов.");
            if (await service.EmailExistsAsync(email))
                return TypedResults.Conflict("Пользователь с таким email уже существует.");

            var response = await service.RegisterAsync(request);
            return TypedResults.Created("/api/auth/me", response);
        })
        .WithName("Register");

        group.MapPost("/login", async Task<Results<Ok<AuthResponse>, BadRequest<string>, UnauthorizedHttpResult>>
            (LoginRequest request, AuthService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
                return TypedResults.BadRequest("Введите email и пароль.");

            var response = await service.LoginAsync(request);
            return response is null
                ? TypedResults.Unauthorized()
                : TypedResults.Ok(response);
        })
        .RequireRateLimiting("login")
        .WithName("Login");

        group.MapPost("/refresh", async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>>
            (RefreshRequest request, AuthService service) =>
        {
            var response = string.IsNullOrWhiteSpace(request.RefreshToken)
                ? null
                : await service.RefreshAsync(request.RefreshToken);
            return response is null ? TypedResults.Unauthorized() : TypedResults.Ok(response);
        })
        .WithName("RefreshToken");

        group.MapPost("/change-password", async Task<Results<Ok<AuthResponse>, BadRequest<string>, UnauthorizedHttpResult>>
            (ChangePasswordRequest request, ClaimsPrincipal principal, AuthService service) =>
        {
            if (request.NewPassword is null || request.NewPassword.Length is < 8 or > 100)
                return TypedResults.BadRequest("Новый пароль должен содержать от 8 до 100 символов.");

            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await service.ChangePasswordAsync(userId, request);
            return response is null
                ? TypedResults.Unauthorized()
                : TypedResults.Ok(response);
        })
        .RequireAuthorization()
        .WithName("ChangePassword");

        group.MapPost("/logout", async (RefreshRequest request, AuthService service) =>
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
                await service.RevokeRefreshTokenAsync(request.RefreshToken);
            return TypedResults.NoContent();
        })
        .WithName("Logout");

        group.MapGet("/me", (ClaimsPrincipal principal) =>
        {
            var id = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var email = principal.FindFirstValue(ClaimTypes.Email)!;
            var role = principal.FindFirstValue("role") ?? "User";
            return TypedResults.Ok(new AuthUserResponse(id, email, role));
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser");
    }
}