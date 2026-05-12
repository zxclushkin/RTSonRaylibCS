namespace TinyRts.Core;

public static class GameConfig
{
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 760;
    public const int TargetFps = 60;

    public const int MapWidth = 64;
    public const int MapHeight = 64;
    public const float TileSize = 2.0f;

    public const float CameraMoveSpeed = 34.0f;
    public const float CameraMinDistance = 22.0f;
    public const float CameraMaxDistance = 92.0f;

    public const int WorkerCarryCapacity = 10;
    public const float WorkerGatherSeconds = 1.15f;
    public const float WorkerBuildRate = 1.0f;

    public const int HumanStartingOre = 220;
    public const int OrcStartingOre = 220;
    public const float AiThinkInterval = 1.0f;
}
