using ListingsApi.Endpoints;
using ListingsApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();                      // разрешение для фронтенда
builder.Services.AddOpenApi();                   // ← ДОБАВЬТЕ ЭТУ СТРОКУ (Служба генерации документации)
builder.Services.AddSingleton<ListingStore>();

var app = builder.Build();
app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.MapListingEndpoints(); 

app.MapOpenApi();          // документ: /openapi/v1.json

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Listings API v1");
    options.RoutePrefix = "swagger";      // страница будет открываться по адресу /swagger
});

app.Run();
