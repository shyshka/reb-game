namespace RebelGame.Models;

public class PlacedBuilding
{
    public Guid   Id           { get; set; } = Guid.NewGuid();
    public string BuildingId   { get; set; } = string.Empty;
    public double X            { get; set; }
    public double Y            { get; set; }
    public bool   IsConstructing       { get; set; } = true;
    public double ConstructionProgress { get; set; } = 0; // 0→1
    public double Health               { get; set; } = 100; // 0–100
}
