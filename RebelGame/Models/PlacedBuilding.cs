namespace RebelGame.Models;

public class PlacedBuilding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BuildingId { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}
