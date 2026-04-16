namespace RebelGame.Models;

public static class BuildingCatalog
{
    public static readonly List<BuildingDefinition> All = new()
    {
        new BuildingDefinition
        {
            Id = "solar_station",
            Name = "Solar Station",
            Description = "Harnesses sunlight to generate passive income.",
            Icon = "☀️",
            BaseCost = 10,
            MoneyPerSecond = 0.1,
            EnergyPerSecond = 0.05,
            CostMultiplier = 1.15,
            BuildTimeSecs = 10,
            RequiredWorkers = 1
        },
        new BuildingDefinition
        {
            Id = "wind_farm",
            Name = "Wind Farm",
            Description = "Converts wind energy into electricity and money.",
            Icon = "🌬️",
            BaseCost = 100,
            MoneyPerSecond = 0.8,
            EnergyPerSecond = 0.2,
            CostMultiplier = 1.15,
            BuildTimeSecs = 25,
            RequiredWorkers = 2
        },
        new BuildingDefinition
        {
            Id = "power_plant",
            Name = "Power Plant",
            Description = "A large facility generating substantial energy and income.",
            Icon = "⚡",
            BaseCost = 1_000,
            MoneyPerSecond = 5.0,
            EnergyPerSecond = 2.0,
            CostMultiplier = 1.15,
            BuildTimeSecs = 60,
            RequiredWorkers = 3
        },
        new BuildingDefinition
        {
            Id = "hunter_lodge",
            Name = "Hunter's Lodge",
            Description = "Train workers to patrol and gather resources.",
            Icon = "🏕️",
            BaseCost = 300,
            MoneyPerSecond = 0,
            EnergyPerSecond = 0,
            CostMultiplier = 1.20,
            BuildTimeSecs = 30,
            RequiredWorkers = 1,
            WorkerCapacity = 5
        },
        new BuildingDefinition
        {
            Id = "rest_house",
            Name = "Rest House",
            Description = "A place for workers to rest and fully recover their health.",
            Icon = "🛖",
            BaseCost = 200,
            MoneyPerSecond = 0,
            EnergyPerSecond = 0,
            CostMultiplier = 1.20,
            BuildTimeSecs = 20,
            RequiredWorkers = 0,
            WorkerCapacity = 2
        }
    };

    public static BuildingDefinition? GetById(string id) =>
        All.FirstOrDefault(b => b.Id == id);
}
