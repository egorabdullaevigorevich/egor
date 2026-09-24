using ListingsApi.Endpoints;
using ListingsApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();                      // разрешение для фронтенда
builder.Services.AddSingleton<ListingStore>();

var app = builder.Build();
app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.MapListingEndpoints(); // ← Этот метод сам подключит все нужные эндпоинты из ListingEndpoints.cs

app.Run();
