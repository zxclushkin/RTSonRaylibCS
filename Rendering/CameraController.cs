using System.Numerics;
using Raylib_cs;
using TinyRts.Core;
using TinyRts.World;

namespace TinyRts.Rendering;

public sealed class CameraController
{
    float distance = 52.0f;
    Vector3 target;

    public CameraController(MapGrid map)
    {
        target = new Vector3(map.WorldWidth * 0.28f, 0, map.WorldHeight * 0.28f);
        Camera = BuildCamera();
    }

    public Camera3D Camera { get; private set; }

    public Vector3 Target => target;

    public void Update(MapGrid map, float dt)
    {
        var move = Vector3.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) move.Z -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) move.Z += 1;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) move.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) move.X += 1;

        if (move.LengthSquared() > 0)
        {
            target += Vector3.Normalize(move) * GameConfig.CameraMoveSpeed * dt;
        }

        var wheel = Raylib.GetMouseWheelMove();
        if (MathF.Abs(wheel) > 0.01f)
        {
            distance = Math.Clamp(distance - wheel * 4.5f, GameConfig.CameraMinDistance, GameConfig.CameraMaxDistance);
        }

        target.X = Math.Clamp(target.X, 4, map.WorldWidth - 4);
        target.Z = Math.Clamp(target.Z, 4, map.WorldHeight - 4);
        Camera = BuildCamera();
    }

    public void CenterOn(MapGrid map, Vector3 worldPosition)
    {
        target = new Vector3(
            Math.Clamp(worldPosition.X, 4, map.WorldWidth - 4),
            0,
            Math.Clamp(worldPosition.Z, 4, map.WorldHeight - 4));
        Camera = BuildCamera();
    }

    public bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
    {
        var ray = Raylib.GetScreenToWorldRay(screenPosition, Camera);
        if (MathF.Abs(ray.Direction.Y) < 0.0001f)
        {
            point = default;
            return false;
        }

        var t = -ray.Position.Y / ray.Direction.Y;
        if (t < 0)
        {
            point = default;
            return false;
        }

        point = ray.Position + ray.Direction * t;
        point.Y = 0;
        return true;
    }

    Camera3D BuildCamera()
    {
        var offset = new Vector3(0, distance * 0.88f, distance);
        return new Camera3D
        {
            Position = target + offset,
            Target = target,
            Up = Vector3.UnitY,
            FovY = 45.0f,
            Projection = CameraProjection.Perspective
        };
    }
}
