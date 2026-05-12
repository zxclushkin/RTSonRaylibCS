using System.Numerics;
using System.Text.Json;
using TinyRts.Core;
using TinyRts.Gameplay;

namespace TinyRts.World;

public sealed class MapGrid
{
    readonly Tile[,] tiles;

    public MapGrid(int width, int height, float tileSize)
    {
        Width = width;
        Height = height;
        TileSize = tileSize;
        tiles = new Tile[width, height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                tiles[x, y] = new Tile(x, y);
            }
        }
    }

    public int Width { get; }
    public int Height { get; }
    public float TileSize { get; }
    public float WorldWidth => Width * TileSize;
    public float WorldHeight => Height * TileSize;

    public Tile GetTile(TileCoord coord) => tiles[coord.X, coord.Y];

    public bool IsInside(TileCoord coord)
    {
        return coord.X >= 0 && coord.Y >= 0 && coord.X < Width && coord.Y < Height;
    }

    public Vector3 TileCenter(TileCoord coord)
    {
        return new Vector3((coord.X + 0.5f) * TileSize, 0, (coord.Y + 0.5f) * TileSize);
    }

    public Vector3 FootprintCenter(TileCoord anchor, int width, int height)
    {
        return new Vector3((anchor.X + width / 2f) * TileSize, 0, (anchor.Y + height / 2f) * TileSize);
    }

    public TileCoord WorldToTile(Vector3 world)
    {
        return new TileCoord(
            Math.Clamp((int)MathF.Floor(world.X / TileSize), 0, Width - 1),
            Math.Clamp((int)MathF.Floor(world.Z / TileSize), 0, Height - 1));
    }

    public bool CanWalk(TileCoord coord)
    {
        return IsInside(coord) && GetTile(coord) is { Walkable: true, OccupiedByBuilding: false };
    }

    public IEnumerable<TileCoord> GetNeighbors(TileCoord coord)
    {
        TileCoord[] candidates =
        [
            new TileCoord(coord.X + 1, coord.Y),
            new TileCoord(coord.X - 1, coord.Y),
            new TileCoord(coord.X, coord.Y + 1),
            new TileCoord(coord.X, coord.Y - 1),
            new TileCoord(coord.X + 1, coord.Y + 1),
            new TileCoord(coord.X - 1, coord.Y - 1),
            new TileCoord(coord.X + 1, coord.Y - 1),
            new TileCoord(coord.X - 1, coord.Y + 1)
        ];

        foreach (var candidate in candidates)
        {
            if (IsInside(candidate))
            {
                yield return candidate;
            }
        }
    }

    public TileCoord FindNearestWalkable(TileCoord preferred, int radius = 5)
    {
        if (CanWalk(preferred)) return preferred;

        for (var r = 1; r <= radius; r++)
        {
            for (var y = -r; y <= r; y++)
            {
                for (var x = -r; x <= r; x++)
                {
                    if (Math.Abs(x) != r && Math.Abs(y) != r) continue;
                    var candidate = new TileCoord(preferred.X + x, preferred.Y + y);
                    if (CanWalk(candidate)) return candidate;
                }
            }
        }

        return preferred;
    }

    public TileCoord FindNearestWalkableAdjacent(TileCoord anchor, int width, int height, int radius = 4)
    {
        var center = new TileCoord(anchor.X + width / 2, anchor.Y + height / 2);
        var best = center;
        var bestScore = int.MaxValue;

        for (var y = -radius; y < height + radius; y++)
        {
            for (var x = -radius; x < width + radius; x++)
            {
                var onInnerFootprint = x >= 0 && y >= 0 && x < width && y < height;
                if (onInnerFootprint) continue;

                var candidate = new TileCoord(anchor.X + x, anchor.Y + y);
                if (!CanWalk(candidate)) continue;

                var dx = candidate.X - center.X;
                var dy = candidate.Y - center.Y;
                var score = dx * dx + dy * dy;
                if (score >= bestScore) continue;
                best = candidate;
                bestScore = score;
            }
        }

        return bestScore == int.MaxValue ? FindNearestWalkable(center, radius + 2) : best;
    }

    public bool CanPlaceBuilding(TileCoord anchor, int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var coord = new TileCoord(anchor.X + x, anchor.Y + y);
                if (!IsInside(coord)) return false;
                var tile = GetTile(coord);
                if (!tile.Buildable || !tile.Walkable || tile.HasResource || tile.OccupiedByBuilding) return false;
            }
        }

        return true;
    }

    public void ReserveBuilding(TileCoord anchor, int width, int height, Guid buildingId)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var tile = GetTile(new TileCoord(anchor.X + x, anchor.Y + y));
                tile.BuildingId = buildingId;
                tile.Walkable = false;
            }
        }
    }

    public void ReleaseBuilding(Guid buildingId)
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var tile = tiles[x, y];
                if (tile.BuildingId != buildingId) continue;
                tile.BuildingId = null;
                tile.Walkable = true;
            }
        }
    }

    public void MarkResource(TileCoord coord, Guid resourceId)
    {
        var tile = GetTile(coord);
        tile.ResourceId = resourceId;
        tile.Buildable = false;
    }

    public MapSaveData ToSaveData(IEnumerable<ResourceNode> resources)
    {
        var data = new MapSaveData { Width = Width, Height = Height };
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var tile = tiles[x, y];
                data.Tiles.Add(new SavedTileData
                {
                    X = x,
                    Y = y,
                    Terrain = tile.Terrain,
                    Walkable = tile.Walkable,
                    Buildable = tile.Buildable
                });
            }
        }

        foreach (var resource in resources)
        {
            data.Resources.Add(new SavedResourceNode
            {
                Type = resource.Type,
                X = resource.Tile.X,
                Y = resource.Tile.Y,
                Amount = resource.Amount
            });
        }

        return data;
    }

    public static MapGrid FromSaveData(MapSaveData data)
    {
        var grid = new MapGrid(data.Width, data.Height, GameConfig.TileSize);
        foreach (var savedTile in data.Tiles)
        {
            if (!grid.IsInside(new TileCoord(savedTile.X, savedTile.Y))) continue;
            var tile = grid.GetTile(new TileCoord(savedTile.X, savedTile.Y));
            tile.Terrain = savedTile.Terrain;
            tile.Walkable = savedTile.Walkable;
            tile.Buildable = savedTile.Buildable;
        }

        return grid;
    }

    public static MapGrid LoadOrCreateDefault(string path, out MapSaveData? loadedData)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            loadedData = JsonSerializer.Deserialize<MapSaveData>(json);
            if (loadedData is not null)
            {
                return FromSaveData(loadedData);
            }
        }

        loadedData = null;
        return CreateDefault();
    }

    public static MapGrid CreateDefault()
    {
        var grid = new MapGrid(GameConfig.MapWidth, GameConfig.MapHeight, GameConfig.TileSize);

        for (var y = 24; y < 31; y++)
        {
            for (var x = 24; x < 41; x++)
            {
                var tile = grid.GetTile(new TileCoord(x, y));
                tile.Terrain = TerrainType.ShallowWater;
                tile.Walkable = false;
                tile.Buildable = false;
            }
        }

        for (var i = 0; i < 64; i++)
        {
            var edge = grid.GetTile(new TileCoord(i, 0));
            edge.Buildable = false;
            edge.Walkable = false;
        }

        return grid;
    }
}
