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
        Add(new CreateListingRequest("2-комн. рядом с метро Чиланзар", 48000, "Чиланзар", "ул. Мукими, 12", 2));
        Add(new CreateListingRequest("3-комн. с ремонтом", 71000, "Юнусабад", "квартал 4, дом 7", 3));
        Add(new CreateListingRequest("1-комн. студия", 32000, "Мирзо-Улугбек", "пр-т Мустакиллик, 45", 1));
        Add(new CreateListingRequest("3-комн. на Чиланзаре", 78600, "Чиланзар", "квартал 8, д. 21", 3));
        Add(new CreateListingRequest("2-комн. уютная", 45000, "Чиланзар", "квартал 9, д. 5", 2));
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
                District = r.District, 
                Address = r.Address,
                Rooms = r.Rooms, CreatedAt = DateTime.UtcNow
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
            listing.District = r.District; 
            listing.Address = r.Address;
            listing.Rooms = r.Rooms;
            return true;
        }
    }
 
    public bool Delete(int id)
    {
        lock (_lock) return _items.RemoveAll(l => l.Id == id) > 0;
    }
}
