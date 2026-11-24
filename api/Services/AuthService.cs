using System.Security.Claims;
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

    public async Task<bool> EmailExistsAsync(string email) =>
        await db.Users.AnyAsync(user => user.Email == NormalizeEmail(email));

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            Email = NormalizeEmail(request.Email),
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return CreateResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate =>
            candidate.Email == NormalizeEmail(request.Email));
        if (user is null) return null;

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        return result == PasswordVerificationResult.Failed ? null : CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Email),
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
        return new AuthResponse(token, expiresAt, new AuthUserResponse(user.Id, user.Email));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static byte[] GetSigningKey(IConfiguration configuration)
    {
        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
        return Encoding.UTF8.GetBytes(key);
    }
}