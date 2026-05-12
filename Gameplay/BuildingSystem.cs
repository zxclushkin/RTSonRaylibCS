using TinyRts.Core;
using TinyRts.Entities;
using TinyRts.World;

namespace TinyRts.Gameplay;

public static class BuildingSystem
{
    public sealed record BuildingDefinition(
        int Width,
        int Height,
        float BuildTime,
        string DisplayName,
        int OreCost,
        float MaxHealth,
        int VisionRange,
        IReadOnlyList<UnitType> TrainsUnits);

    public static BuildingDefinition GetBuildingSpec(Faction faction, BuildingType type)
    {
        return type switch
        {
            BuildingType.HumanTownHall => new BuildingDefinition(4, 4, 0, FactionCatalog.Get(Faction.Human).MainBuildingName, 0, 720, 10, [UnitType.HumanWorker]),
            BuildingType.OrcGreatHall => new BuildingDefinition(4, 4, 0, FactionCatalog.Get(Faction.Orc).MainBuildingName, 0, 780, 10, [UnitType.OrcWorker]),
            BuildingType.HumanBarracks => new BuildingDefinition(3, 3, 7.0f, FactionCatalog.Get(Faction.Human).ProductionBuildingName, 120, 480, 8, [UnitType.HumanVanguard, UnitType.HumanRanger]),
            BuildingType.OrcWarHut => new BuildingDefinition(3, 3, 7.0f, FactionCatalog.Get(Faction.Orc).ProductionBuildingName, 120, 520, 8, [UnitType.OrcBrute, UnitType.OrcRaider]),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static bool TryBeginConstruction(GameState state, Worker worker, BuildingType type, TileCoord anchor, out Building? building, bool reportStatus = true)
    {
        var spec = GetBuildingSpec(worker.Faction, type);
        building = null;

        if (!state.Map.CanPlaceBuilding(anchor, spec.Width, spec.Height))
        {
            if (reportStatus) state.StatusText = "Cannot build there.";
            return false;
        }

        var player = state.GetPlayer(worker.Faction);
        if (player.Ore < spec.OreCost)
        {
            if (reportStatus) state.StatusText = $"Need {spec.OreCost} ore.";
            return false;
        }

        player.Ore -= spec.OreCost;
        building = state.AddBuilding(worker.Faction, type, anchor, underConstruction: true);
        if (reportStatus) state.StatusText = $"{spec.DisplayName} foundation placed.";
        return true;
    }

    public static bool TryQueueTraining(GameState state, Building building, UnitType? requestedType = null, bool reportStatus = true)
    {
        if (!building.IsCompleted)
        {
            if (reportStatus) state.StatusText = "That structure is still under construction.";
            return false;
        }

        var spec = GetBuildingSpec(building.Faction, building.Type);
        if (spec.TrainsUnits.Count == 0)
        {
            if (reportStatus) state.StatusText = "This structure has no train command yet.";
            return false;
        }

        var type = requestedType ?? spec.TrainsUnits[0];
        if (!spec.TrainsUnits.Contains(type))
        {
            if (reportStatus) state.StatusText = "This structure cannot train that unit.";
            return false;
        }

        var unit = UnitCatalog.Get(type);
        var player = state.GetPlayer(building.Faction);
        if (player.Ore < unit.OreCost)
        {
            if (reportStatus) state.StatusText = $"Need {unit.OreCost} ore.";
            return false;
        }

        if (building.ProductionQueue.Count >= 5)
        {
            if (reportStatus) state.StatusText = "Training queue is full.";
            return false;
        }

        player.Ore -= unit.OreCost;
        building.ProductionQueue.Enqueue(unit.Type);
        if (reportStatus) state.StatusText = $"{unit.DisplayName} queued.";
        return true;
    }

    public static void UpdateProduction(GameState state, CommandSystem commandSystem, float dt)
    {
        foreach (var building in state.Buildings.Where(b => b.IsCompleted))
        {
            if (building.CurrentTraining is null && building.ProductionQueue.Count > 0)
            {
                building.CurrentTraining = building.ProductionQueue.Dequeue();
                building.TrainingProgress = 0;
            }

            if (building.CurrentTraining is null) continue;
            var unitDefinition = UnitCatalog.Get(building.CurrentTraining.Value);
            building.TrainingProgress += dt;
            if (building.TrainingProgress < unitDefinition.TrainTime) continue;

            var spawnTile = state.Map.FindNearestWalkableAdjacent(building.AnchorTile, building.FootprintWidth, building.FootprintHeight, radius: 5);
            var unit = state.AddUnit(unitDefinition.Type, state.Map.TileCenter(spawnTile));
            commandSystem.IssueMove(state, unit, building.RallyPoint);
            building.CurrentTraining = null;
            building.TrainingProgress = 0;
        }
    }

    public static void UpdateBuilder(Worker worker, float dt)
    {
        var building = worker.BuildingTarget;
        if (building is null || building.IsCompleted)
        {
            worker.CommandType = UnitCommandType.None;
            worker.State = UnitState.Idle;
            return;
        }

        if (worker.Path.Count > 0 && worker.PathIndex < worker.Path.Count && !CommandSystem.MoveAlongPath(worker, dt))
        {
            return;
        }

        if (System.Numerics.Vector3.Distance(worker.Position, building.Position) <= MathF.Max(building.FootprintWidth, building.FootprintHeight) * 1.5f)
        {
            building.BuildProgress += dt * GameConfig.WorkerBuildRate;
            if (building.BuildProgress >= building.BuildTime)
            {
                building.BuildProgress = building.BuildTime;
                building.IsUnderConstruction = false;
                worker.CommandType = UnitCommandType.None;
                worker.State = UnitState.Idle;
            }
        }
    }
}
