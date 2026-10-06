namespace ListingsApi.Contracts;
 
public record CreateListingRequest(string Title, decimal Price, int Rooms, int DistrictId, string? Address);
public record UpdateListingRequest(string Title, decimal Price, int Rooms, int DistrictId, string? Address);
 
public record ListingResponse(
    int Id, string Title, decimal Price, int Rooms,
    int DistrictId, string DistrictName, string? Address, DateTime CreatedAt);
 
public record DistrictResponse(int Id, string Name, int ListingsCount);
