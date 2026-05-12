using System.Numerics;
using TinyRts.World;

namespace TinyRts.Entities;

public sealed record QueuedUnitCommand(
    UnitCommandType Type,
    Vector3? TargetPosition = null,
    ResourceNode? ResourceTarget = null,
    Building? BuildingTarget = null,
    Unit? AttackUnitTarget = null,
    bool AttackMove = false);
