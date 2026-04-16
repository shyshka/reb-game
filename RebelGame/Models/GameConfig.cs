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
    public const double WorkerCost        = 50;
    public const double WorkerSpeed       = 60;   // world px per second
    public const double WorkerHomeRadius  = 320;  // max wander distance from home lodge
}
