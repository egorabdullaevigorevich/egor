using ListingsApi.Contracts;
using ListingsApi.Data;
using ListingsApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
 
namespace ListingsApi.Services;
 
public class ListingService(AppDbContext db)
{
    private static readonly System.Linq.Expressions.Expression<Func<Listing, ListingResponse>> ToResponse =
        l => new ListingResponse(l.Id, l.Title, l.Price, l.Rooms,
                                 l.DistrictId, l.District!.Name, l.Address, l.CreatedAt);
 
    public async Task<List<ListingResponse>> GetAllAsync(int? districtId, decimal? maxPrice)
    {
        var query = db.Listings.AsNoTracking().AsQueryable();
 
        if (districtId is not null) query = query.Where(l => l.DistrictId == districtId);
        if (maxPrice is not null)   query = query.Where(l => l.Price <= maxPrice);
 
        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Select(ToResponse)
            .ToListAsync();
    }
 
    public async Task<ListingResponse?> GetAsync(int id) =>
        await db.Listings.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync();

    // Наш бонусный метод подсчета для эндпоинта /count
    public async Task<int> GetCountAsync() =>
        await db.Listings.CountAsync();
 
    public async Task<ListingResponse?> CreateAsync(CreateListingRequest r)
    {
        if (!await db.Districts.AnyAsync(d => d.Id == r.DistrictId))
            return null;
 
        var listing = new Listing
        {
            Title = r.Title, Price = r.Price, Rooms = r.Rooms,
            DistrictId = r.DistrictId, Address = r.Address, CreatedAt = DateTime.UtcNow
        };
        db.Listings.Add(listing);
        await db.SaveChangesAsync();                 
 
        return (await GetAsync(listing.Id))!;        
    }
 
    public async Task<bool> UpdateAsync(int id, UpdateListingRequest r)
    {
        var listing = await db.Listings.FindAsync(id);   
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
 
    public async Task<bool> DeleteAsync(int id)
    {
        var listing = await db.Listings.FindAsync(id);
        if (listing is null) return false;
 
        db.Listings.Remove(listing);
        await db.SaveChangesAsync();                 
        return true;
    }
}
