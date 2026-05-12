using System.Numerics;
using Raylib_cs;
using TinyRts.World;

namespace TinyRts.Rendering;

public static class DebugRenderer
{
    public static void DrawGrid(MapGrid map)
    {
        var color = new Color(45, 62, 55, 150);
        for (var x = 0; x <= map.Width; x++)
        {
            var worldX = x * map.TileSize;
            Raylib.DrawLine3D(new Vector3(worldX, 0.025f, 0), new Vector3(worldX, 0.025f, map.WorldHeight), color);
        }

        for (var y = 0; y <= map.Height; y++)
        {
            var worldZ = y * map.TileSize;
            Raylib.DrawLine3D(new Vector3(0, 0.025f, worldZ), new Vector3(map.WorldWidth, 0.025f, worldZ), color);
        }
    }

    public static void DrawGroundRing(Vector3 center, float radius, Color color)
    {
        const int segments = 40;
        var previous = center + new Vector3(radius, 0.06f, 0);
        for (var i = 1; i <= segments; i++)
        {
            var angle = MathF.Tau * i / segments;
            var next = center + new Vector3(MathF.Cos(angle) * radius, 0.06f, MathF.Sin(angle) * radius);
            Raylib.DrawLine3D(previous, next, color);
            previous = next;
        }
    }
}
