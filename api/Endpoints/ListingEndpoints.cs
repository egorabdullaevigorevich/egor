using ListingsApi.Contracts;
using ListingsApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
 
namespace ListingsApi.Endpoints;
 
public static class ListingEndpoints
{
    public static void MapListingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/listings").WithTags("Listings");
 
        group.MapGet("/mine", async (ClaimsPrincipal user, ListingService service) =>
            TypedResults.Ok(await service.GetMineAsync(GetRequiredUserId(user))))
        .RequireAuthorization()
        .WithName("GetMyListings");

        group.MapGet("/", async (ListingService service, ClaimsPrincipal user, int? districtId, decimal? maxPrice) =>
            TypedResults.Ok(await service.GetAllAsync(districtId, maxPrice, GetUserId(user))))
        .WithName("GetListings")
        .WithSummary("Список объявлений с фильтром по району (id) и максимальной цене");

        group.MapGet("/{id:int}", async Task<Results<Ok<ListingResponse>, NotFound>>
            (int id, ClaimsPrincipal user, ListingService service) =>
            await service.GetAsync(id, GetUserId(user)) is { } dto
                ? TypedResults.Ok(dto)
                : TypedResults.NotFound())
        .WithName("GetListing");
 
        group.MapPost("/", async Task<Results<Created<ListingResponse>, BadRequest<string>>>
            (CreateListingRequest request, ClaimsPrincipal user, ListingService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title)) return TypedResults.BadRequest("Поле title обязательно");
            if (request.Price <= 0) return TypedResults.BadRequest("Поле price должно быть больше 0");
            if (request.Rooms is < 1 or > 10) return TypedResults.BadRequest("Поле rooms — от 1 до 10");
 
            var ownerId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var created = await service.CreateAsync(request, ownerId);
            if (created is null) return TypedResults.BadRequest("Район с таким districtId не найден");
 
            return TypedResults.Created($"/api/listings/{created.Id}", created);
        })
        .RequireAuthorization()
        .WithName("CreateListing");
 
        group.MapPut("/{id:int}", async Task<Results<NoContent, NotFound, BadRequest<string>>>
            (int id, UpdateListingRequest request, ClaimsPrincipal user, ListingService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
                return TypedResults.BadRequest("Название обязательно и не должно превышать 200 символов.");
            if (request.Price <= 0)
                return TypedResults.BadRequest("Цена должна быть больше 0.");
            if (request.Rooms is < 1 or > 10)
                return TypedResults.BadRequest("Количество комнат должно быть от 1 до 10.");
            if (request.Address?.Length > 300)
                return TypedResults.BadRequest("Адрес не должен превышать 300 символов.");
            var ownerId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return await service.UpdateAsync(id, request, ownerId)
                ? TypedResults.NoContent()
                : TypedResults.NotFound();
        })
        .RequireAuthorization()
        .WithName("UpdateListing");
 
        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>>
            (int id, ClaimsPrincipal user, ListingService service) =>
            await service.DeleteAsync(id, GetRequiredUserId(user), user.IsInRole("Admin"))
                ? TypedResults.NoContent() : TypedResults.NotFound())
        .RequireAuthorization()
        .WithName("DeleteListing");

        app.MapDelete("/api/admin/listings/{id:int}", async Task<Results<NoContent, NotFound>>
            (int id, ListingService service) =>
            await service.DeleteAsync(id, 0, isAdmin: true)
                ? TypedResults.NoContent() : TypedResults.NotFound())
        .RequireAuthorization("AdminOnly")
        .WithTags("Admin")
        .WithName("AdminDeleteListing");
    }

    private static int? GetUserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    private static int GetRequiredUserId(ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
