namespace RebelGame.Models;

public class BuildingDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public double BaseCost { get; set; }
    public double MoneyPerSecond { get; set; }
    public double EnergyPerSecond { get; set; }
    public double CostMultiplier { get; set; } = 1.15;
}
