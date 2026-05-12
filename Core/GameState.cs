using TinyRts.Entities;
using TinyRts.Gameplay;
using TinyRts.World;

namespace TinyRts.Core;

public enum BuildPlacementMode
{
    None,
    BasicProduction
}

public sealed class GameState
{
    public GameState()
    {
        Map = MapGrid.LoadOrCreateDefault(Path.Combine("Maps", "skirmish.json"), out var loadedData);
        Human = new Player(Faction.Human, GameConfig.HumanStartingOre);
        Orc = new Player(Faction.Orc, GameConfig.OrcStartingOre);
        HumanVision = new TileVisibility[Map.Width, Map.Height];

        if (loadedData is null)
        {
            SeedDefaultScenario();
        }
        else
        {
            SeedFromMapData(loadedData);
        }
    }

    public MapGrid Map { get; }
    public Player Human { get; }
    public Player Orc { get; }
    public List<Unit> Units { get; } = [];
    public List<Building> Buildings { get; } = [];
    public List<ResourceNode> Resources { get; } = [];
    public Dictionary<int, List<Guid>> ControlGroups { get; } = [];
    public Unit? SelectedUnit { get; set; }
    public Building? SelectedBuilding { get; set; }
    public IEnumerable<Unit> SelectedUnits => Units.Where(unit => unit.Selected && unit.IsAlive);
    public Worker? SelectedWorker => SelectedUnits.OfType<Worker>().FirstOrDefault();
    public TileVisibility[,] HumanVision { get; }
    public BuildPlacementMode BuildMode { get; set; } = BuildPlacementMode.None;
    public BuildingType PendingBuildingType { get; set; }
    public string StatusText { get; set; } = "Select a worker. Right click ground/resources. B starts construction.";

    public Player GetPlayer(Faction faction) => faction == Faction.Human ? Human : Orc;

    public Building? FindNearestCompletedMainBuilding(Faction faction, System.Numerics.Vector3 position)
    {
        return Buildings
            .Where(b => b.Faction == faction && b.IsCompleted && (b.Type == BuildingType.HumanTownHall || b.Type == BuildingType.OrcGreatHall))
            .OrderBy(b => System.Numerics.Vector3.DistanceSquared(b.Position, position))
            .FirstOrDefault();
    }

    void SeedDefaultScenario()
    {
        AddResource(new TileCoord(12, 18), 900);
        AddResource(new TileCoord(16, 19), 820);
        AddResource(new TileCoord(45, 43), 900);
        AddResource(new TileCoord(50, 42), 760);

        AddBuilding(Faction.Human, BuildingType.HumanTownHall, new TileCoord(8, 10), underConstruction: false);
        AddBuilding(Faction.Orc, BuildingType.OrcGreatHall, new TileCoord(51, 50), underConstruction: false);

        AddUnit(UnitType.HumanWorker, Map.TileCenter(new TileCoord(12, 13)));
        AddUnit(UnitType.HumanVanguard, Map.TileCenter(new TileCoord(14, 13)));
        AddUnit(UnitType.HumanVanguard, Map.TileCenter(new TileCoord(15, 14)));
        AddUnit(UnitType.HumanRanger, Map.TileCenter(new TileCoord(16, 14)));
        AddUnit(UnitType.OrcWorker, Map.TileCenter(new TileCoord(55, 48)));
        AddUnit(UnitType.OrcBrute, Map.TileCenter(new TileCoord(49, 50)));
        AddUnit(UnitType.OrcBrute, Map.TileCenter(new TileCoord(48, 51)));
        AddUnit(UnitType.OrcRaider, Map.TileCenter(new TileCoord(47, 50)));
    }

    void SeedFromMapData(MapSaveData data)
    {
        foreach (var resource in data.Resources)
        {
            AddResource(new TileCoord(resource.X, resource.Y), resource.Amount);
        }

        foreach (var building in data.Buildings)
        {
            AddBuilding(building.Faction, building.Type, new TileCoord(building.X, building.Y), underConstruction: false);
        }

        if (Buildings.Count == 0)
        {
            AddBuilding(Faction.Human, BuildingType.HumanTownHall, new TileCoord(8, 10), underConstruction: false);
            AddBuilding(Faction.Orc, BuildingType.OrcGreatHall, new TileCoord(51, 50), underConstruction: false);
        }

        AddUnit(UnitType.HumanWorker, Map.TileCenter(new TileCoord(12, 13)));
        AddUnit(UnitType.HumanVanguard, Map.TileCenter(new TileCoord(14, 13)));
        AddUnit(UnitType.HumanRanger, Map.TileCenter(new TileCoord(15, 14)));
        AddUnit(UnitType.OrcWorker, Map.TileCenter(new TileCoord(55, 48)));
        AddUnit(UnitType.OrcBrute, Map.TileCenter(new TileCoord(49, 50)));
        AddUnit(UnitType.OrcRaider, Map.TileCenter(new TileCoord(48, 51)));
    }

    void AddResource(TileCoord coord, int amount)
    {
        var node = new ResourceNode(ResourceType.Ore, coord, Map.TileCenter(coord), amount);
        Resources.Add(node);
        Map.MarkResource(coord, node.Id);
    }

    public Unit AddUnit(UnitType type, System.Numerics.Vector3 position)
    {
        var definition = UnitCatalog.Get(type);
        Unit unit = definition.IsWorker
            ? new Worker(definition.Faction, type, position)
            : new Unit(definition.Faction, type, position);

        Units.Add(unit);
        return unit;
    }

    public Building AddBuilding(Faction faction, BuildingType type, TileCoord anchor, bool underConstruction)
    {
        var spec = BuildingSystem.GetBuildingSpec(faction, type);
        var center = Map.FootprintCenter(anchor, spec.Width, spec.Height);
        var building = new Building(faction, type, anchor, center, spec.Width, spec.Height, underConstruction)
        {
            BuildTime = spec.BuildTime,
            DisplayName = spec.DisplayName,
            MaxHealth = spec.MaxHealth,
            Health = spec.MaxHealth,
            VisionRange = spec.VisionRange,
            RallyPoint = center + new System.Numerics.Vector3(4, 0, 4)
        };

        Buildings.Add(building);
        Map.ReserveBuilding(anchor, spec.Width, spec.Height, building.Id);
        return building;
    }
}
