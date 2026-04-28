namespace RebelGame.Models;

public class AirRaidPlane
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public double X { get; set; }
    public double Y { get; set; }
    public bool HasAttacked { get; set; } = false;
    public Guid? TargetBuildingId { get; set; }
    public DateTime LaunchTime { get; set; }
}
