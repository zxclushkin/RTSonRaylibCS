using System.Numerics;
using TinyRts.Core;
using TinyRts.Entities;
using TinyRts.World;

namespace TinyRts.Gameplay;

public sealed class CommandSystem
{
    readonly Pathfinding pathfinding = new();

    public void IssueMove(GameState state, Unit unit, Vector3 target, bool queue = false, bool attackMove = false)
    {
        if (queue)
        {
            unit.CommandQueue.Enqueue(new QueuedUnitCommand(UnitCommandType.Move, TargetPosition: target, AttackMove: attackMove));
            return;
        }

        // Unit commands are explicit objects-in-waiting: this can become a queued command system later.
        unit.CommandType = UnitCommandType.Move;
        unit.State = UnitState.Moving;
        unit.AttackMove = attackMove;
        SetPathToWorld(state, unit, target);
        unit.ResourceTarget = null;
        unit.BuildingTarget = null;
        unit.AttackUnitTarget = null;
        unit.AttackBuildingTarget = null;
    }

    public void IssueGather(GameState state, Worker worker, ResourceNode resource, bool queue = false)
    {
        if (queue)
        {
            worker.CommandQueue.Enqueue(new QueuedUnitCommand(UnitCommandType.Gather, ResourceTarget: resource));
            return;
        }

        // Gather command stores both target resource and movement target for future pathfinding replacement.
        worker.CommandType = UnitCommandType.Gather;
        worker.State = UnitState.Gathering;
        worker.AttackMove = false;
        worker.ResourceTarget = resource;
        worker.PreferredResource = resource;
        worker.BuildingTarget = null;
        worker.AttackUnitTarget = null;
        worker.AttackBuildingTarget = null;
        SetPathToTile(state, worker, state.Map.FindNearestWalkableAdjacent(resource.Tile, 1, 1, radius: 3));
    }

    public void IssueBuild(GameState state, Worker worker, Building building, bool queue = false)
    {
        if (queue)
        {
            worker.CommandQueue.Enqueue(new QueuedUnitCommand(UnitCommandType.Build, BuildingTarget: building));
            return;
        }

        // Build command is intentionally separate from construction progress so AI can issue it too.
        worker.CommandType = UnitCommandType.Build;
        worker.State = UnitState.Building;
        worker.AttackMove = false;
        worker.BuildingTarget = building;
        worker.ResourceTarget = null;
        worker.AttackUnitTarget = null;
        worker.AttackBuildingTarget = null;
        SetPathToTile(state, worker, state.Map.FindNearestWalkableAdjacent(building.AnchorTile, building.FootprintWidth, building.FootprintHeight));
    }

    public void IssueAttack(GameState state, Unit unit, Unit target, bool queue = false)
    {
        if (queue)
        {
            unit.CommandQueue.Enqueue(new QueuedUnitCommand(UnitCommandType.Attack, AttackUnitTarget: target));
            return;
        }

        unit.CommandType = UnitCommandType.Attack;
        unit.State = UnitState.Attacking;
        unit.AttackMove = false;
        unit.AttackUnitTarget = target;
        unit.AttackBuildingTarget = null;
        unit.ResourceTarget = null;
        unit.BuildingTarget = null;
        SetPathToWorld(state, unit, target.Position);
    }

    public void IssueAttack(GameState state, Unit unit, Building target, bool queue = false)
    {
        if (queue)
        {
            unit.CommandQueue.Enqueue(new QueuedUnitCommand(UnitCommandType.Attack, BuildingTarget: target));
            return;
        }

        unit.CommandType = UnitCommandType.Attack;
        unit.State = UnitState.Attacking;
        unit.AttackMove = false;
        unit.AttackUnitTarget = null;
        unit.AttackBuildingTarget = target;
        unit.ResourceTarget = null;
        unit.BuildingTarget = null;
        SetPathToTile(state, unit, state.Map.FindNearestWalkableAdjacent(target.AnchorTile, target.FootprintWidth, target.FootprintHeight));
    }

    public void Update(GameState state, float dt)
    {
        foreach (var unit in state.Units)
        {
            if (!unit.IsAlive) continue;

            if (unit is Worker worker)
            {
                UpdateWorker(state, worker, dt);
            }
            else
            {
                UpdateCombatUnit(state, unit, dt);
            }
        }

        ResolveUnitSeparation(state);
    }

