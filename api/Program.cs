var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();                      // разрешение для фронтенда

var app = builder.Build();
app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

// временные данные — на неделе 3 заменим базой
var listings = new[]
{
    new { id = 1, title = "2-комнатная, Юнусабад",  price = 62000, area = 54, rooms = 2 },
    new { id = 2, title = "3-комнатная, Чиланзар-9", price = 41000, area = 30, rooms = 3 },
    new { id = 3, title = "3-комнатная, Мирзo-Улугбек", price = 95000, area = 78, rooms = 3 },
    new { id = 4, title = "3-комнатная, Чиланзар-8", price = 78600, area = 65, rooms = 3 },
    new { id = 5, title = "2-комнатная, Чиланзар-9", price = 45000, area = 41, rooms = 2 },
};

app.MapGet("/api/listings", () => listings);
app.MapGet("/api/listings/{id:int}", (int id) =>
    listings.FirstOrDefault(l => l.id == id) is {} found
        ? Results.Ok(found)
        : Results.NotFound());
app.MapGet("/api/listings/count", () =>{
    return Results.Ok( new { count = 5 });
});
app.Run();