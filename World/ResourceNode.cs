using System.Numerics;
using Raylib_cs;
using TinyRts.Gameplay;

namespace TinyRts.World;

public sealed class ResourceNode
{
    public ResourceNode(ResourceType type, TileCoord tile, Vector3 position, int amount)
    {
        Type = type;
        Tile = tile;
        Position = position;
        Amount = amount;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public ResourceType Type { get; }
    public TileCoord Tile { get; }
    public Vector3 Position { get; }
    public int Amount { get; set; }
    public float Radius { get; } = 1.15f;

    public BoundingBox Bounds => new()
    {
        Min = Position + new Vector3(-Radius, 0, -Radius),
        Max = Position + new Vector3(Radius, 2.4f, Radius)
    };
}
