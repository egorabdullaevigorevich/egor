using ListingsApi.Data;
using ListingsApi.Endpoints;
using ListingsApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
        };
    });
builder.Services.AddAuthorization();
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