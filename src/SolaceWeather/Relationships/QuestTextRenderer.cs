using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace SolaceWeather.Relationships;

internal static class QuestTextRenderer
{
    internal static readonly Color ItemColor = new(130, 48, 170);

    internal static void DrawButton(SpriteBatch batch, Rectangle bounds, string label, IEnumerable<string> names)
    {
        bool hover = bounds.Contains(Game1.getMouseX(), Game1.getMouseY());
        batch.Draw(Game1.fadeToBlackRect, bounds, hover ? new Color(255, 238, 203) : new Color(250, 222, 171));
        float scale = Math.Min(1f, (bounds.Width - 24) / Game1.smallFont.MeasureString(label).X);
        var at = new Vector2(bounds.X + 12, bounds.Y + 12);
        batch.DrawString(Game1.smallFont, label, at, Game1.textColor, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
        foreach (var range in QuestText.FindItems(label, names))
            batch.DrawString(Game1.smallFont, label.Substring(range.Start, range.Length),
                at + new Vector2(Game1.smallFont.MeasureString(label[..range.Start]).X * scale, 0), ItemColor,
                0, Vector2.Zero, scale, SpriteEffects.None, 1);
    }

    // Recolor only quest glyphs after native dialogue drawing. Native paging, animation,
    // text layout, and input remain in charge; no shared font state or global patches.
    internal static void OverlayDialogue(SpriteBatch batch, string text, int x, int y, int width, int visible, IReadOnlyList<ItemTextRange> ranges)
    {
        if (ranges.Count == 0) return;
        float zoom = SpriteText.FontPixelZoom;
        float penX = Math.Max(0, Math.Min(x, Game1.graphics.GraphicsDevice.Viewport.Width - width - 4));
        float penY = y;
        bool latin = LocalizedContentManager.CurrentLanguageLatin
            || (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru && !Game1.options.useAlternateFont);
        for (int i = 0; i < Math.Min(text.Length, visible); i++)
        {
            char c = text[i];
            float lineHeight = (latin ? 18 : SpriteText.FontFile.Common.LineHeight + 2) * zoom;
            if (c == '^') { penX = x; penY += lineHeight; continue; }
            if (SpriteText.positionOfNextSpace(text, i, (int)penX - (latin ? x : 0), 0) >= (latin ? width : x + width - 4))
            {
                penX = x; penY += lineHeight;
                if (latin && c == ' ') continue;
            }
            if (ranges.Any(r => i >= r.Start && i < r.Start + r.Length))
                SpriteText.drawString(batch, c.ToString(), (int)penX, (int)penY, 1,
                    Math.Max(32, Game1.graphics.GraphicsDevice.Viewport.Width - (int)penX - 4), color: ItemColor);
            if (latin)
            {
                if (i < text.Length - 1) penX += (8 + SpriteText.getWidthOffsetForChar(text[i + 1])) * zoom;
                penX += SpriteText.getWidthOffsetForChar(c) * zoom;
            }
            else if (SpriteText.characterMap.TryGetValue(c, out var glyph)) penX += glyph.XAdvance * zoom;
        }
    }
}
