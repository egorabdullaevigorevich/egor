using ListingsApi.Endpoints;
using ListingsApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();                      
builder.Services.AddOpenApi();                   // Включаем поддержку OpenAPI
builder.Services.AddSingleton<ListingStore>();

var app = builder.Build();

app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.MapOpenApi();          // документ: /openapi/v1.json

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Listings API v1");
    options.RoutePrefix = "swagger";      
});

app.MapListingEndpoints(); // Перенесли вызов эндпоинтов в самый конец перед запуском

app.Run();
