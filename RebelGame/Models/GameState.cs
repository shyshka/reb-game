namespace RebelGame.Models;

public class GameState
{
    // Air raid state
    public List<AirRaidPlane> ActivePlanes { get; set; } = new();
    public DateTime NextRaidTime { get; set; } = DateTime.UtcNow.AddSeconds(20);
    public int PlanesRemaining { get; set; } = 5;
    public DateTime LastPlaneLaunch { get; set; } = DateTime.MinValue;

    public double Money { get; set; } = 100;
    public double Energy { get; set; } = 0;
    public List<PlacedBuilding> Map     { get; set; } = new();
    public List<WorkerUnit>     Workers { get; set; } = new();
    public DateTime LastSaved { get; set; } = DateTime.UtcNow;
}
