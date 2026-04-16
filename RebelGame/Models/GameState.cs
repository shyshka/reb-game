namespace RebelGame.Models;

public class GameState
{
    public double Money { get; set; } = 100;
    public double Energy { get; set; } = 0;
    public List<PlacedBuilding> Map     { get; set; } = new();
    public List<WorkerUnit>     Workers { get; set; } = new();
    public DateTime LastSaved { get; set; } = DateTime.UtcNow;
}
