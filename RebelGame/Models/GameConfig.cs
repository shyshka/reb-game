namespace RebelGame.Models;

public static class GameConfig
{
    public const double MapWidth          = 4000;
    public const double MapHeight         = 2400;
    public const double BuildingSize      = 80;
    public const double MinSpacing        = 70;
    public const double MinZoom           = 0.40;
    public const double MaxZoom           = 3.0;

    // Workers
    public const double WorkerCost         = 50;
    public const double WorkerSalary       = 0.5;   // money per second per worker
    public const double WorkerSpeed        = 60;    // world px per second
    public const double WorkerHomeRadius   = 320;   // max wander distance from home lodge
    public const double WorkerHealthDecay  = 0.1;   // HP lost per second while active
    public const double WorkerRestDuration = 20.0;  // seconds inside rest house to fully restore health
    public const double WorkerRestRadius   = 50;    // stay close to lodge when resting

    // Buildings
    public const double BuildingHealthDecay     = 0.2;   // HP lost per second (always)
    public const double BuildingRegenPerWorker  = 0.25;  // HP gained per second per assigned worker
    public const double RepairCostPerPoint      = 0.5;   // money cost per 1 HP repaired
    public const double WorkerAssignRadius      = 120;   // how close assigned workers stay to their building
}
