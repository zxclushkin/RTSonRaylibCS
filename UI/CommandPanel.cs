using Raylib_cs;
using TinyRts.Core;
using TinyRts.Entities;
using TinyRts.Gameplay;

namespace TinyRts.UI;

public enum CommandAction
{
    BuildProduction,
    TrainPrimary,
    TrainSecondary,
    Stop,
    Cancel
}

public sealed record CommandButton(CommandAction Action, string Label, string Hotkey, Rectangle Bounds);

public static class CommandPanel
{
    public static Rectangle GetMinimapBounds()
    {
        const int size = 164;
        return new Rectangle(Raylib.GetScreenWidth() - size - 18, 150, size, size);
    }

    public static List<CommandButton> GetButtons(GameState state)
    {
        var buttons = new List<CommandButton>();
        var x = Raylib.GetScreenWidth() - 410;
        var y = Raylib.GetScreenHeight() - 148;
        var w = 124;
        var h = 48;
        var gap = 10;

        void Add(CommandAction action, string label, string hotkey)
        {
            var index = buttons.Count;
            var col = index % 3;
            var row = index / 3;
            buttons.Add(new CommandButton(action, label, hotkey, new Rectangle(x + col * (w + gap), y + row * (h + gap), w, h)));
        }

        if (state.BuildMode != BuildPlacementMode.None)
        {
            Add(CommandAction.Cancel, "Cancel", "Esc");
            return buttons;
        }

        if (state.SelectedWorker is not null)
        {
            Add(CommandAction.BuildProduction, "Build", "B");
            Add(CommandAction.Stop, "Stop", "S");
        }
        else if (state.SelectedBuilding is { Faction: Faction.Human } building)
        {
            var spec = BuildingSystem.GetBuildingSpec(building.Faction, building.Type);
            if (spec.TrainsUnits.Count > 0)
            {
                var unit = UnitCatalog.Get(spec.TrainsUnits[0]);
                Add(CommandAction.TrainPrimary, $"Train {unit.DisplayName}", "T");
            }

            if (spec.TrainsUnits.Count > 1)
            {
                var unit = UnitCatalog.Get(spec.TrainsUnits[1]);
                Add(CommandAction.TrainSecondary, $"Train {unit.DisplayName}", "Y");
            }

            Add(CommandAction.Stop, "Stop Queue", "S");
        }

        return buttons;
    }

    public static CommandAction? HitTest(GameState state, System.Numerics.Vector2 mouse)
    {
        foreach (var button in GetButtons(state))
        {
            if (Raylib.CheckCollisionPointRec(mouse, button.Bounds))
            {
                return button.Action;
            }
        }

        return null;
    }
}
