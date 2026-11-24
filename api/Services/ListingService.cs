using ListingsApi.Contracts;
using ListingsApi.Data;
using ListingsApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
 
namespace ListingsApi.Services;
 
public class ListingService(AppDbContext db)
{
    public async Task<List<ListingResponse>> GetAllAsync(int? districtId, decimal? maxPrice, int? viewerId)
    {
        var query = db.Listings.AsNoTracking().AsQueryable();
 
        if (districtId is not null) query = query.Where(l => l.DistrictId == districtId);
        if (maxPrice is not null)   query = query.Where(l => l.Price <= maxPrice);
 
        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new ListingResponse(l.Id, l.Title, l.Price, l.Rooms,
                l.DistrictId, l.District!.Name, l.Address, l.CreatedAt,
                viewerId != null && l.OwnerId == viewerId))
            .ToListAsync();
    }
 
    public async Task<ListingResponse?> GetAsync(int id, int? viewerId = null) =>
        await db.Listings.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new ListingResponse(l.Id, l.Title, l.Price, l.Rooms,
                l.DistrictId, l.District!.Name, l.Address, l.CreatedAt,
                viewerId != null && l.OwnerId == viewerId))
            .FirstOrDefaultAsync();

    // Наш бонусный метод подсчета для эндпоинта /count
    public async Task<int> GetCountAsync() =>
        await db.Listings.CountAsync();
 
    public async Task<ListingResponse?> CreateAsync(CreateListingRequest r, int ownerId)
    {
        if (!await db.Districts.AnyAsync(d => d.Id == r.DistrictId))
            return null;
 
        var listing = new Listing
        {
            Title = r.Title, Price = r.Price, Rooms = r.Rooms,
            DistrictId = r.DistrictId, Address = r.Address, CreatedAt = DateTime.UtcNow,
            OwnerId = ownerId,
        };
        db.Listings.Add(listing);
        await db.SaveChangesAsync();                 
 
        return (await GetAsync(listing.Id, ownerId))!;
    }
 
    public async Task<bool> UpdateAsync(int id, UpdateListingRequest r, int ownerId)
    {
        var listing = await db.Listings.SingleOrDefaultAsync(l => l.Id == id && l.OwnerId == ownerId);
        if (listing is null) return false;
        if (!await db.Districts.AnyAsync(d => d.Id == r.DistrictId)) return false;
 
        listing.Title = r.Title;
        listing.Price = r.Price;
        listing.Rooms = r.Rooms;
        listing.DistrictId = r.DistrictId;
        listing.Address = r.Address;
        await db.SaveChangesAsync();                 
        return true;
    }
 
    public async Task<bool> DeleteAsync(int id, int ownerId)
    {
        var listing = await db.Listings.SingleOrDefaultAsync(l => l.Id == id && l.OwnerId == ownerId);
        if (listing is null) return false;
 
        db.Listings.Remove(listing);
        await db.SaveChangesAsync();                 
        return true;
    }
}
