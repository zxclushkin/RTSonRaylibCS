using Raylib_cs;
using TinyRts.Core;
using TinyRts.Entities;
using TinyRts.Gameplay;
using TinyRts.World;

namespace TinyRts.UI;

public sealed class Hud
{
    public void Draw(GameState state, Rectangle? selectionRectangle)
    {
        Raylib.DrawRectangle(12, 12, 610, 126, new Color(8, 10, 12, 225));
        Raylib.DrawRectangleLines(12, 12, 610, 126, new Color(90, 105, 112, 255));

        Raylib.DrawText($"Ore: {state.Human.Ore}", 28, 28, 24, Color.Gold);
        Raylib.DrawText($"Selected: {SelectedLabel(state)}", 170, 28, 22, Color.RayWhite);
        Raylib.DrawText($"Mode: {ModeLabel(state)}", 28, 62, 20, state.BuildMode == BuildPlacementMode.None ? Color.SkyBlue : Color.Lime);
        Raylib.DrawText(state.StatusText, 28, 92, 18, Color.LightGray);

        DrawSelectionRectangle(selectionRectangle);
        DrawMinimap(state);
        DrawSelectedUnitPanel(state);

        var controls = "WASD/Arrows camera | Wheel zoom | LMB/drag select | RMB command | Shift queue | A attack-move | Ctrl+1..9 groups";
        Raylib.DrawText(controls, 18, Raylib.GetScreenHeight() - 28, 18, new Color(210, 215, 210, 255));
        DrawCommandPanel(state);
    }

    static string SelectedLabel(GameState state)
    {
        var selectedUnits = state.SelectedUnits.ToList();
        if (selectedUnits.Count > 1)
        {
            var workers = selectedUnits.Count(unit => unit is Worker);
            return workers > 0 ? $"{selectedUnits.Count} units, {workers} workers" : $"{selectedUnits.Count} units";
        }

        return state.SelectedUnit switch
        {
            Worker worker => $"{worker.Type} ({worker.State}) carry {worker.CarriedOre}",
            Unit unit => $"{unit.Type} ({unit.State})",
            null when state.SelectedBuilding is not null => $"{state.SelectedBuilding.DisplayName}",
            _ => "none"
        };
    }

    static string ModeLabel(GameState state)
    {
        return state.BuildMode == BuildPlacementMode.None ? "command" : $"placing {state.PendingBuildingType}";
    }

    static void DrawCommandPanel(GameState state)
    {
        var buttons = CommandPanel.GetButtons(state);
        if (buttons.Count == 0) return;

        Raylib.DrawText("Commands", Raylib.GetScreenWidth() - 410, Raylib.GetScreenHeight() - 178, 20, Color.RayWhite);
        foreach (var button in buttons)
        {
            Raylib.DrawRectangleRounded(button.Bounds, 0.12f, 8, new Color(26, 32, 38, 235));
            Raylib.DrawRectangleRoundedLines(button.Bounds, 0.12f, 8, new Color(115, 130, 145, 255));
            Raylib.DrawText(Fit(button.Label, 14), (int)button.Bounds.X + 10, (int)button.Bounds.Y + 9, 14, Color.RayWhite);
            Raylib.DrawText(button.Hotkey, (int)button.Bounds.X + 10, (int)button.Bounds.Y + 29, 15, Color.Gold);
        }
    }

