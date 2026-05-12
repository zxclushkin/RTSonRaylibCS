using System.Numerics;
using Raylib_cs;
using TinyRts.Core;
using TinyRts.Entities;
using TinyRts.Gameplay;
using TinyRts.Rendering;
using TinyRts.World;

namespace TinyRts.Input;

public sealed class SelectionSystem
{
    public Unit? PickUnit(GameState state, CameraController cameraController, Vector2 screenPosition)
    {
        var ray = Raylib.GetScreenToWorldRay(screenPosition, cameraController.Camera);
        Unit? best = null;
        var bestDistance = float.MaxValue;

        foreach (var unit in state.Units)
        {
            if (!unit.IsAlive) continue;
            if (unit.Faction != Faction.Human && !IsVisible(state, unit.Position)) continue;
            var hit = Raylib.GetRayCollisionBox(ray, unit.Bounds);
            if (!hit.Hit || hit.Distance >= bestDistance) continue;
            best = unit;
            bestDistance = hit.Distance;
        }

        return best;
    }

    public ResourceNode? PickResource(GameState state, CameraController cameraController, Vector2 screenPosition)
    {
        var ray = Raylib.GetScreenToWorldRay(screenPosition, cameraController.Camera);
        ResourceNode? best = null;
        var bestDistance = float.MaxValue;

        foreach (var resource in state.Resources.Where(r => r.Amount > 0))
        {
            var hit = Raylib.GetRayCollisionBox(ray, resource.Bounds);
            if (!hit.Hit || hit.Distance >= bestDistance) continue;
            best = resource;
            bestDistance = hit.Distance;
        }

        return best;
    }

    public Building? PickBuilding(GameState state, CameraController cameraController, Vector2 screenPosition)
    {
        var ray = Raylib.GetScreenToWorldRay(screenPosition, cameraController.Camera);
        Building? best = null;
        var bestDistance = float.MaxValue;

        foreach (var building in state.Buildings)
        {
            if (building.Health <= 0) continue;
            if (building.Faction != Faction.Human && !IsVisible(state, building.Position)) continue;
            var hit = Raylib.GetRayCollisionBox(ray, building.Bounds);
            if (!hit.Hit || hit.Distance >= bestDistance) continue;
            best = building;
            bestDistance = hit.Distance;
        }

        return best;
    }

    public void Select(GameState state, Unit? unit)
    {
        ClearSelection(state);
        if (unit is not null)
        {
            unit.Selected = true;
            state.SelectedUnit = unit;
        }
    }

    public void Select(GameState state, Building? building)
    {
        ClearSelection(state);
        if (building is not null)
        {
            building.Selected = true;
            state.SelectedBuilding = building;
        }
    }

    public void SelectUnits(GameState state, IEnumerable<Unit> units)
    {
        ClearSelection(state);

        var selectableUnits = units
            .Where(unit => unit is { IsAlive: true, Faction: Faction.Human })
            .ToList();

        foreach (var unit in selectableUnits)
        {
            unit.Selected = true;
        }

        state.SelectedUnit = selectableUnits.FirstOrDefault();
    }

    public void ClearSelection(GameState state)
    {
        foreach (var existing in state.Units)
        {
            existing.Selected = false;
        }

        foreach (var building in state.Buildings)
        {
            building.Selected = false;
        }

        state.SelectedUnit = null;
        state.SelectedBuilding = null;
    }

    public List<Unit> GetUnitsInScreenRectangle(GameState state, CameraController cameraController, Rectangle rectangle)
    {
        var units = new List<Unit>();
        foreach (var unit in state.Units)
        {
            if (unit is not { IsAlive: true, Faction: Faction.Human }) continue;
            if (!IsVisible(state, unit.Position)) continue;

            var screenPosition = Raylib.GetWorldToScreen(unit.Position + new Vector3(0, 1.1f, 0), cameraController.Camera);
            if (Raylib.CheckCollisionPointRec(screenPosition, rectangle))
            {
                units.Add(unit);
            }
        }

        return units;
    }

    static bool IsVisible(GameState state, System.Numerics.Vector3 position)
    {
        var tile = state.Map.WorldToTile(position);
        return state.HumanVision[tile.X, tile.Y] == TileVisibility.Visible;
    }
}
