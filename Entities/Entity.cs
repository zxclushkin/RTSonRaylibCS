using System.Numerics;
using Raylib_cs;
using TinyRts.Gameplay;

namespace TinyRts.Entities;

public abstract class Entity
{
    protected Entity(Faction faction, Vector3 position)
    {
        Faction = faction;
        Position = position;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Faction Faction { get; }
    public Vector3 Position { get; set; }
    public abstract BoundingBox Bounds { get; }
}
