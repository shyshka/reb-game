using RebelGame.Models;

namespace RebelGame.Services;

public class GameService(StorageService storage) : IAsyncDisposable
{
    private static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxOfflineTime = TimeSpan.FromHours(8);

    private readonly CancellationTokenSource _cts = new();
    private Task? _gameLoop;
    private DateTime _lastTick;
    private DateTime _lastSave;

    public GameState State { get; private set; } = new();
    public event Action? OnStateChanged;

    public async Task InitializeAsync()
    {
        var saved = await storage.LoadStateAsync();
        if (saved != null)
        {
            State = saved;
            ApplyOfflineProduction();
        }

        _lastTick = DateTime.UtcNow;
        _lastSave = DateTime.UtcNow;
        _gameLoop = RunGameLoopAsync(_cts.Token);
    }

    private void ApplyOfflineProduction()
    {
        var offline = DateTime.UtcNow - State.LastSaved;
        if (offline > MaxOfflineTime) offline = MaxOfflineTime;
        ProduceResources(offline.TotalSeconds);
    }

    private async Task RunGameLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try { await Task.Delay(100, token); }
            catch (TaskCanceledException) { break; }
            Tick();
        }
    }

    private static readonly Random _rng = new();

    private void Tick()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastTick).TotalSeconds;
        _lastTick = now;

        ProduceResources(elapsed);
        MoveWorkers(elapsed);

        if (now - _lastSave >= SaveInterval)
        {
            State.LastSaved = now;
            _ = storage.SaveStateAsync(State);
            _lastSave = now;
        }

        OnStateChanged?.Invoke();
    }

    private void ProduceResources(double seconds)
    {
        foreach (var placed in State.Map)
        {
            var def = BuildingCatalog.GetById(placed.BuildingId);
            if (def == null) continue;
            State.Money += def.MoneyPerSecond * seconds;
            State.Energy += def.EnergyPerSecond * seconds;
        }
    }

    public double GetCurrentCost(string buildingId)
    {
        var def = BuildingCatalog.GetById(buildingId);
        if (def == null) return double.MaxValue;
        var owned = GetBuildingCount(buildingId);
        return def.BaseCost * Math.Pow(def.CostMultiplier, owned);
    }

    public bool CanAfford(string buildingId) => State.Money >= GetCurrentCost(buildingId);

    public bool PlaceBuilding(string buildingId, double x, double y)
    {
        if (!CanAfford(buildingId)) return false;
        if (IsTooClose(x, y)) return false;

        x = Math.Clamp(x, 0, GameConfig.MapWidth);
        y = Math.Clamp(y, 0, GameConfig.MapHeight);

        State.Money -= GetCurrentCost(buildingId);
        State.Map.Add(new PlacedBuilding { BuildingId = buildingId, X = x, Y = y });
        OnStateChanged?.Invoke();
        return true;
    }

    private bool IsTooClose(double x, double y) =>
        State.Map.Any(p =>
            Math.Abs(p.X - x) < GameConfig.MinSpacing &&
            Math.Abs(p.Y - y) < GameConfig.MinSpacing);

    public int GetBuildingCount(string buildingId) =>
        State.Map.Count(p => p.BuildingId == buildingId);

    public double GetSellPrice(string buildingId)
    {
        var def = BuildingCatalog.GetById(buildingId);
        if (def == null) return 0;
        var owned = GetBuildingCount(buildingId);
        if (owned == 0) return 0;
        return 0.5 * def.BaseCost * Math.Pow(def.CostMultiplier, owned - 1);
    }

    public void SellBuilding(Guid id)
    {
        var placed = State.Map.FirstOrDefault(p => p.Id == id);
        if (placed == null) return;
        State.Money += GetSellPrice(placed.BuildingId);
        State.Map.Remove(placed);
        State.Workers.RemoveAll(w => w.HomeId == id);
        OnStateChanged?.Invoke();
    }

    public bool CreateWorker(Guid lodgeId)
    {
        if (State.Money < GameConfig.WorkerCost) return false;
        var lodge = State.Map.FirstOrDefault(b => b.Id == lodgeId);
        if (lodge == null) return false;

        State.Money -= GameConfig.WorkerCost;
        var w = new WorkerUnit
        {
            HomeId  = lodgeId,
            X       = lodge.X + (_rng.NextDouble() - 0.5) * 30,
            Y       = lodge.Y + GameConfig.BuildingSize / 2 + 10
        };
        w.TargetX = Math.Clamp(lodge.X + (_rng.NextDouble() - 0.5) * 80, 20, GameConfig.MapWidth  - 20);
        w.TargetY = Math.Clamp(lodge.Y + 40 + _rng.NextDouble() * 60,     20, GameConfig.MapHeight - 20);
        State.Workers.Add(w);
        OnStateChanged?.Invoke();
        return true;
    }

    private void MoveWorkers(double seconds)
    {
        foreach (var w in State.Workers)
        {
            var dx   = w.TargetX - w.X;
            var dy   = w.TargetY - w.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < 5)
            {
                // Arrived — pick a new random target near home lodge
                var lodge = State.Map.FirstOrDefault(b => b.Id == w.HomeId);
                var homeX = lodge?.X ?? w.X;
                var homeY = lodge?.Y ?? w.Y;
                var angle  = _rng.NextDouble() * Math.PI * 2;
                var radius = _rng.NextDouble() * GameConfig.WorkerHomeRadius;
                w.TargetX = Math.Clamp(homeX + Math.Cos(angle) * radius, 20, GameConfig.MapWidth  - 20);
                w.TargetY = Math.Clamp(homeY + Math.Sin(angle) * radius, 20, GameConfig.MapHeight - 20);
            }
            else
            {
                var step = Math.Min(GameConfig.WorkerSpeed * seconds, dist);
                w.FacingLeft = dx < 0;
                w.X += dx / dist * step;
                w.Y += dy / dist * step;
            }
        }
    }

    public double GetMoneyRate() => State.Map
        .Sum(p => BuildingCatalog.GetById(p.BuildingId)?.MoneyPerSecond ?? 0);

    public double GetEnergyRate() => State.Map
        .Sum(p => BuildingCatalog.GetById(p.BuildingId)?.EnergyPerSecond ?? 0);

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        if (_gameLoop != null) await _gameLoop;
        _cts.Dispose();
    }
}
