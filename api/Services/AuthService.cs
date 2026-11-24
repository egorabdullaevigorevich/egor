using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ListingsApi.Contracts;
using ListingsApi.Data;
using ListingsApi.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ListingsApi.Services;

public class AuthService(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration)
{
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "ListingsApi";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "ListingsWeb";
    private readonly byte[] _signingKey = GetSigningKey(configuration);
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<bool> EmailExistsAsync(string email) =>
        await db.Users.AnyAsync(user => user.Email == NormalizeEmail(email));

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            Email = NormalizeEmail(request.Email),
            Role = "User",
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return await CreateResponseAsync(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate =>
            candidate.Email == NormalizeEmail(request.Email));
        if (user is null) return null;

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (string.IsNullOrWhiteSpace(user.Role)) user.Role = "User";
        return await CreateResponseAsync(user);
    }

    public async Task<AuthResponse?> RefreshAsync(string rawToken)
    {
        var now = DateTime.UtcNow;
        var tokenHash = HashRefreshToken(rawToken);
        var storedToken = await db.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash);
        if (storedToken is null || storedToken.RevokedAt is not null || storedToken.ExpiresAt <= now)
            return null;

        await using var transaction = await db.Database.BeginTransactionAsync();
        storedToken.RevokedAt = now;
        var response = await CreateResponseAsync(storedToken.User);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return response;
    }

    public async Task<AuthResponse?> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId);
        if (user is null) return null;

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword);
        if (verification == PasswordVerificationResult.Failed) return null;

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        var now = DateTime.UtcNow;
        var activeTokens = await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now)
            .ToListAsync();
        foreach (var token in activeTokens) token.RevokedAt = now;

        return await CreateResponseAsync(user);
    }

    public async Task RevokeRefreshTokenAsync(string rawToken)
    {
        var tokenHash = HashRefreshToken(rawToken);
        var storedToken = await db.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash);
        if (storedToken is null || storedToken.RevokedAt is not null) return;
        storedToken.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task<AuthResponse> CreateResponseAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Role)) user.Role = "User";
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(AccessTokenLifetime);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim("role", user.Role),
        };
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _issuer,
            Audience = _audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_signingKey), SecurityAlgorithms.HmacSha256),
        };
        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshExpiresAt = now.Add(RefreshTokenLifetime);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashRefreshToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshExpiresAt,
        });
        await db.SaveChangesAsync();

        return new AuthResponse(token, expiresAt, refreshToken, refreshExpiresAt,
            new AuthUserResponse(user.Id, user.Email, user.Role));
    }

    private static string HashRefreshToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static byte[] GetSigningKey(IConfiguration configuration)
    {
        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
        return Encoding.UTF8.GetBytes(key);
    }
}