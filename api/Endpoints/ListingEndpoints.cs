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

        group.MapGet("/count", (ListingStore store) =>
        {
            // Берем реальное количество объявлений из вашего хранилища в памяти
            var totalCount = store.GetAll().Count(); 
            return TypedResults.Ok(new { count = totalCount });
        })
        .WithName("GetListingsCount")
        .WithSummary("Общее количество объявлений");
 
        // GET /api/listings/7
        group.MapGet("/{id:int}", Results<Ok<ListingResponse>, NotFound> (int id, ListingStore store) =>
            store.Get(id) is { } listing
                ? TypedResults.Ok(ToResponse(listing))
                : TypedResults.NotFound())
        .WithName("GetListing")
        .WithSummary("Одно объявление по id");
    }
 
    private static ListingResponse ToResponse(Listing l) =>
        new(l.Id, l.Title, l.Price, l.District, l.Rooms, l.CreatedAt);
}