using System.Numerics;
using Raylib_cs;
using TinyRts.Gameplay;
using TinyRts.World;

namespace TinyRts.Entities;

public enum UnitState
{
    Idle,
    Moving,
    Gathering,
    ReturningResource,
    Building,
    Attacking
}

public enum UnitCommandType
{
    None,
    Move,
    Gather,
    Build,
    Attack
}

public class Unit : Entity
{
    public Unit(Faction faction, UnitType type, Vector3 position) : base(faction, position)
    {
        Type = type;
        var definition = UnitCatalog.Get(type);
        MaxHealth = definition.MaxHealth;
        Health = MaxHealth;
        Speed = definition.Speed;
        Radius = definition.Radius;
        Damage = definition.Damage;
        AttackRange = definition.AttackRange;
        AttackInterval = definition.AttackInterval;
        VisionRange = definition.VisionRange;
    }

    public UnitType Type { get; }
    public float Health { get; set; }
    public float MaxHealth { get; }
    public float Speed { get; protected init; } = 7.5f;
    public float Radius { get; protected init; } = 0.62f;
    public float Damage { get; protected init; }
    public float AttackRange { get; protected init; }
    public float AttackInterval { get; protected init; } = 1.0f;
    public float AttackCooldown { get; set; }
    public int VisionRange { get; protected init; } = 7;
    public bool Selected { get; set; }
    public UnitState State { get; set; } = UnitState.Idle;
    public UnitCommandType CommandType { get; set; } = UnitCommandType.None;
    public Vector3 MoveTarget { get; set; }
    public List<Vector3> Path { get; } = [];
    public int PathIndex { get; set; }
    public Queue<QueuedUnitCommand> CommandQueue { get; } = [];
    public ResourceNode? ResourceTarget { get; set; }
    public Building? BuildingTarget { get; set; }
    public Unit? AttackUnitTarget { get; set; }
    public Building? AttackBuildingTarget { get; set; }
    public bool AttackMove { get; set; }
    public bool IsAlive => Health > 0;

    public override BoundingBox Bounds => new()
    {
        Min = Position + new Vector3(-Radius, 0, -Radius),
        Max = Position + new Vector3(Radius, 2.2f, Radius)
    };
}
