namespace ListingsApi.Contracts;
 
// то, что клиент присылает при создании
public record CreateListingRequest(string Title, decimal Price, string District, int Rooms);
 
// то, что клиент присылает при полной замене (PUT)
public record UpdateListingRequest(string Title, decimal Price, string District, int Rooms);
 
// то, что клиент получает
public record ListingResponse(int Id, string Title, decimal Price, string District, int Rooms, DateTime CreatedAt);