    void UpdateWorker(GameState state, Worker worker, float dt)
    {
        switch (worker.CommandType)
        {
            case UnitCommandType.Move:
                if (worker.AttackMove && TryAcquireAttackMoveTarget(state, worker))
                {
                    break;
                }

                if (MoveAlongPath(worker, dt))
                {
                    CompleteCommandOrContinue(state, worker);
                }
                break;

            case UnitCommandType.Gather:
                ResourceSystem.UpdateGathering(state, worker, this, dt);
                break;

            case UnitCommandType.Build:
                BuildingSystem.UpdateBuilder(worker, dt);
                if (worker.CommandType == UnitCommandType.None)
                {
                    CompleteCommandOrContinue(state, worker);
                }
                break;

            case UnitCommandType.Attack:
                UpdateAttackCommand(state, worker, dt);
                break;

            case UnitCommandType.None:
            default:
                if (!TryStartNextCommand(state, worker) && !TryResumePreferredGathering(state, worker))
                {
                    worker.State = UnitState.Idle;
                }
                break;
        }
    }

    void UpdateCombatUnit(GameState state, Unit unit, float dt)
    {
        if (unit.CommandType == UnitCommandType.Attack)
        {
            UpdateAttackCommand(state, unit, dt);
            return;
        }

        if (unit.CommandType == UnitCommandType.Move && unit.AttackMove && TryAcquireAttackMoveTarget(state, unit))
        {
            return;
        }

        if (unit.CommandType == UnitCommandType.Move && MoveAlongPath(unit, dt))
        {
            CompleteCommandOrContinue(state, unit);
            return;
        }

        if (unit.CommandType == UnitCommandType.None)
        {
            TryStartNextCommand(state, unit);
        }
    }

    void UpdateAttackCommand(GameState state, Unit unit, float dt)
    {
        var targetPosition = unit.AttackUnitTarget is { IsAlive: true } enemyUnit
            ? enemyUnit.Position
            : unit.AttackBuildingTarget is { Health: > 0 } enemyBuilding
                ? enemyBuilding.Position
                : (Vector3?)null;

        if (targetPosition is null)
        {
            CompleteCommandOrContinue(state, unit);
            return;
        }

        unit.AttackCooldown -= dt;
        var engagementRange = unit.AttackRange;
        if (unit.AttackBuildingTarget is { Health: > 0 } buildingTarget)
        {
            engagementRange += MathF.Max(buildingTarget.Size.X, buildingTarget.Size.Z) * 0.5f;
        }

        var distance = Vector3.Distance(unit.Position, targetPosition.Value);
        if (distance > engagementRange)
        {
            unit.State = UnitState.Moving;
            if (unit.Path.Count == 0 || Vector3.Distance(unit.MoveTarget, targetPosition.Value) > 2.0f)
            {
                SetPathToWorld(state, unit, targetPosition.Value);
            }

            MoveAlongPath(unit, dt);
            return;
        }

        unit.State = UnitState.Attacking;
        unit.Path.Clear();
        if (unit.AttackCooldown > 0) return;

        if (unit.AttackUnitTarget is { IsAlive: true } targetUnit)
        {
            targetUnit.Health -= unit.Damage;
        }
        else if (unit.AttackBuildingTarget is { Health: > 0 } targetBuilding)
        {
            targetBuilding.Health -= unit.Damage;
        }

        unit.AttackCooldown = unit.AttackInterval;
    }

    public bool SetPathToWorld(GameState state, Unit unit, Vector3 target)
    {
        var tile = state.Map.WorldToTile(target);
        return SetPathToTile(state, unit, tile);
    }

    public bool SetPathToTile(GameState state, Unit unit, TileCoord targetTile)
    {
        var start = state.Map.WorldToTile(unit.Position);
        var pathTiles = pathfinding.FindPath(state.Map, start, targetTile);
        unit.Path.Clear();
        unit.PathIndex = 0;

        if (pathTiles.Count == 0)
        {
            unit.MoveTarget = unit.Position;
            return false;
        }

        foreach (var tile in pathTiles.Skip(1))
        {
            unit.Path.Add(state.Map.TileCenter(tile));
        }

        unit.MoveTarget = unit.Path.Count > 0 ? unit.Path[^1] : state.Map.TileCenter(targetTile);
        return true;
    }

