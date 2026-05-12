using System.Numerics;
using Raylib_cs;
using TinyRts.Core;
using TinyRts.Entities;
using TinyRts.Gameplay;
using TinyRts.World;

namespace TinyRts.Rendering;

public sealed class Renderer3D : IDisposable
{
    const string TownHallModelRelativePath = "Assets/Models/town_hall.glb";

    Model? townHallModel;
    BoundingBox townHallBounds;
    bool townHallModelLoadAttempted;

    public void Draw(GameState state, Camera3D camera, Vector3? buildPreviewPoint)
    {
        Raylib.BeginMode3D(camera);
        DrawMap(state.Map);
        DrawResources(state);
        DrawBuildings(state);
        DrawUnits(state);
        DrawBuildPreview(state, buildPreviewPoint);
        DrawFog(state);
        Raylib.EndMode3D();
    }

    void DrawMap(MapGrid map)
    {
        Raylib.DrawPlane(
            new Vector3(map.WorldWidth / 2f, -0.03f, map.WorldHeight / 2f),
            new Vector2(map.WorldWidth, map.WorldHeight),
            new Color(31, 46, 37, 255));

        foreach (var tile in EnumerateTiles(map))
        {
            if (tile.Terrain != TerrainType.ShallowWater) continue;
            var center = map.TileCenter(tile.Coord);
            Raylib.DrawCubeV(center + new Vector3(0, -0.04f, 0), new Vector3(map.TileSize, 0.04f, map.TileSize), new Color(35, 78, 92, 210));
        }

        DebugRenderer.DrawGrid(map);
    }

    void DrawResources(GameState state)
    {
        foreach (var resource in state.Resources.Where(r => r.Amount > 0))
        {
            var visibility = GetVisibility(state, resource.Position);
            if (visibility == TileVisibility.Unseen) continue;
            var color = visibility == TileVisibility.Visible
                ? new Color(218, 177, 57, 255)
                : new Color(108, 95, 64, 190);
            Raylib.DrawSphere(resource.Position + new Vector3(0, 1.05f, 0), resource.Radius, color);
            Raylib.DrawSphereWires(resource.Position + new Vector3(0, 1.05f, 0), resource.Radius + 0.08f, 12, 12, visibility == TileVisibility.Visible ? new Color(255, 240, 160, 255) : new Color(120, 115, 95, 180));
        }
    }

    void DrawBuildings(GameState state)
    {
        foreach (var building in state.Buildings)
        {
            var visibility = GetVisibility(state, building.Position);
            if (building.Faction != Faction.Human && visibility == TileVisibility.Unseen) continue;

            var definition = FactionCatalog.Get(building.Faction);
            var baseColor = building.IsUnderConstruction ? new Color(120, 120, 120, 190) : definition.PrimaryColor;
            if (building.Faction != Faction.Human && visibility == TileVisibility.Explored)
            {
                baseColor = new Color(65, 65, 65, 180);
            }

            var size = building.Size;
            var center = building.Position + new Vector3(0, size.Y / 2f, 0);

            if (!TryDrawBuildingModel(building))
            {
                Raylib.DrawCubeV(center, size, baseColor);
                Raylib.DrawCubeWiresV(center, size, definition.SecondaryColor);
            }

            DrawHealthBar3D(building.Position + new Vector3(0, size.Y + 0.45f, 0), building.Health / building.MaxHealth, 3.8f);

            if (building.IsUnderConstruction)
            {
                var progress = building.BuildTime <= 0 ? 1 : building.BuildProgress / building.BuildTime;
                Raylib.DrawCubeV(building.Position + new Vector3(0, 0.12f, 0), new Vector3(size.X * progress, 0.16f, size.Z), new Color(82, 210, 102, 220));
            }

            if (building.CurrentTraining is not null)
            {
                var unit = UnitCatalog.Get(building.CurrentTraining.Value);
                var progress = Math.Clamp(building.TrainingProgress / unit.TrainTime, 0, 1);
                Raylib.DrawCubeV(building.Position + new Vector3(0, size.Y + 0.7f, 0), new Vector3(3.8f * progress, 0.13f, 0.35f), new Color(80, 160, 255, 230));
            }

            if (building.Selected)
            {
                DebugRenderer.DrawGroundRing(building.Position, MathF.Max(size.X, size.Z) * 0.58f, Color.Lime);
                DebugRenderer.DrawGroundRing(building.RallyPoint, 0.55f, Color.SkyBlue);
                Raylib.DrawLine3D(building.Position + new Vector3(0, 0.1f, 0), building.RallyPoint + new Vector3(0, 0.1f, 0), new Color(100, 200, 255, 180));
            }
        }
    }

