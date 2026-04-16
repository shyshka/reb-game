namespace RebelGame.Models;

public class WorkerUnit
{
    public Guid   Id      { get; set; } = Guid.NewGuid();
    public Guid   HomeId  { get; set; }          // Hunter's Lodge that spawned this worker
    public Guid?  AssignedBuildingId  { get; set; } // null = wandering freely near lodge
    public Guid?  PreviousBuildingId  { get; set; } // building to return to after resting
    public Guid?  WaitingForRestHouseId { get; set; } // queued for a rest house bed
    public double X       { get; set; }
    public double Y       { get; set; }
    public double TargetX { get; set; }
    public double TargetY { get; set; }
    public bool   FacingLeft { get; set; }
    public double Health     { get; set; } = 100;   // 0–100, decreases over time
    public double RestTimer  { get; set; } = 0;     // seconds spent resting in lodge
}
