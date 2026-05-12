using Raylib_cs;

namespace TinyRts.UI;

public enum GameMenuAction
{
    Continue,
    NewGame,
    Save,
    Load,
    Settings,
    Exit
}

public sealed class GameMenu
{
    readonly (GameMenuAction Action, string Label)[] items =
    [
        (GameMenuAction.Continue, "Продолжить"),
        (GameMenuAction.NewGame, "Новая игра"),
        (GameMenuAction.Save, "Сохранить"),
        (GameMenuAction.Load, "Загрузить"),
        (GameMenuAction.Settings, "Настройки"),
        (GameMenuAction.Exit, "Выход")
    ];

    public GameMenuAction? Update()
    {
        var mouse = Raylib.GetMousePosition();
        for (var i = 0; i < items.Length; i++)
        {
            if (!Raylib.CheckCollisionPointRec(mouse, GetButtonBounds(i))) continue;
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                return items[i].Action;
            }
        }

        return null;
    }

    public void Draw()
    {
        var sw = Raylib.GetScreenWidth();
        var sh = Raylib.GetScreenHeight();
        var panel = GetPanelBounds();

        Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, 140));
        Raylib.DrawRectangleRounded(panel, 0.08f, 8, new Color(20, 25, 30, 245));
        Raylib.DrawRectangleRoundedLines(panel, 0.08f, 8, new Color(95, 110, 125, 255));
        Raylib.DrawText("МЕНЮ", (int)panel.X + 138, (int)panel.Y + 20, 34, Color.RayWhite);

        var mouse = Raylib.GetMousePosition();
        for (var i = 0; i < items.Length; i++)
        {
            var button = GetButtonBounds(i);
            var hovered = Raylib.CheckCollisionPointRec(mouse, button);
            var color = hovered ? new Color(54, 74, 88, 255) : new Color(33, 45, 54, 240);
            Raylib.DrawRectangleRounded(button, 0.2f, 6, color);
            Raylib.DrawRectangleRoundedLines(button, 0.2f, 6, new Color(113, 129, 142, 255));
            Raylib.DrawText(items[i].Label, (int)button.X + 18, (int)button.Y + 10, 22, Color.RayWhite);
        }
    }

    static Rectangle GetPanelBounds()
    {
        const int panelWidth = 360;
        const int panelHeight = 430;
        return new Rectangle((Raylib.GetScreenWidth() - panelWidth) / 2, (Raylib.GetScreenHeight() - panelHeight) / 2, panelWidth, panelHeight);
    }

    static Rectangle GetButtonBounds(int index)
    {
        var panel = GetPanelBounds();
        return new Rectangle(panel.X + 40, panel.Y + 80 + index * 55, panel.Width - 80, 42);
    }
}
