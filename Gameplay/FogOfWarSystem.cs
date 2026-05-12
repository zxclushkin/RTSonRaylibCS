using TinyRts.Core;
using TinyRts.World;

namespace TinyRts.Gameplay;

public sealed class FogOfWarSystem
{
    public void Update(GameState state)
    {
        for (var y = 0; y < state.Map.Height; y++)
        {
            for (var x = 0; x < state.Map.Width; x++)
            {
                if (state.HumanVision[x, y] == TileVisibility.Visible)
                {
                    state.HumanVision[x, y] = TileVisibility.Explored;
                }
            }
        }

        foreach (var unit in state.Units.Where(u => u.Faction == Faction.Human && u.IsAlive))
        {
            Reveal(state, state.Map.WorldToTile(unit.Position), unit.VisionRange);
        }

        foreach (var building in state.Buildings.Where(b => b.Faction == Faction.Human && b.Health > 0))
        {
            Reveal(state, state.Map.WorldToTile(building.Position), building.VisionRange);
        }
    }

    public bool IsVisible(GameState state, System.Numerics.Vector3 position)
    {
        var tile = state.Map.WorldToTile(position);
        return state.HumanVision[tile.X, tile.Y] == TileVisibility.Visible;
    }

    public bool IsExplored(GameState state, System.Numerics.Vector3 position)
    {
        var tile = state.Map.WorldToTile(position);
        return state.HumanVision[tile.X, tile.Y] != TileVisibility.Unseen;
    }

    static void Reveal(GameState state, TileCoord center, int radius)
    {
        var radiusSquared = radius * radius;
        for (var y = center.Y - radius; y <= center.Y + radius; y++)
        {
            for (var x = center.X - radius; x <= center.X + radius; x++)
            {
                var coord = new TileCoord(x, y);
                if (!state.Map.IsInside(coord)) continue;

                var dx = x - center.X;
                var dy = y - center.Y;
                if (dx * dx + dy * dy > radiusSquared) continue;
                state.HumanVision[x, y] = TileVisibility.Visible;
            }
        }
    }
}
