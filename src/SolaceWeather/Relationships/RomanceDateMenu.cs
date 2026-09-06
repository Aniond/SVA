using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

/// <summary>Transparent native menu keeps the real meeting location and NPC visible behind each beat.</summary>
internal sealed class RomanceDateMenu : IClickableMenu
{
    private readonly NPC? npc;
    private readonly string text;
    private readonly (string Label, Action Action)[] options;
    private readonly Action cancel;
    private string narration = "";
    internal RomanceDateMenu(NPC? npc, string text, (string Label, Action Action)[] options, Action cancel)
    { this.npc = npc; this.text = text; this.options = options; this.cancel = cancel; }
    internal void SetNarration(string? value) => narration = string.IsNullOrWhiteSpace(value) ? "" : value.Replace('\n', ' ').Trim()[..Math.Min(value.Replace('\n', ' ').Trim().Length, 180)];
    private int Width => Math.Min(820, Game1.uiViewport.Width - 40);
    private int Left => (Game1.uiViewport.Width - Width) / 2;
    private int Top => Math.Max(20, Game1.uiViewport.Height - 230 - Math.Max(1, options.Length) * 44);
    private Rectangle Button(int i) => new(Left + 16, Top + 174 + i * 44, Width - 32, 40);
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < options.Length; i++) if (Button(i).Contains(x, y)) { options[i].Action(); return; }
        if (options.Length == 0 && Button(0).Contains(x, y)) cancel();
    }
    public override void receiveKeyPress(Keys key) { if (key == Keys.Escape) { cancel(); if (Game1.activeClickableMenu == this) Game1.exitActiveMenu(); } }
    public override void draw(SpriteBatch b)
    {
        drawTextureBox(b, Left, Top, Width, 190 + Math.Max(1, options.Length) * 44, Color.White);
        int inset = 20;
        if (npc?.Portrait != null)
        {
            b.Draw(npc.Portrait, new Rectangle(Left + 20, Top + 24, 96, 96), new Rectangle(0, 0, 64, 64), Color.White);
            inset = 136;
        }
        string shown = narration.Length == 0 ? text : text.Split('\n')[0] + "\n" + narration;
        b.DrawString(Game1.smallFont, Game1.parseText(shown, Game1.smallFont, Width - inset - 24), new Vector2(Left + inset, Top + 20), Game1.textColor);
        for (int i = 0; i < Math.Max(1, options.Length); i++)
        {
            Rectangle bounds = Button(i);
            drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White);
            b.DrawString(Game1.smallFont, options.Length == 0 ? "Close" : options[i].Label, new Vector2(bounds.X + 12, bounds.Y + 8), Game1.textColor);
        }
        drawMouse(b);
    }
}
