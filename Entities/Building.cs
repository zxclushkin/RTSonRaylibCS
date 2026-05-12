using System.Numerics;
using Raylib_cs;
using TinyRts.Gameplay;
using TinyRts.World;

namespace TinyRts.Entities;

public sealed class Building : Entity
{
    public Building(Faction faction, BuildingType type, TileCoord anchorTile, Vector3 position, int footprintWidth, int footprintHeight, bool underConstruction) : base(faction, position)
    {
        Type = type;
        AnchorTile = anchorTile;
        FootprintWidth = footprintWidth;
        FootprintHeight = footprintHeight;
        IsUnderConstruction = underConstruction;
        BuildProgress = underConstruction ? 0 : BuildTime;
    }

    public BuildingType Type { get; }
    public TileCoord AnchorTile { get; }
    public int FootprintWidth { get; }
    public int FootprintHeight { get; }
    public float BuildTime { get; init; } = 8.0f;
    public float BuildProgress { get; set; }
    public bool IsUnderConstruction { get; set; }
    public bool IsCompleted => !IsUnderConstruction;
    public string DisplayName { get; init; } = "Building";
    public float MaxHealth { get; init; } = 420;
    public float Health { get; set; } = 420;
    public int VisionRange { get; init; } = 9;
    public bool Selected { get; set; }
    public Queue<UnitType> ProductionQueue { get; } = [];
    public UnitType? CurrentTraining { get; set; }
    public float TrainingProgress { get; set; }
    public Vector3 RallyPoint { get; set; }

    public Vector3 Size => new(FootprintWidth * 2.0f, IsUnderConstruction ? 1.2f : 3.2f, FootprintHeight * 2.0f);

    public override BoundingBox Bounds => new()
    {
        Min = Position + new Vector3(-Size.X / 2f, 0, -Size.Z / 2f),
        Max = Position + new Vector3(Size.X / 2f, Size.Y, Size.Z / 2f)
    };
}
