using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Tailoring;

internal sealed class TailoringPreview : IClickableMenu
{
    private readonly Item item;
    private readonly string text;
    private readonly Action craft, cancel;
    internal TailoringPreview(Item item, string text, Action craft, Action cancel) { this.item = item; this.text = text; this.craft = craft; this.cancel = cancel; }
    private Rectangle Panel => new((Game1.uiViewport.Width - 720) / 2, (Game1.uiViewport.Height - 430) / 2, 720, 430);
    private Rectangle Button(int i) => new(Panel.X + 24, Panel.Y + 260 + i * 50, 672, 44);
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    { if (Button(0).Contains(x, y)) craft(); else if (Button(1).Contains(x, y)) Game1.exitActiveMenu(); else if (Button(2).Contains(x, y)) cancel(); }
    public override void receiveKeyPress(Keys key) { if (key == Keys.Escape) Game1.exitActiveMenu(); }
    public override void draw(SpriteBatch b)
    {
        var p = Panel; drawTextureBox(b, p.X, p.Y, p.Width, p.Height, Color.White);
        item.drawInMenu(b, new Vector2(p.X + 28, p.Y + 36), 2f);
        b.DrawString(Game1.smallFont, Game1.parseText(text, Game1.smallFont, 480), new Vector2(p.X + 180, p.Y + 30), Game1.textColor);
        var labels = new[] { "Use these materials and craft this garment", "Keep preview for later", "Cancel this order" };
        for (int i = 0; i < 3; i++) { var r = Button(i); drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White); b.DrawString(Game1.smallFont, labels[i], new Vector2(r.X + 12, r.Y + 10), Game1.textColor); }
        drawMouse(b);
    }
}
