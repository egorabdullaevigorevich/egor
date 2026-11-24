using ListingsApi.Data;
using ListingsApi.Endpoints;
using ListingsApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();                      
builder.Services.AddOpenApi();                   

var jwtKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ListingsApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ListingsWeb";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
});
builder.Services.AddScoped<IPasswordHasher<ListingsApi.Data.Entities.User>, PasswordHasher<ListingsApi.Data.Entities.User>>();

// Подключаем базы данных SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Регистрация сервисов (на один запрос)
builder.Services.AddScoped<ListingService>();
builder.Services.AddScoped<DistrictService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();
app.UseCors(p => p.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
    .AllowAnyHeader().AllowAnyMethod());
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();          

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("http://localhost:5154/openapi/v1.json", "Listings API v1");
    options.RoutePrefix = "swagger";      
});

// группы эндпоинтов
app.MapListingEndpoints(); 
app.MapDistrictEndpoints();
app.MapAuthEndpoints();

app.Run();