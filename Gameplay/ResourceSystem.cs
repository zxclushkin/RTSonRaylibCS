using System.Numerics;
using TinyRts.Core;
using TinyRts.Entities;

namespace TinyRts.Gameplay;

public static class ResourceSystem
{
    public static void UpdateGathering(GameState state, Worker worker, CommandSystem commandSystem, float dt)
    {
        var resource = worker.ResourceTarget;
        if (resource is null || resource.Amount <= 0)
        {
            if (TryRetargetNearestResource(state, worker, commandSystem))
            {
                return;
            }

            worker.PreferredResource = null;
            worker.CommandType = UnitCommandType.None;
            worker.State = UnitState.Idle;
            return;
        }

        if (worker.CarriedOre >= GameConfig.WorkerCarryCapacity)
        {
            ReturnToNearestDropoff(state, worker, commandSystem, dt);
            return;
        }

        worker.State = UnitState.Gathering;
        if (Vector3.Distance(worker.Position, resource.Position) > resource.Radius + 0.95f)
        {
            if (worker.Path.Count == 0 || worker.PathIndex >= worker.Path.Count)
            {
                commandSystem.SetPathToTile(state, worker, state.Map.FindNearestWalkableAdjacent(resource.Tile, 1, 1, radius: 3));
            }

            CommandSystem.MoveAlongPath(worker, dt);
            return;
        }

        worker.GatherTimer -= dt;
        if (worker.GatherTimer > 0) return;

        worker.GatherTimer = GameConfig.WorkerGatherSeconds;
        var amount = Math.Min(GameConfig.WorkerCarryCapacity - worker.CarriedOre, Math.Min(5, resource.Amount));
        resource.Amount -= amount;
        worker.CarriedOre += amount;
    }

    static void ReturnToNearestDropoff(GameState state, Worker worker, CommandSystem commandSystem, float dt)
    {
        var dropoff = state.FindNearestCompletedMainBuilding(worker.Faction, worker.Position);
        if (dropoff is null)
        {
            worker.CommandType = UnitCommandType.None;
            worker.State = UnitState.Idle;
            return;
        }

        worker.State = UnitState.ReturningResource;
        if (!IsAtDropoff(worker, dropoff))
        {
            if (worker.Path.Count == 0 || worker.PathIndex >= worker.Path.Count)
            {
                var targetTile = state.Map.FindNearestWalkableAdjacent(dropoff.AnchorTile, dropoff.FootprintWidth, dropoff.FootprintHeight);
                commandSystem.SetPathToTile(state, worker, targetTile);
            }

            CommandSystem.MoveAlongPath(worker, dt);
            return;
        }

        state.GetPlayer(worker.Faction).Ore += worker.CarriedOre;
        worker.CarriedOre = 0;
        worker.Path.Clear();
        worker.PathIndex = 0;

        if (worker.ResourceTarget is { Amount: > 0 } resource)
        {
            worker.State = UnitState.Gathering;
            worker.MoveTarget = resource.Position;
        }
        else
        {
            worker.PreferredResource = null;
        }
    }

    static bool TryRetargetNearestResource(GameState state, Worker worker, CommandSystem commandSystem)
    {
        var resource = state.Resources
            .Where(candidate => candidate.Amount > 0)
            .OrderBy(candidate => Vector3.DistanceSquared(candidate.Position, worker.Position))
            .FirstOrDefault();

        if (resource is null) return false;

        commandSystem.IssueGather(state, worker, resource);
        return true;
    }

    static bool IsAtDropoff(Worker worker, Building dropoff)
    {
        var bounds = dropoff.Bounds;
        var closestX = Math.Clamp(worker.Position.X, bounds.Min.X, bounds.Max.X);
        var closestZ = Math.Clamp(worker.Position.Z, bounds.Min.Z, bounds.Max.Z);
        var dx = worker.Position.X - closestX;
        var dz = worker.Position.Z - closestZ;
        var handoffRange = worker.Radius + 0.85f;

        return dx * dx + dz * dz <= handoffRange * handoffRange;
    }
}
