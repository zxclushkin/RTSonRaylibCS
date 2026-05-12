using TinyRts.Gameplay;

namespace TinyRts.World;

public sealed class MapSaveData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<SavedTileData> Tiles { get; set; } = [];
    public List<SavedStartPosition> StartPositions { get; set; } = [];
    public List<SavedResourceNode> Resources { get; set; } = [];
    public List<SavedBuilding> Buildings { get; set; } = [];
}

public sealed class SavedTileData
{
    public int X { get; set; }
    public int Y { get; set; }
    public TerrainType Terrain { get; set; }
    public bool Walkable { get; set; }
    public bool Buildable { get; set; }
}

public sealed class SavedStartPosition
{
    public Faction Faction { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class SavedResourceNode
{
    public ResourceType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Amount { get; set; }
}

public sealed class SavedBuilding
{
    public Faction Faction { get; set; }
    public BuildingType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
