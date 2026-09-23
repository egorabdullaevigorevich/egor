namespace ListingsApi.Models;
 
public class Listing
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public decimal Price { get; set; }        // в условных единицах
    public string District { get; set; } = "";
    public int Rooms { get; set; }
    public DateTime CreatedAt { get; set; }
}