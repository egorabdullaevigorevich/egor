using ListingsApi.Contracts;
using ListingsApi.Models;
 
namespace ListingsApi.Services;
 
public class ListingStore
{
    private readonly List<Listing> _items = new();
    private readonly object _lock = new();
    private int _nextId = 1;
 
    public ListingStore()
    {
        // стартовые данные, чтобы список не был пустым
        Add(new CreateListingRequest("2-комнатная, Юнусабад", 62000, "Юнусабад", 2));
        Add(new CreateListingRequest("3-комнатная, Чиланзар-9", 41000, "Чиланзар", 3));
        Add(new CreateListingRequest("3-комнатная, Мирзo-Улугбек", 95000, "Мирзо-Улугбек", 3));
        Add(new CreateListingRequest("3-комнатная, Чиланзар-8", 78600, "Чиланзар", 3));
        Add(new CreateListingRequest("2-комнатная, Чиланзар-9", 45000, "Чиланзар", 2));
    }
 
    public IReadOnlyList<Listing> GetAll()
    {
        lock (_lock) return _items.ToList();
    }
 
    public Listing? Get(int id)
    {
        lock (_lock) return _items.Find(l => l.Id == id);
    }
 
    public Listing Add(CreateListingRequest r)
    {
        lock (_lock)
        {
            var listing = new Listing
            {
                Id = _nextId++, Title = r.Title, Price = r.Price,
                District = r.District, Rooms = r.Rooms, CreatedAt = DateTime.UtcNow
            };
            _items.Add(listing);
            return listing;
        }
    }
 
    public bool Update(int id, UpdateListingRequest r)
    {
        lock (_lock)
        {
            var listing = _items.Find(l => l.Id == id);
            if (listing is null) return false;
            listing.Title = r.Title; listing.Price = r.Price;
            listing.District = r.District; listing.Rooms = r.Rooms;
            return true;
        }
    }
 
    public bool Delete(int id)
    {
        lock (_lock) return _items.RemoveAll(l => l.Id == id) > 0;
    }
}
