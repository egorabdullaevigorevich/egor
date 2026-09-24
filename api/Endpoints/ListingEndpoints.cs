using ListingsApi.Contracts;
using ListingsApi.Models;
using ListingsApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
 
namespace ListingsApi.Endpoints;
 
public static class ListingEndpoints
{
    public static void MapListingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/listings").WithTags("Listings");
 
        // GET /api/listings?district=Чиланзар&maxPrice=50000
        group.MapGet("/", (ListingStore store, string? district, decimal? maxPrice) =>
        {
            var items = store.GetAll().AsEnumerable();
            if (district is not null) items = items.Where(l => l.District == district);
            if (maxPrice is not null) items = items.Where(l => l.Price <= maxPrice);
            return TypedResults.Ok(items.Select(ToResponse));
        })
        .WithName("GetListings")
        .WithSummary("Список объявлений с фильтром по району и максимальной цене");

        // GET /api/listings/count
        group.MapGet("/count", (ListingStore store) =>
        {
            var totalCount = store.GetAll().Count(); 
            return TypedResults.Ok(new { count = totalCount });
        })
        .WithName("GetListingsCount")
        .WithSummary("Общее количество объявлений");

        // POST /api/listings
        group.MapPost("/", Results<Created<ListingResponse>, BadRequest<string>>
            (CreateListingRequest request, ListingStore store) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return TypedResults.BadRequest("Поле title обязательно");
            if (request.Price <= 0)
                return TypedResults.BadRequest("Поле price должно быть больше 0");
            if (request.Rooms is < 1 or > 10)
                return TypedResults.BadRequest("Поле rooms — от 1 до 10");

            var created = store.Add(request);
            return TypedResults.Created($"/api/listings/{created.Id}", ToResponse(created));
        })
        .WithName("CreateListing")
        .WithSummary("Создать объявление");
 
        // GET /api/listings/7
        group.MapGet("/{id:int}", Results<Ok<ListingResponse>, NotFound> (int id, ListingStore store) =>
            store.Get(id) is { } listing
                ? TypedResults.Ok(ToResponse(listing))
                : TypedResults.NotFound())
        .WithName("GetListing")
        .WithSummary("Одно объявление по id");

        // ДОБАВИЛИ PUT
        // PUT — полная замена
        group.MapPut("/{id:int}", Results<NoContent, NotFound, BadRequest<string>>
            (int id, UpdateListingRequest request, ListingStore store) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.Price <= 0)
                return TypedResults.BadRequest("title обязателен, price > 0");
            return store.Update(id, request)
                ? TypedResults.NoContent()
                : TypedResults.NotFound();
        })
        .WithName("UpdateListing")
        .WithSummary("Заменить объявление целиком");
 
        // ДОБАВИЛИ DELETE
        // DELETE
        group.MapDelete("/{id:int}", Results<NoContent, NotFound> (int id, ListingStore store) =>
            store.Delete(id) ? TypedResults.NoContent() : TypedResults.NotFound())
        .WithName("DeleteListing")
        .WithSummary("Удалить объявление");
    }
 
    private static ListingResponse ToResponse(Listing l) =>
        new(l.Id, l.Title, l.Price, l.District, l.Address, l.Rooms, l.CreatedAt); // ← Добавили l.Address пятым параметром

}