    bool TryDrawBuildingModel(Building building)
    {
        if (!IsMainBuilding(building) || building.IsUnderConstruction) return false;
        if (!TryGetTownHallModel(out var model)) return false;

        var modelWidth = MathF.Max(0.001f, townHallBounds.Max.X - townHallBounds.Min.X);
        var modelHeight = MathF.Max(0.001f, townHallBounds.Max.Y - townHallBounds.Min.Y);
        var targetWidth = building.FootprintWidth * 2.0f * 0.92f;
        var targetDepth = building.FootprintHeight * 2.0f * 0.92f;

        // This GLB arrives with its height/depth axes swapped for raylib's Y-up world.
        // Keep uniform scale so the mesh keeps the artist-authored proportions.
        var uniformScale = MathF.Min(targetWidth / modelWidth, targetDepth / modelHeight);
        var scale = new Vector3(uniformScale, uniformScale, uniformScale);
        var modelCenterX = (townHallBounds.Min.X + townHallBounds.Max.X) * 0.5f;
        var modelCenterY = (townHallBounds.Min.Y + townHallBounds.Max.Y) * 0.5f;
        var modelMinZ = townHallBounds.Min.Z;
        var position = new Vector3(
            building.Position.X - modelCenterX * uniformScale,
            building.Position.Y - modelMinZ * uniformScale,
            building.Position.Z + modelCenterY * uniformScale);

        Raylib.DrawModelEx(model, position, Vector3.UnitX, -90, scale, Color.White);
        return true;
    }

    bool TryGetTownHallModel(out Model model)
    {
        if (townHallModel is { } loaded)
        {
            model = loaded;
            return true;
        }

        if (townHallModelLoadAttempted)
        {
            model = default;
            return false;
        }

        townHallModelLoadAttempted = true;
        var modelPath = ResolveAssetPath(TownHallModelRelativePath);
        if (modelPath is null)
        {
            model = default;
            return false;
        }

        var loadedModel = Raylib.LoadModel(modelPath);
        if (!Raylib.IsModelValid(loadedModel))
        {
            model = default;
            return false;
        }

        townHallModel = loadedModel;
        townHallBounds = Raylib.GetModelBoundingBox(loadedModel);
        model = loadedModel;
        return true;
    }

    static string? ResolveAssetPath(string relativePath)
    {
        if (File.Exists(relativePath)) return relativePath;

        var outputPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (File.Exists(outputPath)) return outputPath;

        var projectPath = Path.Combine(Environment.CurrentDirectory, relativePath);
        return File.Exists(projectPath) ? projectPath : null;
    }

    static bool IsMainBuilding(Building building)
    {
        return building.Type is BuildingType.HumanTownHall or BuildingType.OrcGreatHall;
    }

