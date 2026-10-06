using ListingsApi.Contracts;
using ListingsApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
 
namespace ListingsApi.Endpoints;
 
public static class ListingEndpoints
{
    public static void MapListingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/listings").WithTags("Listings");
 
        group.MapGet("/", async (ListingService service, int? districtId, decimal? maxPrice) =>
            TypedResults.Ok(await service.GetAllAsync(districtId, maxPrice)))
        .WithName("GetListings")
        .WithSummary("Список объявлений с фильтром по району (id) и максимальной цене");
 
        group.MapGet("/{id:int}", async Task<Results<Ok<ListingResponse>, NotFound>>
            (int id, ListingService service) =>
            await service.GetAsync(id) is { } dto
                ? TypedResults.Ok(dto)
                : TypedResults.NotFound())
        .WithName("GetListing");
 
        group.MapPost("/", async Task<Results<Created<ListingResponse>, BadRequest<string>>>
            (CreateListingRequest request, ListingService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title)) return TypedResults.BadRequest("Поле title обязательно");
            if (request.Price <= 0) return TypedResults.BadRequest("Поле price должно быть больше 0");
            if (request.Rooms is < 1 or > 10) return TypedResults.BadRequest("Поле rooms — от 1 до 10");
 
            var created = await service.CreateAsync(request);
            if (created is null) return TypedResults.BadRequest("Район с таким districtId не найден");
 
            return TypedResults.Created($"/api/listings/{created.Id}", created);
        })
        .WithName("CreateListing");
 
        group.MapPut("/{id:int}", async Task<Results<NoContent, NotFound, BadRequest<string>>>
            (int id, UpdateListingRequest request, ListingService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.Price <= 0)
                return TypedResults.BadRequest("title обязателен, price > 0");
            return await service.UpdateAsync(id, request)
                ? TypedResults.NoContent()
                : TypedResults.NotFound();
        })
        .WithName("UpdateListing");
 
        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>>
            (int id, ListingService service) =>
            await service.DeleteAsync(id) ? TypedResults.NoContent() : TypedResults.NotFound())
        .WithName("DeleteListing");
    }
}
