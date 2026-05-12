using TinyRts.Core;
using TinyRts.Entities;
using TinyRts.World;

namespace TinyRts.Gameplay;

public sealed class OrcAiSystem
{
    float thinkTimer;
    float attackTimer = 22.0f;

    public void Update(GameState state, CommandSystem commandSystem, float dt)
    {
        thinkTimer -= dt;
        attackTimer -= dt;

        if (thinkTimer <= 0)
        {
            thinkTimer = GameConfig.AiThinkInterval;
            ThinkEconomy(state, commandSystem);
            ThinkProduction(state);
        }

        if (attackTimer <= 0)
        {
            attackTimer = 32.0f;
            LaunchAttackWave(state, commandSystem);
        }
    }

    void ThinkEconomy(GameState state, CommandSystem commandSystem)
    {
        var worker = state.Units.OfType<Worker>().FirstOrDefault(w => w.Faction == Faction.Orc && w.IsAlive);
        if (worker is null) return;

        var hall = state.Buildings.FirstOrDefault(b => b.Faction == Faction.Orc && b.Type == BuildingType.OrcGreatHall && b.IsCompleted);
        if (hall is null) return;

        var hasWarHut = state.Buildings.Any(b => b.Faction == Faction.Orc && b.Type == BuildingType.OrcWarHut);
        if (!hasWarHut && state.Orc.Ore >= BuildingSystem.GetBuildingSpec(Faction.Orc, BuildingType.OrcWarHut).OreCost && worker.CommandType != UnitCommandType.Build)
        {
            if (TryPlaceOrcWarHut(state, commandSystem, worker, hall))
            {
                return;
            }
        }

        if (worker.CommandType is UnitCommandType.None or UnitCommandType.Move)
        {
            var resource = state.Resources
                .Where(r => r.Amount > 0)
                .OrderBy(r => System.Numerics.Vector3.DistanceSquared(r.Position, worker.Position))
                .FirstOrDefault();

            if (resource is not null)
            {
                commandSystem.IssueGather(state, worker, resource);
            }
        }
    }

    void ThinkProduction(GameState state)
    {
        foreach (var building in state.Buildings.Where(b => b.Faction == Faction.Orc && b.IsCompleted))
        {
            var spec = BuildingSystem.GetBuildingSpec(building.Faction, building.Type);
            if (spec.TrainsUnits.Count == 0) continue;
            if (building.CurrentTraining is not null || building.ProductionQueue.Count >= 2) continue;

            var trainType = spec.TrainsUnits[Math.Min(building.ProductionQueue.Count, spec.TrainsUnits.Count - 1)];
            var unit = UnitCatalog.Get(trainType);
            if (state.Orc.Ore >= unit.OreCost)
            {
                BuildingSystem.TryQueueTraining(state, building, trainType, reportStatus: false);
            }
        }
    }

    bool TryPlaceOrcWarHut(GameState state, CommandSystem commandSystem, Worker worker, Building hall)
    {
        var candidates = new[]
        {
            new TileCoord(hall.AnchorTile.X - 5, hall.AnchorTile.Y),
            new TileCoord(hall.AnchorTile.X, hall.AnchorTile.Y - 5),
            new TileCoord(hall.AnchorTile.X + 5, hall.AnchorTile.Y + 1),
            new TileCoord(hall.AnchorTile.X + 1, hall.AnchorTile.Y + 5)
        };

        foreach (var candidate in candidates)
        {
            if (!BuildingSystem.TryBeginConstruction(state, worker, BuildingType.OrcWarHut, candidate, out var building, reportStatus: false) || building is null)
            {
                continue;
            }

            commandSystem.IssueBuild(state, worker, building);
            state.StatusText = "Orc AI started a War Hut.";
            return true;
        }

        return false;
    }

    void LaunchAttackWave(GameState state, CommandSystem commandSystem)
    {
        var target = state.Buildings.FirstOrDefault(b => b.Faction == Faction.Human && b.IsCompleted)
            ?? (Entity?)state.Units.FirstOrDefault(u => u.Faction == Faction.Human && u.IsAlive);

        if (target is null) return;

        var attackers = state.Units
            .Where(u => u.Faction == Faction.Orc && !UnitCatalog.Get(u.Type).IsWorker && u.IsAlive)
            .Take(5)
            .ToList();

        if (attackers.Count < 2) return;

        foreach (var attacker in attackers)
        {
            if (target is Building building)
            {
                commandSystem.IssueAttack(state, attacker, building);
            }
            else if (target is Unit unit)
            {
                commandSystem.IssueAttack(state, attacker, unit);
            }
        }

        state.StatusText = "Orc raiders are advancing.";
    }
}