    static void DrawMinimap(GameState state)
    {
        var bounds = CommandPanel.GetMinimapBounds();
        Raylib.DrawRectangleRec(bounds, new Color(7, 11, 13, 235));
        Raylib.DrawRectangleLinesEx(bounds, 2, new Color(94, 116, 124, 255));
        Raylib.DrawText("Map", (int)bounds.X + 8, (int)bounds.Y + 7, 16, Color.RayWhite);

        var mapArea = new Rectangle(bounds.X + 8, bounds.Y + 28, bounds.Width - 16, bounds.Height - 36);
        Raylib.DrawRectangleRec(mapArea, new Color(24, 42, 32, 255));

        for (var y = 0; y < state.Map.Height; y++)
        {
            for (var x = 0; x < state.Map.Width; x++)
            {
                var visibility = state.HumanVision[x, y];
                if (visibility == TileVisibility.Unseen) continue;

                var color = visibility == TileVisibility.Visible
                    ? new Color(42, 86, 52, 255)
                    : new Color(36, 42, 40, 255);
                var px = mapArea.X + x / (float)state.Map.Width * mapArea.Width;
                var py = mapArea.Y + y / (float)state.Map.Height * mapArea.Height;
                Raylib.DrawPixel((int)px, (int)py, color);
            }
        }

        foreach (var resource in state.Resources.Where(resource => resource.Amount > 0))
        {
            DrawMinimapPoint(state, mapArea, resource.Position, Color.Gold, 2);
        }

        foreach (var building in state.Buildings.Where(building => building.Health > 0))
        {
            if (building.Faction != Faction.Human && GetVisibility(state, building.Position) == TileVisibility.Unseen) continue;
            DrawMinimapPoint(state, mapArea, building.Position, FactionCatalog.Get(building.Faction).PrimaryColor, 4);
        }

        foreach (var unit in state.Units.Where(unit => unit.IsAlive))
        {
            if (unit.Faction != Faction.Human && GetVisibility(state, unit.Position) != TileVisibility.Visible) continue;
            DrawMinimapPoint(state, mapArea, unit.Position, FactionCatalog.Get(unit.Faction).SecondaryColor, unit.Selected ? 4 : 2);
        }
    }

    static void DrawSelectedUnitPanel(GameState state)
    {
        var selectedUnits = state.SelectedUnits.Take(12).ToList();
        if (selectedUnits.Count == 0) return;

        const int iconSize = 36;
        const int gap = 6;
        var x = 18;
        var y = Raylib.GetScreenHeight() - 82;

        for (var i = 0; i < selectedUnits.Count; i++)
        {
            var unit = selectedUnits[i];
            var rect = new Rectangle(x + i * (iconSize + gap), y, iconSize, iconSize);
            Raylib.DrawRectangleRounded(rect, 0.08f, 6, UnitCatalog.Get(unit.Type).BodyColor);
            Raylib.DrawRectangleRoundedLines(rect, 0.08f, 6, unit == state.SelectedUnit ? Color.Lime : new Color(70, 82, 90, 255));

            var hp = Math.Clamp(unit.Health / unit.MaxHealth, 0, 1);
            Raylib.DrawRectangle((int)rect.X, (int)(rect.Y + iconSize + 2), iconSize, 4, new Color(70, 10, 10, 255));
            Raylib.DrawRectangle((int)rect.X, (int)(rect.Y + iconSize + 2), (int)(iconSize * hp), 4, new Color(80, 220, 90, 255));
        }
    }

    static string Fit(string value, int max)
    {
        return value.Length <= max ? value : value[..Math.Max(0, max - 1)] + ".";
    }

    static void DrawSelectionRectangle(Rectangle? selectionRectangle)
    {
        if (selectionRectangle is not { } rectangle) return;

        Raylib.DrawRectangleRec(rectangle, new Color(78, 185, 105, 38));
        Raylib.DrawRectangleLinesEx(rectangle, 1.5f, new Color(110, 245, 135, 210));
    }

    static void DrawMinimapPoint(GameState state, Rectangle mapArea, System.Numerics.Vector3 position, Color color, int size)
    {
        var x = mapArea.X + position.X / state.Map.WorldWidth * mapArea.Width;
        var y = mapArea.Y + position.Z / state.Map.WorldHeight * mapArea.Height;
        Raylib.DrawRectangle((int)x - size / 2, (int)y - size / 2, size, size, color);
    }

    static TileVisibility GetVisibility(GameState state, System.Numerics.Vector3 position)
    {
        var tile = state.Map.WorldToTile(position);
        return state.HumanVision[tile.X, tile.Y];
    }
}
