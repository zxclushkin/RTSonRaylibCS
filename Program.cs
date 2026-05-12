using Raylib_cs;
using TinyRts.Core;

Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
Raylib.InitWindow(GameConfig.ScreenWidth, GameConfig.ScreenHeight, "Tiny RTS 3D Prototype");
Raylib.SetTargetFPS(GameConfig.TargetFps);

using (var game = new Game())
{
    game.Run();
}

Raylib.CloseWindow();