    bool TryStartNextCommand(GameState state, Unit unit)
    {
        while (unit.CommandQueue.Count > 0)
        {
            var command = unit.CommandQueue.Dequeue();
            switch (command.Type)
            {
                case UnitCommandType.Move when command.TargetPosition is { } target:
                    IssueMove(state, unit, target, attackMove: command.AttackMove);
                    return true;

                case UnitCommandType.Gather when unit is Worker worker && command.ResourceTarget is { Amount: > 0 } resource:
                    IssueGather(state, worker, resource);
                    return true;

                case UnitCommandType.Build when unit is Worker worker && command.BuildingTarget is { IsCompleted: false } building:
                    IssueBuild(state, worker, building);
                    return true;

                case UnitCommandType.Attack when command.AttackUnitTarget is { IsAlive: true } targetUnit:
                    IssueAttack(state, unit, targetUnit);
                    return true;

                case UnitCommandType.Attack when command.BuildingTarget is { Health: > 0 } targetBuilding:
                    IssueAttack(state, unit, targetBuilding);
                    return true;
            }
        }

        return false;
    }

    void CompleteCommandOrContinue(GameState state, Unit unit)
    {
        unit.CommandType = UnitCommandType.None;
        unit.State = UnitState.Idle;
        unit.AttackMove = false;
        unit.Path.Clear();
        unit.PathIndex = 0;
        if (!TryStartNextCommand(state, unit) && unit is Worker worker)
        {
            TryResumePreferredGathering(state, worker);
        }
    }

    bool TryResumePreferredGathering(GameState state, Worker worker)
    {
        if (worker.PreferredResource is not { Amount: > 0 } resource) return false;

        IssueGather(state, worker, resource);
        return true;
    }

    bool TryAcquireAttackMoveTarget(GameState state, Unit unit)
    {
        var searchRange = unit.VisionRange * state.Map.TileSize;
        var targetUnit = state.Units
            .Where(other => other.IsAlive && other.Faction != unit.Faction)
            .OrderBy(other => Vector3.DistanceSquared(other.Position, unit.Position))
            .FirstOrDefault(other => Vector3.Distance(other.Position, unit.Position) <= searchRange);

        if (targetUnit is not null)
        {
            IssueAttack(state, unit, targetUnit);
            return true;
        }

        var targetBuilding = state.Buildings
            .Where(building => building.Health > 0 && building.Faction != unit.Faction)
            .OrderBy(building => Vector3.DistanceSquared(building.Position, unit.Position))
            .FirstOrDefault(building => Vector3.Distance(building.Position, unit.Position) <= searchRange + MathF.Max(building.Size.X, building.Size.Z) * 0.5f);

        if (targetBuilding is null) return false;

        IssueAttack(state, unit, targetBuilding);
        return true;
    }

    static void ResolveUnitSeparation(GameState state)
    {
        for (var i = 0; i < state.Units.Count; i++)
        {
            var a = state.Units[i];
            if (!a.IsAlive) continue;

            for (var j = i + 1; j < state.Units.Count; j++)
            {
                var b = state.Units[j];
                if (!b.IsAlive) continue;

                var delta = a.Position - b.Position;
                delta.Y = 0;
                var minDistance = a.Radius + b.Radius + 0.08f;
                var distanceSquared = delta.LengthSquared();
                if (distanceSquared <= 0.0001f || distanceSquared >= minDistance * minDistance) continue;

                var distance = MathF.Sqrt(distanceSquared);
                var push = Vector3.Normalize(delta) * ((minDistance - distance) * 0.5f);
                TryPushUnit(state, a, push);
                TryPushUnit(state, b, -push);
            }
        }
    }

    static void TryPushUnit(GameState state, Unit unit, Vector3 push)
    {
        var candidate = unit.Position + push;
        if (state.Map.CanWalk(state.Map.WorldToTile(candidate)))
        {
            unit.Position = candidate;
        }
    }

    public static bool MoveAlongPath(Unit unit, float dt)
    {
        if (unit.PathIndex >= unit.Path.Count)
        {
            return true;
        }

        var target = unit.Path[unit.PathIndex];
        if (MoveToward(unit, target, dt))
        {
            unit.PathIndex++;
        }

        return unit.PathIndex >= unit.Path.Count;
    }

    public static bool MoveToward(Unit unit, Vector3 target, float dt)
    {
        var delta = target - unit.Position;
        delta.Y = 0;
        if (delta.LengthSquared() <= 0.05f)
        {
            unit.Position = new Vector3(target.X, unit.Position.Y, target.Z);
            return true;
        }

        var step = Vector3.Normalize(delta) * unit.Speed * dt;
        if (step.LengthSquared() > delta.LengthSquared())
        {
            step = delta;
        }

        unit.Position += step;
        return false;
    }
}
