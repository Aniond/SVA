using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

internal sealed record TreeCardView(string Id, string Title, string Branch, string Requirement, string Perk, string State, string[] Parents);
internal sealed record RelationshipTreeView(TreeCardView[] Cards, string Outlook, string HelpStatus, string History, string PromiseDetails, string Approach);

/// <summary>A read-only journal: selecting a card never accepts a promise or grants a perk.</summary>
internal sealed class RelationshipTreeMenu : IClickableMenu
{
    private static readonly string[] Branches = { "Strange Little Treasures", "Beyond the Fence", "On My Own Terms" };
    private static readonly Color Ink = new(239, 228, 249), Muted = new(190, 178, 202), Panel = new(33, 25, 43);
    private readonly Func<RelationshipTreeView> getView;
    private readonly Dictionary<string, Rectangle> cards = new();
    private readonly List<(string Text, Rectangle Bounds)> headings = new();
    private RelationshipTreeView view;
    private Rectangle treeArea, detailArea, close;
    private readonly Rectangle[] tabs = new Rectangle[3];
    private string? selected;
    private int tab, treeScroll, detailScroll, treeHeight, detailHeight;

    public RelationshipTreeMenu(Func<RelationshipTreeView> getView)
    {
        this.getView = getView;
        view = getView();
        selected = view.Cards.FirstOrDefault()?.Id;
        Layout();
    }

    private void Layout()
    {
        width = Math.Max(240, Math.Min(1220, Game1.uiViewport.Width - 24));
        height = Math.Max(240, Math.Min(840, Game1.uiViewport.Height - 24));
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        int x = xPositionOnScreen + 16, y = yPositionOnScreen + 90;
        close = new Rectangle(xPositionOnScreen + width - 48, yPositionOnScreen + 12, 32, 32);
        for (int i = 0; i < 3; i++) tabs[i] = new Rectangle(x + i * 100, yPositionOnScreen + 48, 94, 32);
        if (width >= 940)
        {
            treeArea = new Rectangle(x, y, width - 360, height - 128);
            detailArea = new Rectangle(treeArea.Right + 12, y, 316, treeArea.Height);
        }
        else
        {
            int available = height - 128;
            int detail = Math.Min(210, Math.Max(92, available / 3));
            treeArea = new Rectangle(x, y, width - 32, available - detail - 10);
            detailArea = new Rectangle(x, treeArea.Bottom + 10, width - 32, detail);
        }
        if (tab != 0) detailArea = new Rectangle(x, y, width - 32, height - 128);
        cards.Clear(); headings.Clear();
        bool columns = treeArea.Width >= 650;
        int gap = 14, cardWidth = columns ? (treeArea.Width - 24 - gap * 2) / 3 : treeArea.Width - 24;
        var root = view.Cards.FirstOrDefault(c => c.Parents.Length == 0);
        if (root != null) cards[root.Id] = new Rectangle((treeArea.Width - cardWidth) / 2, 12, cardWidth, 96);
        int bottom = 116, nextY = 138;
        for (int branch = 0; branch < Branches.Length; branch++)
        {
            int bx = columns ? 12 + branch * (cardWidth + gap) : 12;
            int by = columns ? 138 : nextY;
            headings.Add((Branches[branch], new Rectangle(bx, by, cardWidth, 56)));
            by += 64;
            foreach (var card in view.Cards.Where(c => c.Id != root?.Id && c.Branch == Branches[branch]))
            {
                cards[card.Id] = new Rectangle(bx, by, cardWidth, 92);
                by += 116;
            }
            bottom = Math.Max(bottom, by);
            nextY = by + 14;
        }
        treeHeight = bottom;
        treeScroll = Math.Clamp(treeScroll, 0, Math.Max(0, treeHeight - treeArea.Height));
    }

    private Rectangle OnTree(Rectangle r) => new(treeArea.X + r.X, treeArea.Y + r.Y - treeScroll, r.Width, r.Height);
    private static Color StatusColor(string state) => state.ToLowerInvariant() switch
    {
        "unlocked" or "active" or "earned" => new Color(174, 120, 228),
        "paused" or "suspended" => new Color(226, 174, 79),
        "available" or "ready" => new Color(115, 199, 173),
        _ => new Color(139, 134, 149)
    };

