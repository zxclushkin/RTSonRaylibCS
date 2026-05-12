namespace TinyRts.World;

public readonly record struct TileCoord(int X, int Y);

public enum TerrainType
{
    Grass,
    Stone,
    ShallowWater
}

public sealed class Tile
{
    public Tile(int x, int y)
    {
        Coord = new TileCoord(x, y);
    }

    public TileCoord Coord { get; }
    public TerrainType Terrain { get; set; } = TerrainType.Grass;
    public bool Walkable { get; set; } = true;
    public bool Buildable { get; set; } = true;
    public Guid? ResourceId { get; set; }
    public Guid? BuildingId { get; set; }

    public bool HasResource => ResourceId.HasValue;
    public bool OccupiedByBuilding => BuildingId.HasValue;
}
