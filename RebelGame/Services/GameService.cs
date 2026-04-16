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
        PaySalaries(elapsed);
        UpdateConstruction(elapsed);
        UpdateBuildingHealth(elapsed);
        ProcessRestHouseQueue();
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
            if (placed.IsConstructing) continue;
            if (placed.Health <= 0) continue;
            var def = BuildingCatalog.GetById(placed.BuildingId);
            if (def == null) continue;
            var efficiency = placed.Health / 100.0;
            State.Money  += def.MoneyPerSecond  * seconds * efficiency;
            State.Energy += def.EnergyPerSecond * seconds * efficiency;
        }
    }

    private void PaySalaries(double seconds)
    {
        var totalSalary = State.Workers.Count * GameConfig.WorkerSalary * seconds;
        if (State.Money >= totalSalary)
        {
            State.Money -= totalSalary;
            return;
        }
        // Can't afford full salary — pay what we can, fire workers until balanced
        State.Money = 0;
        while (State.Workers.Count > 0)
        {
            var perWorkerPerTick = GameConfig.WorkerSalary * seconds;
            if (State.Money >= State.Workers.Count * perWorkerPerTick) break;
            // Fire the least useful worker: free > resting > assigned (last hired = last in list)
            var toLay = State.Workers
                .OrderBy(w => w.AssignedBuildingId.HasValue ? 1 : 0)
                .ThenByDescending(w => State.Workers.IndexOf(w))
                .First();
            UnassignWorker(toLay.Id);
            State.Workers.Remove(toLay);
        }
    }


    private void UpdateBuildingHealth(double seconds)
    {
        foreach (var b in State.Map)
        {
            if (b.IsConstructing) continue;
            var def = BuildingCatalog.GetById(b.BuildingId);
            if (def?.RequiredWorkers == 0) continue;   // no decay for buildings that need no workers
            var assigned = State.Workers.Count(w => w.AssignedBuildingId == b.Id);
            var regen    = assigned * GameConfig.BuildingRegenPerWorker * seconds;
            var decay    = GameConfig.BuildingHealthDecay * seconds;
            b.Health = Math.Clamp(b.Health + regen - decay, 0, 100);
        }
    }

    // Returns the "beside the building" anchor point — bottom-right edge, offset by worker index
    private (double x, double y) GetWorkSideOffset(PlacedBuilding b, int workerIndex = 0)
    {
        var edge = GameConfig.BuildingSize / 2 + 14;  // just outside the building edge
        // Alternate sides for multiple workers: right, bottom, left, top
        return (workerIndex % 4) switch
        {
            0 => (b.X + edge,  b.Y + edge * 0.5),
            1 => (b.X - edge,  b.Y + edge * 0.5),
            2 => (b.X + edge,  b.Y - edge * 0.5),
            _ => (b.X - edge,  b.Y - edge * 0.5),
        };
    }

    public void AssignWorker(Guid workerId, Guid buildingId)
    {
        var w = State.Workers.FirstOrDefault(x => x.Id == workerId);
        if (w == null) return;
        w.AssignedBuildingId = buildingId;
        var b = State.Map.FirstOrDefault(x => x.Id == buildingId);
        if (b != null)
        {
            var idx = State.Workers.Count(x => x.AssignedBuildingId == buildingId) - 1;
            var (sx, sy) = GetWorkSideOffset(b, Math.Max(0, idx));
            w.TargetX = Math.Clamp(sx + (_rng.NextDouble() - 0.5) * 8, 20, GameConfig.MapWidth  - 20);
            w.TargetY = Math.Clamp(sy + (_rng.NextDouble() - 0.5) * 8, 20, GameConfig.MapHeight - 20);
        }
        OnStateChanged?.Invoke();
    }

    public void UnassignWorker(Guid workerId)
    {
        var w = State.Workers.FirstOrDefault(x => x.Id == workerId);
        if (w == null) return;
        w.AssignedBuildingId     = null;
        w.PreviousBuildingId     = null;
        OnStateChanged?.Invoke();
    }

    public IEnumerable<WorkerUnit> GetFreeWorkers() =>
        State.Workers.Where(w => w.AssignedBuildingId == null);

    public IEnumerable<WorkerUnit> GetAssignedWorkers(Guid buildingId) =>
        State.Workers.Where(w => w.AssignedBuildingId == buildingId);

    public bool RepairBuilding(Guid id)
    {
        var b = State.Map.FirstOrDefault(x => x.Id == id);
        if (b == null || b.IsConstructing) return false;
        var missing = 100 - b.Health;
        if (missing <= 0) return false;
        var cost = missing * GameConfig.RepairCostPerPoint;
        if (State.Money < cost) return false;
        State.Money -= cost;
        b.Health = 100;
        OnStateChanged?.Invoke();
        return true;
    }

    public double GetRepairCost(Guid id)
    {
        var b = State.Map.FirstOrDefault(x => x.Id == id);
        if (b == null) return 0;
        return Math.Ceiling((100 - b.Health) * GameConfig.RepairCostPerPoint);
    }

    private void UpdateConstruction(double seconds)
    {
        foreach (var b in State.Map)
        {
            if (!b.IsConstructing) continue;
            var def = BuildingCatalog.GetById(b.BuildingId);
            if (def == null) continue;
            b.ConstructionProgress += seconds / def.BuildTimeSecs;
            if (b.ConstructionProgress >= 1.0)
            {
                b.ConstructionProgress = 1.0;
                b.IsConstructing = false;
            }
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
        var def = BuildingCatalog.GetById(lodge.BuildingId);
        if (def == null) return false;

        var currentCount = State.Workers.Count(w => w.HomeId == lodgeId);
        if (def.WorkerCapacity > 0 && currentCount >= def.WorkerCapacity) return false;

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
            var isResting = IsWorkerResting(w);

            if (isResting)
            {
                // Only count rest time once the worker has arrived at the rest house
                var restBld = State.Map.FirstOrDefault(b => b.Id == w.AssignedBuildingId);
                if (restBld != null)
                {
                    var distToRest = Math.Sqrt(Math.Pow(w.X - restBld.X, 2) + Math.Pow(w.Y - restBld.Y, 2));
                    if (distToRest <= GameConfig.BuildingSize)
                        w.RestTimer += seconds;
                }
                if (w.RestTimer >= GameConfig.WorkerRestDuration)
                {
                    // Fully healed — wake up
                    w.Health    = 100;
                    w.RestTimer = 0;
                    var prevBld = w.PreviousBuildingId.HasValue
                        ? State.Map.FirstOrDefault(b => b.Id == w.PreviousBuildingId && !b.IsConstructing && b.Health > 0)
                        : null;
                    if (prevBld != null)
                    {
                        w.AssignedBuildingId = w.PreviousBuildingId;
                        w.PreviousBuildingId = null;
                        var idx = State.Workers.Where(x => x.AssignedBuildingId == w.AssignedBuildingId)
                                               .ToList().IndexOf(w);
                        var (sx, sy) = GetWorkSideOffset(prevBld, Math.Max(0, idx));
                        w.TargetX = Math.Clamp(sx + (_rng.NextDouble() - 0.5) * 6, 20, GameConfig.MapWidth  - 20);
                        w.TargetY = Math.Clamp(sy + (_rng.NextDouble() - 0.5) * 6, 20, GameConfig.MapHeight - 20);
                    }
                    else
                    {
                        w.AssignedBuildingId = null;
                        w.PreviousBuildingId = null;
                    }
                    continue;
                }
            }
            else
            {
                w.RestTimer = 0;
                w.Health = Math.Max(0, w.Health - GameConfig.WorkerHealthDecay * seconds);
            }

            // Auto-rest: send to nearest rest house when health drops below 50%
            if (!isResting && w.Health < 50)
            {
                var restHouse = FindRestHouse(w);
                if (restHouse != null)
                {
                    if (w.AssignedBuildingId.HasValue && w.AssignedBuildingId != restHouse.Id)
                        w.PreviousBuildingId = w.AssignedBuildingId;
                    w.AssignedBuildingId = restHouse.Id;
                    w.RestTimer = 0;
                    w.TargetX = Math.Clamp(restHouse.X + (_rng.NextDouble() - 0.5) * GameConfig.WorkerRestRadius, 20, GameConfig.MapWidth  - 20);
                    w.TargetY = Math.Clamp(restHouse.Y + (_rng.NextDouble() - 0.5) * GameConfig.WorkerRestRadius, 20, GameConfig.MapHeight - 20);
                    continue;
                }
            }

            var dx   = w.TargetX - w.X;
            var dy   = w.TargetY - w.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < 5)
            {
                PlacedBuilding? anchor = w.AssignedBuildingId.HasValue
                    ? State.Map.FirstOrDefault(b => b.Id == w.AssignedBuildingId)
                    : null;
                if (anchor == null)
                    anchor = State.Map.FirstOrDefault(b => b.Id == w.HomeId);

                double homeX, homeY;
                if (!isResting && w.AssignedBuildingId.HasValue && anchor != null)
                {
                    // Stay beside the building, not on top of it
                    var idx = State.Workers.Where(x => x.AssignedBuildingId == w.AssignedBuildingId)
                                           .ToList().IndexOf(w);
                    (homeX, homeY) = GetWorkSideOffset(anchor, Math.Max(0, idx));
                }
                else
                {
                    homeX = anchor?.X ?? w.X;
                    homeY = anchor?.Y ?? w.Y;
                }

                var radius = isResting
                    ? GameConfig.WorkerRestRadius
                    : w.AssignedBuildingId.HasValue
                        ? 6
                        : GameConfig.WorkerHomeRadius;

                var angle = _rng.NextDouble() * Math.PI * 2;
                var r     = _rng.NextDouble() * radius;
                w.TargetX = Math.Clamp(homeX + Math.Cos(angle) * r, 20, GameConfig.MapWidth  - 20);
                w.TargetY = Math.Clamp(homeY + Math.Sin(angle) * r, 20, GameConfig.MapHeight - 20);
            }
            else
            {
                var speed = isResting ? GameConfig.WorkerSpeed * 0.4 : GameConfig.WorkerSpeed;
                var step  = Math.Min(speed * seconds, dist);
                w.FacingLeft = dx < 0;
                w.X += dx / dist * step;
                w.Y += dy / dist * step;
            }
        }

        // Remove dead workers
        State.Workers.RemoveAll(w => w.Health <= 0);
    }

    public bool IsWorkerResting(WorkerUnit w)
    {
        if (!w.AssignedBuildingId.HasValue) return false;
        var b = State.Map.FirstOrDefault(x => x.Id == w.AssignedBuildingId);
        return b != null
            && !b.IsConstructing
            && b.Health > 0
            && BuildingCatalog.GetById(b.BuildingId)?.Id == "rest_house";
    }

    private void ProcessRestHouseQueue() { }  // no-op, queue removed



    public PlacedBuilding? FindRestHousePublic(WorkerUnit w)
    {
        return FindRestHouse(w);
    }

    private PlacedBuilding? FindRestHouse(WorkerUnit w)
    {
        return State.Map
            .Where(b => !b.IsConstructing && b.Health > 0
                     && BuildingCatalog.GetById(b.BuildingId)?.Id == "rest_house")
            .Where(b =>
            {
                var cap = BuildingCatalog.GetById(b.BuildingId)?.WorkerCapacity ?? 0;
                var used = State.Workers.Count(x => x.AssignedBuildingId == b.Id);
                return cap <= 0 || used < cap;
            })
            .OrderBy(b => Math.Sqrt(Math.Pow(b.X - w.X, 2) + Math.Pow(b.Y - w.Y, 2)))
            .FirstOrDefault();
    }

    public int GetLodgeCapacity(Guid lodgeId)
    {
        var b = State.Map.FirstOrDefault(x => x.Id == lodgeId);
        return BuildingCatalog.GetById(b?.BuildingId ?? "")?.WorkerCapacity ?? 0;
    }

    public int GetLodgeWorkerCount(Guid lodgeId) =>
        State.Workers.Count(w => w.HomeId == lodgeId);

    public bool CanLodgeHireMore(Guid lodgeId)
    {
        var cap = GetLodgeCapacity(lodgeId);
        return cap <= 0 || GetLodgeWorkerCount(lodgeId) < cap;
    }

    public int GetRestHouseCapacity(Guid id)
    {
        var b = State.Map.FirstOrDefault(x => x.Id == id);
        return BuildingCatalog.GetById(b?.BuildingId ?? "")?.WorkerCapacity ?? 0;
    }

    public int GetRestHouseOccupancy(Guid id) =>
        State.Workers.Count(w => w.AssignedBuildingId == id);

    public bool HasRestHouseSpace(Guid id)
    {
        var cap = GetRestHouseCapacity(id);
        return cap <= 0 || GetRestHouseOccupancy(id) < cap;
    }

    public void SendToRestFromBuilding(Guid workerId)
    {
        var w = State.Workers.FirstOrDefault(x => x.Id == workerId);
        if (w == null || !w.AssignedBuildingId.HasValue) return;
        var restHouse = FindRestHouse(w);
        if (restHouse == null) return;
        w.PreviousBuildingId = w.AssignedBuildingId;
        w.AssignedBuildingId = restHouse.Id;
        w.RestTimer = 0;
        var angle = _rng.NextDouble() * Math.PI * 2;
        var r     = _rng.NextDouble() * GameConfig.WorkerRestRadius;
        w.TargetX = Math.Clamp(restHouse.X + Math.Cos(angle) * r, 20, GameConfig.MapWidth  - 20);
        w.TargetY = Math.Clamp(restHouse.Y + Math.Cos(angle) * r, 20, GameConfig.MapHeight - 20);
        OnStateChanged?.Invoke();
    }

    public IEnumerable<WorkerUnit> GetReturningWorkers(Guid buildingId) =>
        State.Workers.Where(w => w.PreviousBuildingId == buildingId);

    public void SendToRest(Guid workerId, Guid restHouseId)
    {
        var w = State.Workers.FirstOrDefault(x => x.Id == workerId);
        if (w == null) return;
        if (w.AssignedBuildingId.HasValue && w.AssignedBuildingId != restHouseId)
            w.PreviousBuildingId = w.AssignedBuildingId;
        w.RestTimer = 0;
        AssignWorker(workerId, restHouseId);
    }

    public IEnumerable<WorkerUnit> GetRestingWorkers(Guid lodgeId) =>
        State.Workers.Where(w => w.AssignedBuildingId == lodgeId);

    public double GetSalaryRate() => State.Workers.Count * GameConfig.WorkerSalary;

    public double GetMoneyRate() => State.Map
        .Where(p => !p.IsConstructing && p.Health > 0)
        .Sum(p => (BuildingCatalog.GetById(p.BuildingId)?.MoneyPerSecond ?? 0) * (p.Health / 100.0))
        - GetSalaryRate();

    public double GetEnergyRate() => State.Map
        .Sum(p => BuildingCatalog.GetById(p.BuildingId)?.EnergyPerSecond ?? 0);

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        if (_gameLoop != null) await _gameLoop;
        _cts.Dispose();
    }
}
