using Raylib_cs;
using TinyRts.Audio;
using TinyRts.Gameplay;
using TinyRts.Input;
using TinyRts.Rendering;
using TinyRts.UI;

namespace TinyRts.Core;

public sealed class Game : IDisposable
{
    GameState state = new();
    readonly CommandSystem commandSystem = new();
    readonly CombatSystem combatSystem = new();
    readonly FogOfWarSystem fogOfWarSystem = new();
    readonly OrcAiSystem orcAiSystem = new();
    readonly SelectionSystem selectionSystem = new();
    readonly CameraController cameraController;
    readonly InputController inputController;
    readonly Renderer3D renderer = new();
    readonly Hud hud = new();
    readonly GameMenu gameMenu = new();
    readonly SimpleSound sound = new();
    bool menuOpen;

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

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            menuOpen = !menuOpen;
            state.StatusText = menuOpen ? "Game paused." : "Game resumed.";
        }

        if (menuOpen)
        {
            HandleMenuAction();
            return;
        }

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

    void HandleMenuAction()
    {
        var action = gameMenu.Update();
        if (action is null) return;

        sound.PlayClick();
        switch (action)
        {
            case GameMenuAction.Continue:
                menuOpen = false;
                state.StatusText = "Game resumed.";
                break;
            case GameMenuAction.NewGame:
                state = new GameState();
                fogOfWarSystem.Update(state);
                menuOpen = false;
                state.StatusText = "New game started.";
                break;
            case GameMenuAction.Save:
                state.StatusText = "Save is not implemented yet.";
                break;
            case GameMenuAction.Load:
                state.StatusText = "Load is not implemented yet.";
                break;
            case GameMenuAction.Settings:
                state.StatusText = "Settings are not implemented yet.";
                break;
            case GameMenuAction.Exit:
                Raylib.CloseWindow();
                break;
        }
    }

    void Draw()
    {
        var preview = cameraController.TryGetGroundPoint(Raylib.GetMousePosition(), out var point) ? point : (System.Numerics.Vector3?)null;
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(8, 10, 12, 255));
        renderer.Draw(state, cameraController.Camera, preview);
        hud.Draw(state, inputController.SelectionRectangle);
        if (menuOpen)
        {
            gameMenu.Draw();
        }
        Raylib.EndDrawing();
    }

    public void Dispose()
    {
        sound.Dispose();
        renderer.Dispose();
    }
}
