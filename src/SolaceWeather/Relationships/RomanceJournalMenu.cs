using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

/// <summary>Read-only history browser. Selecting a person never advances their relationship.</summary>
internal sealed class RomanceJournalMenu : IClickableMenu
{
    private static readonly Color Ink = new(239, 228, 249), Muted = new(190, 178, 202), Panel = new(33, 25, 43);
    private readonly RomanceSaveState state;
    private readonly Func<string, string> describe;
    private readonly Action openAbigailTree;
    private string selected;
    private Rectangle roster, details, close, tree;
    private int rosterScroll, detailScroll, detailHeight;
    private const int RowHeight = 52;

    public RomanceJournalMenu(RomanceSaveState state, string selected, Func<string, string> describe, Action openAbigailTree)
    {
        this.state = state;
        this.selected = RomanceProfiles.Get(selected)?.Name ?? "Abigail";
        this.describe = describe;
        this.openAbigailTree = openAbigailTree;
        Layout();
        RevealSelection();
    }

    private void Layout()
    {
        width = Math.Max(240, Math.Min(1060, Game1.uiViewport.Width - 24));
        height = Math.Max(240, Math.Min(820, Game1.uiViewport.Height - 24));
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        close = new(xPositionOnScreen + width - 42, yPositionOnScreen + 10, 30, 30);
        if (width >= 620)
        {
            roster = new(xPositionOnScreen + 16, yPositionOnScreen + 62, 180, height - 110);
            details = new(roster.Right + 14, roster.Y, width - 226, roster.Height);
        }
        else
        {
            roster = new(xPositionOnScreen + 16, yPositionOnScreen + 62, width - 32, Math.Min(130, (height - 110) / 3));
            details = new(roster.X, roster.Bottom + 10, roster.Width, height - 120 - roster.Height);
        }
        tree = new(xPositionOnScreen + 16, yPositionOnScreen + height - 36, Math.Min(220, width - 32), 28);
        rosterScroll = Math.Clamp(rosterScroll, 0, Math.Max(0, RomanceProfiles.All.Count * RowHeight - roster.Height));
    }

    private static int Text(SpriteBatch b, string text, Rectangle area, int y, Color color, float scale = .8f)
    {
        int lineHeight = (int)Math.Ceiling(Game1.smallFont.LineSpacing * scale);
        foreach (string line in Game1.parseText(text, Game1.smallFont, Math.Max(30, (int)((area.Width - 24) / scale))).Split('\n'))
        {
            if (y >= area.Top && y + lineHeight <= area.Bottom)
                b.DrawString(Game1.smallFont, line.TrimEnd('\r'), new Vector2(area.X + 12, y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
            y += lineHeight;
        }
        return y;
    }

    public override void draw(SpriteBatch b)
    {
        Layout();
        var screen = new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height);
        b.Draw(Game1.staminaRect, screen, Color.Black * .75f);
        b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), new Color(21, 15, 29));
        Text(b, "Relationships", new(xPositionOnScreen + 4, yPositionOnScreen + 10, width - 55, 40), yPositionOnScreen + 14, Ink, .95f);
        Text(b, "X", close, close.Y + 2, Ink);
        b.Draw(Game1.staminaRect, roster, Panel);
        b.Draw(Game1.staminaRect, details, Panel);
        for (int i = 0; i < RomanceProfiles.All.Count; i++)
        {
            string name = RomanceProfiles.All[i].Name;
            var row = new Rectangle(roster.X, roster.Y + i * RowHeight - rosterScroll, roster.Width, RowHeight - 3);
            if (name == selected)
            {
                var visible = Rectangle.Intersect(row, roster);
                if (visible.Height > 0) b.Draw(Game1.staminaRect, visible, new Color(91, 57, 120));
            }
            Text(b, name, roster, row.Y + 12, name == selected ? Ink : Muted);
        }
        string content = describe(selected);
        int lineHeight = (int)Math.Ceiling(Game1.smallFont.LineSpacing * .8f);
        detailHeight = Game1.parseText(content, Game1.smallFont, Math.Max(30, (int)((details.Width - 24) / .8f))).Split('\n').Length * lineHeight + 24;
        detailScroll = Math.Clamp(detailScroll, 0, Math.Max(0, detailHeight - details.Height));
        Text(b, content, details, details.Y + 12 - detailScroll, Ink);
        Scrollbar(b, roster, rosterScroll, RomanceProfiles.All.Count * RowHeight);
        Scrollbar(b, details, detailScroll, detailHeight);
        if (selected == "Abigail")
        {
            b.Draw(Game1.staminaRect, tree, new Color(91, 57, 120));
            Text(b, "Abigail's relationship tree", tree, tree.Y + 3, Ink, .65f);
        }
        if (width >= 620)
        {
            var hint = new Rectangle(tree.Right + 10, tree.Y, width - tree.Width - 42, 28);
            Text(b, "Scroll to read / Left or right: person / Esc: close", hint, hint.Y + 3, Muted, .6f);
        }
        drawMouse(b);
    }

    private static void Scrollbar(SpriteBatch b, Rectangle area, int scroll, int contentHeight)
    {
        if (contentHeight <= area.Height) return;
        int thumb = Math.Max(12, area.Height * area.Height / contentHeight);
        int top = area.Y + (area.Height - thumb) * scroll / Math.Max(1, contentHeight - area.Height);
        b.Draw(Game1.staminaRect, new Rectangle(area.Right - 5, top, 3, thumb), Muted);
    }

    private void RevealSelection()
    {
        int index = RomanceProfiles.All.ToList().FindIndex(p => p.Name == selected);
        int top = index * RowHeight;
        if (top < rosterScroll) rosterScroll = top;
        if (top + RowHeight > rosterScroll + roster.Height) rosterScroll = top + RowHeight - roster.Height;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (close.Contains(x, y)) { exitThisMenu(); return; }
        if (selected == "Abigail" && tree.Contains(x, y))
        {
            var prior = Game1.activeClickableMenu;
            openAbigailTree();
            if (!ReferenceEquals(prior, Game1.activeClickableMenu)) Game1.activeClickableMenu.exitFunction = () => Game1.activeClickableMenu = this;
            return;
        }
        if (!roster.Contains(x, y)) return;
        int index = (y - roster.Y + rosterScroll) / RowHeight;
        if (index < 0 || index >= RomanceProfiles.All.Count) return;
        selected = RomanceProfiles.All[index].Name;
        detailScroll = 0;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int delta = direction > 0 ? -64 : 64;
        if (roster.Contains(Game1.getMouseX(true), Game1.getMouseY(true)))
            rosterScroll = Math.Clamp(rosterScroll + delta, 0, Math.Max(0, RomanceProfiles.All.Count * RowHeight - roster.Height));
        else detailScroll = Math.Clamp(detailScroll + delta, 0, Math.Max(0, detailHeight - details.Height));
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape) { exitThisMenu(); return; }
        if (key is Keys.Left or Keys.Right or Keys.Tab)
        {
            int index = RomanceProfiles.All.ToList().FindIndex(p => p.Name == selected);
            selected = RomanceProfiles.All[(index + (key == Keys.Left ? 11 : 1)) % RomanceProfiles.All.Count].Name;
            detailScroll = 0;
            RevealSelection();
        }
        if (key is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown)
            detailScroll = Math.Clamp(detailScroll + (key is Keys.Up or Keys.PageUp ? -64 : 64), 0, Math.Max(0, detailHeight - details.Height));
    }
}