    private static void Fill(SpriteBatch b, Rectangle r, Color color, Rectangle clip)
    {
        r = Rectangle.Intersect(r, clip);
        if (r.Width > 0 && r.Height > 0) b.Draw(Game1.staminaRect, r, color);
    }

    // Clip whole lines instead of changing the game's SpriteBatch/scissor state.
    private static int Text(SpriteBatch b, string text, int x, int y, int width, Color color, Rectangle clip, float scale = .8f)
    {
        string wrapped = Game1.parseText(text ?? "", Game1.smallFont, Math.Max(30, (int)(width / scale)));
        int lineHeight = (int)Math.Ceiling(Game1.smallFont.LineSpacing * scale);
        foreach (string line in wrapped.Split('\n'))
        {
            if (y >= clip.Top && y + lineHeight <= clip.Bottom)
                b.DrawString(Game1.smallFont, line.TrimEnd('\r'), new Vector2(x, y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
            y += lineHeight;
        }
        return y;
    }

    public override void draw(SpriteBatch b)
    {
        view = getView();
        Layout();
        var screen = new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height);
        b.Draw(Game1.staminaRect, screen, Color.Black * .75f);
        b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), new Color(21, 15, 29));
        Text(b, "Abigail / Relationship", xPositionOnScreen + 16, yPositionOnScreen + 12, width - 80, Ink, screen, .95f);
        Text(b, "X", close.X + 7, close.Y + 3, 26, Ink, screen);
        for (int i = 0; i < tabs.Length; i++)
        {
            b.Draw(Game1.staminaRect, tabs[i], i == tab ? new Color(91, 57, 120) : Panel);
            Text(b, new[] { "Tree", "History", "Promises" }[i], tabs[i].X + 8, tabs[i].Y + 4, tabs[i].Width - 12, Ink, tabs[i], .65f);
        }
        if (tab == 0) DrawTree(b);
        DrawDetails(b);
        Text(b, "Scroll panels / Arrow keys scroll / Esc closes", xPositionOnScreen + 16, yPositionOnScreen + height - 28, width - 32, Muted, screen, .65f);
        drawMouse(b);
    }

    private void DrawTree(SpriteBatch b)
    {
        b.Draw(Game1.staminaRect, treeArea, Panel);
        foreach (var card in view.Cards)
        {
            if (!cards.TryGetValue(card.Id, out var target)) continue;
            foreach (string parent in card.Parents)
            {
                if (!cards.TryGetValue(parent, out var source)) continue;
                var a = OnTree(source); var z = OnTree(target);
                int middle = z.Top - 12;
                if (a.X == z.X && z.Top - a.Bottom > 40)
                {
                    // A later sibling shares its parent's connector, rather than looking like
                    // it depends on the intervening sibling (the safe/bold adventure fork).
                    int gutter = z.Right + 5;
                    Fill(b, new Rectangle(a.Center.X, a.Bottom + 8, gutter - a.Center.X + 2, 2), Muted * .5f, treeArea);
                    Fill(b, new Rectangle(a.Center.X, a.Bottom, 2, 10), Muted * .5f, treeArea);
                    Fill(b, new Rectangle(gutter, a.Bottom + 8, 2, Math.Max(2, middle - a.Bottom - 8)), Muted * .5f, treeArea);
                    Fill(b, new Rectangle(z.Center.X, middle, gutter - z.Center.X + 2, 2), Muted * .5f, treeArea);
                }
                else
                {
                    Fill(b, new Rectangle(a.Center.X, a.Bottom, 2, Math.Max(2, middle - a.Bottom)), Muted * .5f, treeArea);
                    Fill(b, new Rectangle(Math.Min(a.Center.X, z.Center.X), middle, Math.Abs(a.Center.X - z.Center.X) + 2, 2), Muted * .5f, treeArea);
                }
                Fill(b, new Rectangle(z.Center.X, middle, 2, 12), Muted * .5f, treeArea);
            }
        }
        foreach (var heading in headings)
        {
            var r = OnTree(heading.Bounds);
            Fill(b, r, Panel, treeArea);
            Text(b, heading.Text, r.X + 4, r.Y, r.Width - 8, new Color(213, 179, 246), Rectangle.Intersect(treeArea, r), .8f);
        }
        foreach (var card in view.Cards)
        {
            if (!cards.TryGetValue(card.Id, out var logical)) continue;
            var r = OnTree(logical);
            var clip = Rectangle.Intersect(r, treeArea);
            if (clip.Width <= 0 || clip.Height <= 0) continue;
            Fill(b, r, selected == card.Id ? new Color(73, 48, 93) : new Color(46, 37, 56), treeArea);
            Color color = StatusColor(card.State);
            Fill(b, new Rectangle(r.X, r.Y, 4, r.Height), color, treeArea);
            int inset = 12;
            if (card.Parents.Length == 0)
            {
                var portrait = new Rectangle(r.X + 10, r.Y + 12, 64, 64);
                if (treeArea.Contains(portrait))
                    b.Draw(Game1.content.Load<Texture2D>("Portraits/Abigail"), portrait, new Rectangle(0, 0, 64, 64), Color.White);
                inset = 84;
            }
            Text(b, card.Title, r.X + inset, r.Y + 9, r.Width - inset - 8, Ink, clip, .78f);
            Text(b, card.State, r.X + inset, r.Bottom - 28, r.Width - inset - 8, color, clip, .7f);
        }
        Scrollbar(b, treeArea, treeScroll, treeHeight);
    }

    private void DrawDetails(SpriteBatch b)
    {
        b.Draw(Game1.staminaRect, detailArea, Panel);
        var chosen = view.Cards.FirstOrDefault(c => c.Id == selected);
        string text = tab switch
        {
            1 => $"History\n\n{view.History}",
            2 => $"Promises\n\n{view.PromiseDetails}\n\nHelp status\n{view.HelpStatus}",
            _ => chosen == null ? "Select a card to read its story." :
                $"{chosen.Title}\n{chosen.State}\n\nRequirements\n{chosen.Requirement}\n\nPerk\n{chosen.Perk}\n\nAbigail's outlook\n{view.Outlook}\n\nHelp status\n{view.HelpStatus}\n\nApproach\n{view.Approach}"
        };
        int textWidth = detailArea.Width - 28;
        detailHeight = (int)Math.Ceiling(Game1.smallFont.LineSpacing * .8f) * Game1.parseText(text, Game1.smallFont, Math.Max(30, (int)(textWidth / .8f))).Split('\n').Length + 24;
        detailScroll = Math.Clamp(detailScroll, 0, Math.Max(0, detailHeight - detailArea.Height));
        Text(b, text, detailArea.X + 12, detailArea.Y + 10 - detailScroll, textWidth, Ink, detailArea);
        Scrollbar(b, detailArea, detailScroll, detailHeight);
    }

    private static void Scrollbar(SpriteBatch b, Rectangle area, int scroll, int contentHeight)
    {
        if (contentHeight <= area.Height) return;
        int thumb = Math.Max(18, area.Height * area.Height / contentHeight);
        int top = area.Y + (area.Height - thumb) * scroll / Math.Max(1, contentHeight - area.Height);
        b.Draw(Game1.staminaRect, new Rectangle(area.Right - 5, top, 3, thumb), Muted);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (close.Contains(x, y)) { exitThisMenu(); return; }
        for (int i = 0; i < tabs.Length; i++)
            if (tabs[i].Contains(x, y)) { tab = i; detailScroll = 0; Layout(); return; }
        if (tab == 0 && treeArea.Contains(x, y))
            foreach (var card in cards)
                if (OnTree(card.Value).Contains(x, y)) { selected = card.Key; detailScroll = 0; return; }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int delta = direction > 0 ? -64 : 64;
        if (tab == 0 && treeArea.Contains(Game1.getMouseX(true), Game1.getMouseY(true)))
            treeScroll = Math.Clamp(treeScroll + delta, 0, Math.Max(0, treeHeight - treeArea.Height));
        else detailScroll = Math.Clamp(detailScroll + delta, 0, Math.Max(0, detailHeight - detailArea.Height));
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape) { exitThisMenu(); return; }
        if (key == Keys.Tab) { tab = (tab + 1) % 3; detailScroll = 0; Layout(); return; }
        if (key is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown)
        {
            int delta = key is Keys.Up or Keys.PageUp ? -64 : 64;
            if (tab == 0) treeScroll = Math.Clamp(treeScroll + delta, 0, Math.Max(0, treeHeight - treeArea.Height));
            else detailScroll = Math.Clamp(detailScroll + delta, 0, Math.Max(0, detailHeight - detailArea.Height));
        }
    }
}
