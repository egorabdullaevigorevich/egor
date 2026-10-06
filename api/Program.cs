using ListingsApi.Data;
using ListingsApi.Endpoints;
using ListingsApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();                      
builder.Services.AddOpenApi();                   

// Подключаем базы данных SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Регистрация сервисов (на один запрос)
builder.Services.AddScoped<ListingService>();
builder.Services.AddScoped<DistrictService>();

var app = builder.Build();
app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.MapOpenApi();          

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("http://localhost:5154/openapi/v1.json", "Listings API v1");
    options.RoutePrefix = "swagger";      
});

// группы эндпоинтов
app.MapListingEndpoints(); 
app.MapDistrictEndpoints();

app.Run();