using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

/// <summary>Lists the conversation's available services. Only the controller can perform a selection.</summary>
internal sealed partial class NpcConversation
{
private sealed class ServiceChoicesMenu : IClickableMenu
{
    private readonly Func<QuestChoice[]> choices;
    private readonly Func<string> help;
    private readonly Action<string> choose;
    private readonly Action back;
    private QuestChoice[] visible = Array.Empty<QuestChoice>();
    private readonly List<(Rectangle Bounds, string Label)> rows = new();
    private Rectangle list, backButton;
    private int scroll, contentHeight;
    private string helpText = "";

    public ServiceChoicesMenu(Func<QuestChoice[]> choices, Func<string> help, Action<string> choose, Action back)
    {
        this.choices = choices; this.help = help; this.choose = choose; this.back = back;
        Layout();
    }

    private void Layout()
    {
        visible = choices();
        width = Math.Min(840, Game1.uiViewport.Width - 32);
        height = Math.Min(660, Game1.uiViewport.Height - 32);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        list = new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 102, width - 32, Math.Max(48, height - 180));
        backButton = new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + height - 64, 180, 48);
        rows.Clear();
        helpText = Game1.parseText(help(), Game1.smallFont, Math.Max(40, (int)((list.Width - 32) / .85f)));
        int y = string.IsNullOrWhiteSpace(helpText) ? 0 : (int)Math.Ceiling(Game1.smallFont.MeasureString(helpText).Y * .85f) + 24;
        foreach (var choice in visible)
        {
            string label = Game1.parseText(choice.Label, Game1.smallFont, Math.Max(40, (int)((list.Width - 32) / .85f)));
            int rowHeight = Math.Max(56, (int)Math.Ceiling(Game1.smallFont.MeasureString(label).Y * .85f) + 24);
            rows.Add((new Rectangle(list.X, y, list.Width - 8, rowHeight), label));
            y += rowHeight + 10;
        }
        contentHeight = y + (visible.Length == 0 ? 56 : 0);
        scroll = Math.Clamp(scroll, 0, Math.Max(0, contentHeight - list.Height));
    }

    public override void draw(SpriteBatch b)
    {
        Layout();
        b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * .75f);
        b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), new Color(33, 25, 43));
        b.DrawString(Game1.smallFont, Speaker + " — relationship choices", new Vector2(xPositionOnScreen + 20, yPositionOnScreen + 18), Color.Wheat);
        string hint = "Choose a favor or plan to discuss. Scroll for more.";
        float hintScale = Math.Min(.8f, (width - 40) / Game1.smallFont.MeasureString(hint).X);
        b.DrawString(Game1.smallFont, hint, new Vector2(xPositionOnScreen + 20, yPositionOnScreen + 58), Color.LightGray,
            0, Vector2.Zero, hintScale, SpriteEffects.None, 1);
        DrawLines(b, helpText, list.X + 12, list.Y + 12 - scroll, Color.LightGray);
        if (visible.Length == 0)
            DrawLines(b, "No perks to discuss yet.", list.X + 12,
                list.Y + contentHeight - 44 - scroll, Color.LightGray);
        for (int i = 0; i < rows.Count; i++)
        {
            var bounds = RowBounds(i);
            var clip = Rectangle.Intersect(bounds, list);
            if (clip.Height <= 0) continue;
            bool hover = list.Contains(Game1.getMouseX(true), Game1.getMouseY(true)) && bounds.Contains(Game1.getMouseX(true), Game1.getMouseY(true));
            b.Draw(Game1.staminaRect, clip, hover ? new Color(88, 59, 113) : new Color(56, 41, 70));
            DrawLines(b, rows[i].Label, bounds.X + 12, bounds.Y + 12, Color.White);
        }
        if (contentHeight > list.Height)
        {
            int thumb = Math.Max(18, list.Height * list.Height / contentHeight);
            int top = list.Y + (list.Height - thumb) * scroll / Math.Max(1, contentHeight - list.Height);
            b.Draw(Game1.staminaRect, new Rectangle(list.Right - 4, top, 3, thumb), Color.Plum);
        }
        QuestTextRenderer.DrawButton(b, backButton, "Back to " + Speaker, Array.Empty<string>());
        drawMouse(b);
    }

    private void DrawLines(SpriteBatch b, string text, int x, int y, Color color)
    {
        int lineHeight = (int)Math.Ceiling(Game1.smallFont.LineSpacing * .85f);
        foreach (var line in text.Split('\n'))
        {
            if (y >= list.Top && y + lineHeight <= list.Bottom)
                b.DrawString(Game1.smallFont, line.TrimEnd('\r'), new Vector2(x, y), color,
                    0, Vector2.Zero, .85f, SpriteEffects.None, 1);
            y += lineHeight;
        }
    }

    private Rectangle RowBounds(int index)
    {
        var bounds = rows[index].Bounds;
        bounds.Y += list.Y - scroll;
        return bounds;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (backButton.Contains(x, y)) { back(); return; }
        if (!list.Contains(x, y)) return;
        for (int i = 0; i < rows.Count; i++)
            if (RowBounds(i).Contains(x, y))
            {
                // Keep the displayed key, but reject options no longer offered by the controller.
                string key = visible[i].Key;
                if (choices().Any(choice => choice.Key == key)) choose(key);
                return;
            }
    }

    public override void receiveScrollWheelAction(int direction) =>
        scroll = Math.Clamp(scroll + (direction > 0 ? -64 : 64), 0, Math.Max(0, contentHeight - list.Height));

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape) { back(); return; }
        if (key is Keys.Up or Keys.PageUp) receiveScrollWheelAction(1);
        if (key is Keys.Down or Keys.PageDown) receiveScrollWheelAction(-1);
    }
}
}
