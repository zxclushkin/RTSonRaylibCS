using System.Numerics;
using TinyRts.Core;
using TinyRts.Gameplay;
using TinyRts.World;

namespace TinyRts.Entities;

public sealed class Worker : Unit
{
    public Worker(Faction faction, UnitType type, Vector3 position) : base(faction, type, position)
    {
    }

    public int CarriedOre { get; set; }
    public float GatherTimer { get; set; } = GameConfig.WorkerGatherSeconds;
    public ResourceNode? PreferredResource { get; set; }
}
