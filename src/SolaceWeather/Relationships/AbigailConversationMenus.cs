using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

internal sealed partial class NpcConversation
{
    internal static PlayerPortraits.PlayerPortraitService? PlayerPortraits { get; set; }
    private sealed class ConversationInput : NamingMenu
    {
        private readonly Action cancel;
        private readonly Func<QuestChoice[]> choices;
        private readonly Func<string[]> itemTerms;
        private readonly Action<string> choose;
        private QuestChoice[] visibleChoices;

        public ConversationInput(Action<string> send, Action cancel, Func<QuestChoice[]> choices, Func<string[]> itemTerms, Action<string> choose)
            : base(send.Invoke, "Talk to " + Speaker + " (Enter to send, Esc to exit)", "")
        {
            this.cancel = cancel; this.choices = choices; this.itemTerms = itemTerms; this.choose = choose;
            visibleChoices = choices();
            FilterInput = false;
            textBox.textLimit = 500;
            textBox.limitWidth = false;
            randomButton.bounds.X = -1000;
            LayoutInput();
        }

        private void LayoutInput()
        {
            textBox.X = Game1.uiViewport.Width / 2 - 192;
            textBox.Y = Math.Min(Game1.uiViewport.Height / 2, Game1.uiViewport.Height - visibleChoices.Length * 56 - 112);
            doneNamingButton.bounds.X = textBox.X + textBox.Width + 36;
            doneNamingButton.bounds.Y = textBox.Y - 8;
            textBoxCC.bounds.X = textBox.X; textBoxCC.bounds.Y = textBox.Y;
        }

        private Rectangle QuestBounds(int index) => ChoiceBounds(
            Math.Min(textBox.Y + 90, Game1.uiViewport.Height - visibleChoices.Length * 56 - 24) + index * 56);

        private int PortraitTop => Game1.uiViewport.Height < 600 ? 20 : Math.Max(20, textBox.Y - PortraitSize - 168);

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (RetryPortrait(x, y, PortraitTop)) return;
            if (PerksBounds(PortraitTop).Contains(x, y)) { textBox.Selected = false; choose("ui:services"); return; }
            for (int i = 0; i < visibleChoices.Length; i++)
                if (QuestBounds(i).Contains(x, y)) { textBox.Selected = false; choose(visibleChoices[i].Key); return; }
            base.receiveLeftClick(x, y, playSound);
        }

