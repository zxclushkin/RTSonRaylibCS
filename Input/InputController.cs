using System.Numerics;
using Raylib_cs;
using TinyRts.Core;
using TinyRts.Entities;
using TinyRts.Gameplay;
using TinyRts.Rendering;
using TinyRts.UI;

namespace TinyRts.Input;

public sealed class InputController
{
    const float SelectionDragThreshold = 6.0f;

    readonly SelectionSystem selectionSystem;
    readonly CommandSystem commandSystem;
    Vector2 selectionDragStart;
    bool selectionDragActive;
    bool attackMoveArmed;

    public InputController(SelectionSystem selectionSystem, CommandSystem commandSystem)
    {
        this.selectionSystem = selectionSystem;
        this.commandSystem = commandSystem;
    }

    public Rectangle? SelectionRectangle { get; private set; }

    public void Update(GameState state, CameraController cameraController)
    {
        HandleHotkeys(state);

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            CancelSelectionDrag();
            if (state.BuildMode != BuildPlacementMode.None)
            {
                state.BuildMode = BuildPlacementMode.None;
                state.StatusText = "Build mode cancelled.";
            }
            else
            {
                selectionSystem.ClearSelection(state);
                state.StatusText = "Selection cleared.";
            }
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            var mouse = Raylib.GetMousePosition();
            var action = CommandPanel.HitTest(state, mouse);
            if (action is not null)
            {
                CancelSelectionDrag();
                ExecuteCommandAction(state, action.Value);
                return;
            }

            if (TryHandleMinimapClick(state, cameraController, mouse))
            {
                CancelSelectionDrag();
                return;
            }

            if (attackMoveArmed)
            {
                CancelSelectionDrag();
                HandleAttackMoveClick(state, cameraController, mouse);
            }
            else if (state.BuildMode != BuildPlacementMode.None)
            {
                HandleBuildPlacement(state, cameraController, mouse);
            }
            else
            {
                BeginSelectionDrag(mouse);
            }
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Left) && selectionDragActive)
        {
            var rectangle = MakeScreenRectangle(selectionDragStart, Raylib.GetMousePosition());
            SelectionRectangle = IsDragSelection(rectangle) ? rectangle : null;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left) && selectionDragActive)
        {
            var mouse = Raylib.GetMousePosition();
            var rectangle = MakeScreenRectangle(selectionDragStart, mouse);
            CancelSelectionDrag();

            if (IsDragSelection(rectangle))
            {
                HandleDragSelection(state, cameraController, rectangle);
            }
            else
            {
                HandleSelectionClick(state, cameraController, mouse);
            }
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Right))
        {
            CancelSelectionDrag();
            HandleRightClick(state, cameraController);
        }
    }

    void HandleHotkeys(GameState state)
    {
        HandleControlGroups(state);

        if (Raylib.IsKeyPressed(KeyboardKey.B))
        {
            ExecuteCommandAction(state, CommandAction.BuildProduction);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.A) && state.SelectedUnits.Any())
        {
            attackMoveArmed = true;
            state.StatusText = "Attack-move armed: left click a destination.";
        }

        if (Raylib.IsKeyPressed(KeyboardKey.T))
        {
            ExecuteCommandAction(state, CommandAction.TrainPrimary);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Y))
        {
            ExecuteCommandAction(state, CommandAction.TrainSecondary);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.S))
        {
            ExecuteCommandAction(state, CommandAction.Stop);
        }
    }

    void HandleControlGroups(GameState state)
    {
        for (var slot = 1; slot <= 9; slot++)
        {
            if (!Raylib.IsKeyPressed((KeyboardKey)(48 + slot))) continue;

            if (IsControlDown())
            {
                var selectedIds = state.SelectedUnits.Select(unit => unit.Id).ToList();
                if (selectedIds.Count == 0) return;
                state.ControlGroups[slot] = selectedIds;
                state.StatusText = $"Control group {slot} saved ({selectedIds.Count} units).";
                return;
            }

            if (!state.ControlGroups.TryGetValue(slot, out var ids)) return;
            var units = ids
                .Select(id => state.Units.FirstOrDefault(unit => unit.Id == id && unit.IsAlive && unit.Faction == Faction.Human))
                .Where(unit => unit is not null)
                .Cast<Unit>()
                .ToList();

            selectionSystem.SelectUnits(state, units);
            state.StatusText = units.Count == 0 ? $"Control group {slot} is empty." : $"Control group {slot} selected.";
            return;
        }
    }

    void ExecuteCommandAction(GameState state, CommandAction action)
    {
        switch (action)
        {
            case CommandAction.BuildProduction when state.SelectedWorker is not null:
            {
                var faction = FactionCatalog.Get(state.SelectedWorker.Faction);
                state.PendingBuildingType = faction.BasicProductionBuilding;
                state.BuildMode = BuildPlacementMode.BasicProduction;
                state.StatusText = $"Place {faction.ProductionBuildingName}: left click a valid tile.";
                break;
            }

            case CommandAction.TrainPrimary when state.SelectedBuilding is not null:
                QueueTrainingByIndex(state, state.SelectedBuilding, 0);
                break;

            case CommandAction.TrainSecondary when state.SelectedBuilding is not null:
                QueueTrainingByIndex(state, state.SelectedBuilding, 1);
                break;

            case CommandAction.Stop:
                attackMoveArmed = false;
                StopSelection(state);
                break;

            case CommandAction.Cancel:
                attackMoveArmed = false;
                state.BuildMode = BuildPlacementMode.None;
                state.StatusText = "Build mode cancelled.";
                break;
        }
    }

    static void QueueTrainingByIndex(GameState state, Building building, int index)
    {
        var spec = BuildingSystem.GetBuildingSpec(building.Faction, building.Type);
        if (index >= spec.TrainsUnits.Count)
        {
            state.StatusText = "No unit in that production slot.";
            return;
        }

        BuildingSystem.TryQueueTraining(state, building, spec.TrainsUnits[index]);
    }

    static void StopSelection(GameState state)
    {
        var selectedUnits = state.SelectedUnits.ToList();
        if (selectedUnits.Count > 0)
        {
            foreach (var unit in selectedUnits)
            {
                ClearUnitOrders(unit);
            }

            state.StatusText = selectedUnits.Count == 1 ? "Unit stopped." : $"{selectedUnits.Count} units stopped.";
            return;
        }

        if (state.SelectedBuilding is not null)
        {
            state.SelectedBuilding.ProductionQueue.Clear();
            state.SelectedBuilding.CurrentTraining = null;
            state.SelectedBuilding.TrainingProgress = 0;
            state.StatusText = "Training queue cleared.";
        }
    }

    static void ClearUnitOrders(Unit unit)
    {
        unit.CommandType = UnitCommandType.None;
        unit.State = UnitState.Idle;
        unit.Path.Clear();
        unit.PathIndex = 0;
        unit.CommandQueue.Clear();
        unit.ResourceTarget = null;
        unit.BuildingTarget = null;
        unit.AttackUnitTarget = null;
        unit.AttackBuildingTarget = null;
        unit.AttackMove = false;
    }

    void HandleBuildPlacement(GameState state, CameraController cameraController, Vector2 mouse)
    {
        var worker = state.SelectedWorker;
        if (worker is null)
        {
            state.BuildMode = BuildPlacementMode.None;
            return;
        }

        if (!cameraController.TryGetGroundPoint(mouse, out var groundPoint)) return;
        var anchor = state.Map.WorldToTile(groundPoint);
        var queue = IsShiftDown();
        if (BuildingSystem.TryBeginConstruction(state, worker, state.PendingBuildingType, anchor, out var building) && building is not null)
        {
            commandSystem.IssueBuild(state, worker, building, queue);
            state.BuildMode = BuildPlacementMode.None;
        }
    }

    void HandleSelectionClick(GameState state, CameraController cameraController, Vector2 mouse)
    {
        var unit = selectionSystem.PickUnit(state, cameraController, mouse);
        if (unit is not null && unit.Faction == Faction.Human)
        {
            selectionSystem.Select(state, unit);
            state.StatusText = unit is Worker ? "Worker selected." : "Unit selected.";
            return;
        }

        var building = selectionSystem.PickBuilding(state, cameraController, mouse);
        if (building is not null && building.Faction == Faction.Human)
        {
            selectionSystem.Select(state, building);
            state.StatusText = $"{building.DisplayName} selected.";
            return;
        }

        selectionSystem.ClearSelection(state);
        state.StatusText = "Selection cleared.";
    }

    void HandleDragSelection(GameState state, CameraController cameraController, Rectangle rectangle)
    {
        var units = selectionSystem.GetUnitsInScreenRectangle(state, cameraController, rectangle);
        selectionSystem.SelectUnits(state, units);

        state.StatusText = units.Count switch
        {
            0 => "Selection cleared.",
            1 => units[0] is Worker ? "Worker selected." : "Unit selected.",
            _ => $"{units.Count} units selected."
        };
    }

    void HandleRightClick(GameState state, CameraController cameraController)
    {
        var selectedUnits = state.SelectedUnits.ToList();
        var selectedBuilding = state.SelectedBuilding;
        if (selectedUnits.Count == 0 && selectedBuilding is null) return;
        var queue = IsShiftDown();

        var mouse = Raylib.GetMousePosition();
        var targetUnit = selectionSystem.PickUnit(state, cameraController, mouse);
        if (targetUnit is not null && selectedUnits.Any(unit => targetUnit.Faction != unit.Faction))
        {
            foreach (var unit in selectedUnits.Where(unit => targetUnit.Faction != unit.Faction))
            {
                commandSystem.IssueAttack(state, unit, targetUnit, queue);
            }

            state.StatusText = queue ? "Attack command queued." : selectedUnits.Count == 1 ? "Attack command issued." : $"{selectedUnits.Count} units ordered to attack.";
            return;
        }

        var targetBuilding = selectionSystem.PickBuilding(state, cameraController, mouse);
        if (targetBuilding is not null && selectedUnits.Any(unit => targetBuilding.Faction != unit.Faction))
        {
            foreach (var unit in selectedUnits.Where(unit => targetBuilding.Faction != unit.Faction))
            {
                commandSystem.IssueAttack(state, unit, targetBuilding, queue);
            }

            state.StatusText = queue ? "Attack structure command queued." : selectedUnits.Count == 1 ? "Attack structure command issued." : $"{selectedUnits.Count} units ordered to attack structure.";
            return;
        }

        var selectedWorkers = selectedUnits.OfType<Worker>().ToList();
        if (selectedWorkers.Count > 0)
        {
            var resource = selectionSystem.PickResource(state, cameraController, mouse);
            if (resource is not null)
            {
                foreach (var worker in selectedWorkers)
                {
                    commandSystem.IssueGather(state, worker, resource, queue);
                }

                state.StatusText = queue ? "Gather command queued." : selectedWorkers.Count == 1 ? "Worker ordered to gather ore." : $"{selectedWorkers.Count} workers ordered to gather ore.";
                return;
            }
        }

        if (!cameraController.TryGetGroundPoint(mouse, out var groundPoint)) return;

        if (selectedBuilding is not null)
        {
            selectedBuilding.RallyPoint = groundPoint;
            state.StatusText = "Rally point set.";
            return;
        }

        if (selectedUnits.Count == 0) return;
        if (!state.Map.CanWalk(state.Map.WorldToTile(groundPoint)))
        {
            state.StatusText = "That tile is blocked.";
            return;
        }

        for (var i = 0; i < selectedUnits.Count; i++)
        {
            var target = GetFormationTarget(state, groundPoint, i, selectedUnits.Count);
            commandSystem.IssueMove(state, selectedUnits[i], target, queue);
        }

        state.StatusText = queue ? "Move command queued." : selectedUnits.Count == 1 ? "Move command issued." : $"{selectedUnits.Count} units moving.";
    }

    void HandleAttackMoveClick(GameState state, CameraController cameraController, Vector2 mouse)
    {
        attackMoveArmed = false;
        if (!cameraController.TryGetGroundPoint(mouse, out var groundPoint)) return;

        var selectedUnits = state.SelectedUnits.ToList();
        if (selectedUnits.Count == 0) return;
        var queue = IsShiftDown();

        for (var i = 0; i < selectedUnits.Count; i++)
        {
            var target = GetFormationTarget(state, groundPoint, i, selectedUnits.Count);
            commandSystem.IssueMove(state, selectedUnits[i], target, queue, attackMove: true);
        }

        state.StatusText = queue ? "Attack-move queued." : $"{selectedUnits.Count} units attack-moving.";
    }

    static bool TryHandleMinimapClick(GameState state, CameraController cameraController, Vector2 mouse)
    {
        var bounds = CommandPanel.GetMinimapBounds();
        if (!Raylib.CheckCollisionPointRec(mouse, bounds)) return false;

        var nx = Math.Clamp((mouse.X - bounds.X) / bounds.Width, 0, 1);
        var nz = Math.Clamp((mouse.Y - bounds.Y) / bounds.Height, 0, 1);
        cameraController.CenterOn(state.Map, new Vector3(nx * state.Map.WorldWidth, 0, nz * state.Map.WorldHeight));
        state.StatusText = "Camera moved on minimap.";
        return true;
    }

    void BeginSelectionDrag(Vector2 mouse)
    {
        selectionDragStart = mouse;
        selectionDragActive = true;
        SelectionRectangle = null;
    }

    void CancelSelectionDrag()
    {
        selectionDragActive = false;
        SelectionRectangle = null;
    }

    static Rectangle MakeScreenRectangle(Vector2 start, Vector2 end)
    {
        return new Rectangle(
            MathF.Min(start.X, end.X),
            MathF.Min(start.Y, end.Y),
            MathF.Abs(end.X - start.X),
            MathF.Abs(end.Y - start.Y));
    }

    static bool IsDragSelection(Rectangle rectangle)
    {
        return rectangle.Width >= SelectionDragThreshold || rectangle.Height >= SelectionDragThreshold;
    }

    static Vector3 GetFormationTarget(GameState state, Vector3 center, int index, int count)
    {
        var target = center + FormationOffset(index, count);
        var targetTile = state.Map.FindNearestWalkable(state.Map.WorldToTile(target), radius: 4);
        return state.Map.TileCenter(targetTile);
    }

    static Vector3 FormationOffset(int index, int count)
    {
        if (count <= 1) return Vector3.Zero;

        const float spacing = 1.65f;
        var columns = Math.Max(1, (int)MathF.Ceiling(MathF.Sqrt(count)));
        var rows = Math.Max(1, (int)MathF.Ceiling(count / (float)columns));
        var column = index % columns;
        var row = index / columns;

        return new Vector3(
            (column - (columns - 1) * 0.5f) * spacing,
            0,
            (row - (rows - 1) * 0.5f) * spacing);
    }

    static bool IsShiftDown()
    {
        return Raylib.IsKeyDown((KeyboardKey)340) || Raylib.IsKeyDown((KeyboardKey)344);
    }

    static bool IsControlDown()
    {
        return Raylib.IsKeyDown((KeyboardKey)341) || Raylib.IsKeyDown((KeyboardKey)345);
    }
}
