using TinyRts.Core;
using TinyRts.Entities;

namespace TinyRts.Gameplay;

public sealed class CombatSystem
{
    public void Update(GameState state, CommandSystem commandSystem)
    {
        foreach (var unit in state.Units.Where(u => u.IsAlive))
        {
            if (unit.CommandType is UnitCommandType.Gather or UnitCommandType.Build or UnitCommandType.Attack)
            {
                continue;
            }

            var targetUnit = state.Units
                .Where(other => other.IsAlive && other.Faction != unit.Faction)
                .OrderBy(other => System.Numerics.Vector3.DistanceSquared(other.Position, unit.Position))
                .FirstOrDefault(other => System.Numerics.Vector3.Distance(other.Position, unit.Position) <= unit.AttackRange + 2.5f);

            if (targetUnit is not null)
            {
                commandSystem.IssueAttack(state, unit, targetUnit);
                continue;
            }

            var targetBuilding = state.Buildings
                .Where(building => building.Health > 0 && building.Faction != unit.Faction)
                .OrderBy(building => System.Numerics.Vector3.DistanceSquared(building.Position, unit.Position))
                .FirstOrDefault(building => System.Numerics.Vector3.Distance(building.Position, unit.Position) <= unit.AttackRange + MathF.Max(building.Size.X, building.Size.Z) * 0.5f + 1.0f);

            if (targetBuilding is not null)
            {
                commandSystem.IssueAttack(state, unit, targetBuilding);
            }
        }
    }

    public void CleanupDead(GameState state)
    {
        for (var i = state.Buildings.Count - 1; i >= 0; i--)
        {
            var building = state.Buildings[i];
            if (building.Health > 0) continue;

            state.Map.ReleaseBuilding(building.Id);
            if (state.SelectedBuilding == building)
            {
                state.SelectedBuilding = null;
            }

            state.Buildings.RemoveAt(i);
        }

        for (var i = state.Units.Count - 1; i >= 0; i--)
        {
            var unit = state.Units[i];
            if (unit.Health > 0) continue;

            if (state.SelectedUnit == unit)
            {
                state.SelectedUnit = null;
            }

            state.Units.RemoveAt(i);
        }
    }
}