        public override void draw(SpriteBatch batch)
        {
            visibleChoices = choices();
            LayoutInput();
            if (Game1.uiViewport.Height < 600)
            {
                if (!Game1.options.showClearBackgrounds)
                    batch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * .75f);
                string hint = "Talk to " + Speaker + ": Enter to send, Esc to exit";
                batch.DrawString(Game1.smallFont, hint,
                    new Vector2((Game1.uiViewport.Width - Game1.smallFont.MeasureString(hint).X) / 2, 128), Color.Wheat);
                textBox.Draw(batch); doneNamingButton.draw(batch);
                DrawAbigailPortrait(batch, Game1.uiViewport.Width / 2, PortraitTop, "neutral");
            }
            else
            {
                base.draw(batch);
                DrawAbigailPortrait(batch, Game1.uiViewport.Width / 2, PortraitTop, "neutral");
            }
            QuestTextRenderer.DrawButton(batch, PerksBounds(PortraitTop), "Bond", Array.Empty<string>());
            string[] terms = itemTerms();
            for (int i = 0; i < visibleChoices.Length; i++)
                QuestTextRenderer.DrawButton(batch, QuestBounds(i), visibleChoices[i].Label, terms);
            drawMouse(batch);
        }
        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Escape) { textBox.Selected = false; cancel(); return; }
            base.receiveKeyPress(key);
        }
    }

    private static int PortraitSize => Game1.uiViewport.Height < 600 ? 64 : 128;

    private static Rectangle PerksBounds(int portraitTop) => new(
        Math.Min(Game1.uiViewport.Width - 120, Game1.uiViewport.Width / 2 + PortraitSize / 2 + 24),
        portraitTop + PortraitSize / 2 - 12, 104, 48);

    private static Rectangle PortraitRetryBounds(int top)
    {
        int left = Game1.uiViewport.Width / 2 - PortraitSize / 2 - PortraitSize - 40;
        return left >= 132
            ? new Rectangle(left - 120, top + PortraitSize / 2 - 12, 104, 40)
            : new Rectangle(left, Math.Max(8, top - 48), 104, 40);
    }

    private static bool RetryPortrait(int x, int y, int top)
    {
        if (PlayerPortraits?.GetStatus(Game1.player.UniqueMultiplayerID) != "Failed"
            || !PortraitRetryBounds(top).Contains(x, y)) return false;
        PlayerPortraits.Retry(Game1.player.UniqueMultiplayerID);
        return true;
    }

    private static void DrawAbigailPortrait(SpriteBatch batch, int centerX, int top, string expression)
    {
        Texture2D portrait = Game1.content.Load<Texture2D>("Portraits/" + Speaker);
        int columns = portrait.Width / 64;
        if (columns < 1 || portrait.Height < 64) return;
        int index = RomanceProfiles.Get(Speaker)?.PortraitIndex(expression) ?? 0;
        if (index >= columns * (portrait.Height / 64)) index = 0;
        int size = PortraitSize;
        var frame = new Rectangle(centerX - size / 2 - 12, top, size + 24, size + 24);
        IClickableMenu.drawTextureBox(batch, frame.X, frame.Y, frame.Width, frame.Height, Color.White);
        batch.Draw(portrait, new Rectangle(centerX - size / 2, top + 8, size, size),
            new Rectangle(index % columns * 64, index / columns * 64, 64, 64), Color.White);
        if (PlayerPortraits?.TryGetPortrait(Game1.player.UniqueMultiplayerID, out var playerPortrait, out var playerSource) == true)
        {
            int left = centerX - size / 2 - size - 40;
            if (left >= 12)
            {
                IClickableMenu.drawTextureBox(batch, left - 12, top, size + 24, size + 24, Color.White);
                batch.Draw(playerPortrait, new Rectangle(left, top + 8, size, size), playerSource, Color.White);
            }
        }
        if (PlayerPortraits?.GetStatus(Game1.player.UniqueMultiplayerID) == "Failed" && PortraitRetryBounds(top).X >= 12)
            QuestTextRenderer.DrawButton(batch, PortraitRetryBounds(top), "Retry", Array.Empty<string>());
    }
    private static Rectangle ChoiceBounds(int top) => new((Game1.uiViewport.Width - Math.Min(720, Game1.uiViewport.Width - 48)) / 2,
        top, Math.Min(720, Game1.uiViewport.Width - 48), 48);

    private sealed class DeliveryReplyBox : DialogueBox
    {
        private readonly Func<QuestChoice[]> choices;
        private readonly Action<string> choose;
        private readonly string[] itemTerms;
        private string? coloredPage;
        private List<ItemTextRange> ranges = new();
        private QuestChoice[] visibleChoices;
        private int layoutWidth, layoutHeight;
        private readonly string expression;

        internal DeliveryReplyBox(string text, Func<QuestChoice[]> choices, string[] itemTerms, Action<string> choose, string expression = "neutral") : base(text)
        {
            this.choices = choices; this.itemTerms = itemTerms; this.choose = choose;
            this.expression = CharacterReactions.Normalize(Speaker, expression);
            visibleChoices = choices();
            Reflow();
        }

        private void Reflow()
        {
            layoutWidth = Game1.uiViewport.Width; layoutHeight = Game1.uiViewport.Height;
            width = Math.Max(240, Math.Min(1120, layoutWidth - 96));
            int availableHeight = Math.Max(108, layoutHeight - 3 * 56 - 144 - PortraitSize - 32);
            var remaining = dialogues.ToArray();
            dialogues.Clear();
            foreach (string part in remaining)
                dialogues.AddRange(SpriteText.getStringBrokenIntoSectionsOfHeight(part, width - 16, availableHeight));
            if (dialogues.Count == 0) dialogues.Add("");
            PositionPage();
        }

        private void PositionPage()
        {
            width = Math.Max(240, Math.Min(1120, Game1.uiViewport.Width - 96));
            height = SpriteText.getHeightOfString(getCurrentString(), width - 16) + 4;
            x = (Game1.uiViewport.Width - width) / 2;
            y = Math.Max(40, Game1.uiViewport.Height - height - Math.Max(64, visibleChoices.Length * 56 + 64));
            if (dialogueIcon != null) dialogueIcon.position = new Vector2(x + width - 32, y + height - 28);
        }

        private Rectangle QuestBounds(int index) => ChoiceBounds(y + height + 40 + index * 56);

        public override void draw(SpriteBatch batch)
        {
            visibleChoices = choices();
            if (layoutWidth != Game1.uiViewport.Width || layoutHeight != Game1.uiViewport.Height) Reflow();
            if (!transitioning) PositionPage();
            base.draw(batch);
            if (!transitioning)
            {
                DrawAbigailPortrait(batch, Game1.uiViewport.Width / 2, Math.Max(20, y - PortraitSize - 40), expression);
                QuestTextRenderer.DrawButton(batch, PerksBounds(Math.Max(20, y - PortraitSize - 40)), "Bond", Array.Empty<string>());
                string page = getCurrentString();
                if (coloredPage != page) { coloredPage = page; ranges = QuestText.FindItems(page, itemTerms); }
                QuestTextRenderer.OverlayDialogue(batch, page, x + 8, y + 8, width, characterIndexInDialogue, ranges);
                for (int i = 0; i < visibleChoices.Length; i++)
                    QuestTextRenderer.DrawButton(batch, QuestBounds(i), visibleChoices[i].Label, itemTerms);
            }
            drawMouse(batch);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!transitioning && RetryPortrait(x, y, Math.Max(20, this.y - PortraitSize - 40))) return;
            if (!transitioning && PerksBounds(Math.Max(20, this.y - PortraitSize - 40)).Contains(x, y))
            { choose("ui:services"); return; }
            if (!transitioning)
                for (int i = 0; i < visibleChoices.Length; i++)
                    if (QuestBounds(i).Contains(x, y)) { choose(visibleChoices[i].Key); return; }
            base.receiveLeftClick(x, y, playSound);
        }
    }
}





