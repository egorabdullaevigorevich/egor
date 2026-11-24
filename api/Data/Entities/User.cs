namespace ListingsApi.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; }
    public List<Listing> Listings { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}