    void DrawUnits(GameState state)
    {
        foreach (var unit in state.Units.Where(u => u.IsAlive))
        {
            if (unit.Faction != Faction.Human && GetVisibility(state, unit.Position) != TileVisibility.Visible) continue;

            var definition = FactionCatalog.Get(unit.Faction);
            var unitDefinition = UnitCatalog.Get(unit.Type);
            var bodyColor = unitDefinition.BodyColor;
            var bodyCenter = unit.Position + new Vector3(0, 0.75f, 0);

            Raylib.DrawCylinder(unit.Position + new Vector3(0, 0.08f, 0), unit.Radius, unit.Radius * 0.75f, 1.5f, 12, bodyColor);
            Raylib.DrawSphere(bodyCenter + new Vector3(0, 0.82f, 0), unit.Radius * 0.62f, definition.PrimaryColor);
            DrawHealthBar3D(unit.Position + new Vector3(0, 2.15f, 0), unit.Health / unit.MaxHealth, 1.5f);

            if (unit.Selected)
            {
                DebugRenderer.DrawGroundRing(unit.Position, unit.Radius + 0.42f, Color.Lime);
                DrawPath(unit);
            }
        }
    }

    void DrawBuildPreview(GameState state, Vector3? buildPreviewPoint)
    {
        if (state.BuildMode == BuildPlacementMode.None || buildPreviewPoint is null) return;
        var worker = state.SelectedWorker;
        if (worker is null) return;

        var anchor = state.Map.WorldToTile(buildPreviewPoint.Value);
        var type = state.PendingBuildingType;
        var spec = BuildingSystem.GetBuildingSpec(worker.Faction, type);
        var canBuild = state.Map.CanPlaceBuilding(anchor, spec.Width, spec.Height);
        var center = state.Map.FootprintCenter(anchor, spec.Width, spec.Height);
        var size = new Vector3(spec.Width * state.Map.TileSize, 1.1f, spec.Height * state.Map.TileSize);
        var color = canBuild ? new Color(80, 220, 110, 110) : new Color(230, 70, 65, 130);

        Raylib.DrawCubeV(center + new Vector3(0, size.Y / 2f, 0), size, color);
        Raylib.DrawCubeWiresV(center + new Vector3(0, size.Y / 2f, 0), size, canBuild ? Color.Lime : Color.Red);
    }

    void DrawFog(GameState state)
    {
        for (var y = 0; y < state.Map.Height; y++)
        {
            for (var x = 0; x < state.Map.Width; x++)
            {
                var visibility = state.HumanVision[x, y];
                if (visibility == TileVisibility.Visible) continue;

                var alpha = visibility == TileVisibility.Unseen ? 220 : 105;
                var center = state.Map.TileCenter(new TileCoord(x, y));
                Raylib.DrawCubeV(center + new Vector3(0, 0.055f, 0), new Vector3(state.Map.TileSize, 0.045f, state.Map.TileSize), new Color(0, 0, 0, alpha));
            }
        }
    }

    static TileVisibility GetVisibility(GameState state, Vector3 position)
    {
        var tile = state.Map.WorldToTile(position);
        return state.HumanVision[tile.X, tile.Y];
    }

    static void DrawPath(Unit unit)
    {
        if (unit.Path.Count == 0 || unit.PathIndex >= unit.Path.Count) return;

        var from = unit.Position + new Vector3(0, 0.08f, 0);
        for (var i = unit.PathIndex; i < unit.Path.Count; i++)
        {
            var to = unit.Path[i] + new Vector3(0, 0.08f, 0);
            Raylib.DrawLine3D(from, to, new Color(120, 230, 120, 140));
            from = to;
        }
    }

    static void DrawHealthBar3D(Vector3 center, float ratio, float width)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        Raylib.DrawCubeV(center, new Vector3(width, 0.12f, 0.18f), new Color(70, 10, 10, 220));
        Raylib.DrawCubeV(center + new Vector3(-(width - width * ratio) / 2f, 0.03f, 0), new Vector3(width * ratio, 0.14f, 0.2f), new Color(65, 220, 86, 235));
    }

    static IEnumerable<Tile> EnumerateTiles(MapGrid map)
    {
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                yield return map.GetTile(new TileCoord(x, y));
            }
        }
    }

    public void Dispose()
    {
        if (townHallModel is { } loaded)
        {
            Raylib.UnloadModel(loaded);
            townHallModel = null;
        }
    }
}
