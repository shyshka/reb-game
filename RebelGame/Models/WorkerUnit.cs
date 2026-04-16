namespace RebelGame.Models;

public class WorkerUnit
{
    public Guid   Id      { get; set; } = Guid.NewGuid();
    public Guid   HomeId  { get; set; }   // Hunter's Lodge that spawned this worker
    public double X       { get; set; }
    public double Y       { get; set; }
    public double TargetX { get; set; }
    public double TargetY { get; set; }
    public bool   FacingLeft { get; set; }
}
