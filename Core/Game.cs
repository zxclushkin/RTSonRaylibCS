using Raylib_cs;
using TinyRts.Audio;
using TinyRts.Gameplay;
using TinyRts.Input;
using TinyRts.Rendering;
using TinyRts.UI;

namespace TinyRts.Core;

public sealed class Game : IDisposable
{
    readonly GameState state = new();
    readonly CommandSystem commandSystem = new();
    readonly CombatSystem combatSystem = new();
    readonly FogOfWarSystem fogOfWarSystem = new();
    readonly OrcAiSystem orcAiSystem = new();
    readonly SelectionSystem selectionSystem = new();
    readonly CameraController cameraController;
    readonly InputController inputController;
    readonly Renderer3D renderer = new();
    readonly Hud hud = new();
    readonly SimpleSound sound = new();

    public Game()
    {
        cameraController = new CameraController(state.Map);
        inputController = new InputController(selectionSystem, commandSystem);
        fogOfWarSystem.Update(state);
    }

    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            var dt = Raylib.GetFrameTime();
            Update(dt);
            Draw();
        }
    }

    void Update(float dt)
    {
        cameraController.Update(state.Map, dt);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) || Raylib.IsMouseButtonPressed(MouseButton.Right))
        {
            sound.PlayClick();
        }

        inputController.Update(state, cameraController);
        orcAiSystem.Update(state, commandSystem, dt);
        BuildingSystem.UpdateProduction(state, commandSystem, dt);
        commandSystem.Update(state, dt);
        combatSystem.Update(state, commandSystem);
        combatSystem.CleanupDead(state);
        fogOfWarSystem.Update(state);
    }

    void Draw()
    {
        var preview = cameraController.TryGetGroundPoint(Raylib.GetMousePosition(), out var point) ? point : (System.Numerics.Vector3?)null;
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(8, 10, 12, 255));
        renderer.Draw(state, cameraController.Camera, preview);
        hud.Draw(state, inputController.SelectionRectangle);
        Raylib.EndDrawing();
    }

    public void Dispose()
    {
        sound.Dispose();
        renderer.Dispose();
    }
}
