using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

internal sealed class PhoneMenu : IClickableMenu
{
    internal delegate bool PortraitLookup(long farmerId, out Texture2D texture, out Rectangle source);
    internal static PortraitLookup? PlayerPortraits { get; set; }
    private readonly PhoneService service;
    private readonly TextBox input;
    private readonly PhoneDictation dictation = new();
    private readonly List<(Rectangle Bounds, Action Click)> buttons = new();
    private readonly Dictionary<string, string> drafts = new();
    private float scale;
    private int left, top, scroll, contactOffset, shortcutPage, contentHeight;
    private int lastCount;
    private string hover = "";
    private bool dropdownOpen;
    private int dropdownOffset, dropdownSelection;
    private int refocusTicks;
    private readonly List<(Rectangle Bounds, Action Click)> dropdownButtons = new();
    private List<PhoneMessage>? laidOutMessages;
    private (PhoneMessage Message, string[] Lines, int Height)[] messageLayouts = Array.Empty<(PhoneMessage, string[], int)>();
    private bool released;
    internal string? Contact { get; private set; }
    private static readonly Color Edge = new(82, 153, 222), Ink = new(230, 240, 255), Muted = new(148, 181, 215);
    private static readonly Rectangle History = new(30, 162, 460, 366);

    internal PhoneMenu(PhoneService service)
    {
        this.service = service;
        input = new TextBox(Game1.content.Load<Texture2D>("LooseSprites/textBox"), null, Game1.smallFont, Ink)
        { Text = "", textLimit = 500, limitWidth = false };
        input.OnEnterPressed += _ => Send(input.Text);
        Reflow();
    }

    private void Reflow()
    {
        scale = Math.Min(1.15f, Math.Min((Game1.uiViewport.Width - 24) / 520f, (Game1.uiViewport.Height - 24) / 780f));
        scale = Math.Max(.25f, scale);
        left = (Game1.uiViewport.Width - (int)(520 * scale)) / 2;
        top = (Game1.uiViewport.Height - (int)(780 * scale)) / 2;
        xPositionOnScreen = left; yPositionOnScreen = top; width = (int)(520 * scale); height = (int)(780 * scale);
        input.X = R(new(30, 690, 340, 56)).X; input.Y = R(new(30, 690, 340, 56)).Y;
        input.Width = (int)(340 * scale);
    }
    private Rectangle R(Rectangle r) => new(left + (int)(r.X * scale), top + (int)(r.Y * scale), Math.Max(1, (int)(r.Width * scale)), Math.Max(1, (int)(r.Height * scale)));
    private void Fill(SpriteBatch b, Rectangle r, Color color) => b.Draw(Game1.staminaRect, R(r), color);
    private void Panel(SpriteBatch b, Rectangle r, Color color)
    {
        Fill(b, new(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), color);
        Fill(b, new(r.X, r.Y, r.Width, 2), Edge * .8f);
        Fill(b, new(r.X, r.Bottom - 2, r.Width, 2), Edge * .8f);
        Fill(b, new(r.X, r.Y + 2, 2, r.Height - 4), Edge * .8f);
        Fill(b, new(r.Right - 2, r.Y + 2, 2, r.Height - 4), Edge * .8f);
    }
    private void Text(SpriteBatch b, string text, int x, int y, Color color, float size = .85f)
        => b.DrawString(Game1.smallFont, text, new(left + x * scale, top + y * scale), color, 0, Vector2.Zero, size * scale, SpriteEffects.None, 0);
    private static string Fit(string text, int width, float size = .85f)
    {
        if (Game1.smallFont.MeasureString(text).X * size <= width) return text;
        while (text.Length > 0 && Game1.smallFont.MeasureString(text + "...").X * size > width) text = text[..^1];
        return text + "...";
    }
    private static string Wrap(string text, int width, float size = .85f)
    {
        // Native word wrapping can leave a long URL or unbroken word wider than its bubble.
        var lines = new List<string>();
        string line = "";
        foreach (char character in text)
        {
            if (character == '\n') { lines.Add(line); line = ""; continue; }
            line += character;
            if (Game1.smallFont.MeasureString(line).X * size <= width) continue;
            int split = line.LastIndexOf(' ');
            if (split <= 0) split = line.Length - 1;
            lines.Add(line[..split]); line = line[split..].TrimStart();
        }
        if (line.Length > 0 || lines.Count == 0) lines.Add(line);
        return string.Join("\n", lines);
    }
    private void Button(SpriteBatch b, Rectangle r, string label, Action click, bool enabled = true)
    {
        Panel(b, r, enabled ? new Color(30, 86, 149) : new Color(35, 49, 70));
        Text(b, Fit(label, r.Width - 16), r.X + 8, r.Y + (r.Height - 24) / 2, enabled ? Ink : Muted);
        if (enabled) buttons.Add((R(r), click));
    }
    private void Portrait(SpriteBatch b, string name, Rectangle r)
    {
        var texture = Game1.content.Load<Texture2D>("Portraits/" + name);
        b.Draw(texture, R(r), new Rectangle(0, 0, 64, 64), Color.White);
    }
    internal void Select(string name)
    {
        if (!service.Contacts.Contains(name)) return;
        SaveDraft(); dictation.Stop();
        Contact = name; dropdownOpen = false; input.Text = drafts.GetValueOrDefault(name, ""); scroll = 0; shortcutPage = 0; lastCount = 0;
        service.State!.Thread(name).Unread = false;
        Focus();
    }
    private void Focus()
    {
        if (Contact == null || !service.CanText(Contact)) return;
        input.Selected = true; Game1.keyboardDispatcher.Subscriber = input;
    }
    private void SaveDraft() { if (Contact != null) drafts[Contact] = input.Text; }
    private void Inbox()
    {
        SaveDraft(); dictation.Stop(); Contact = null; dropdownOpen = false; input.Selected = false;
        if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, input)) Game1.keyboardDispatcher.Subscriber = null;
    }
    private void Send(string text)
    {
        if (Contact == null) return;
        dictation.Stop();
        if (service.Send(Contact, text)) { input.Text = ""; drafts[Contact] = ""; scroll = 0; }
        Focus();
    }
    internal void ReleaseInput()
    {
        if (released) return;
        released = true; dictation.Dispose(); input.Selected = false;
        if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, input)) Game1.keyboardDispatcher.Subscriber = null;
    }
    public override void update(GameTime time)
    {
        base.update(time);
        if (refocusTicks > 0 && --refocusTicks == 0) Focus();
        string? words;
        while ((words = dictation.Poll()) != null)
        {
            string draft = (input.Text + " " + words).Trim();
            input.Text = draft.Length <= 500 ? draft : draft[..500];
        }
        if (Contact != null && service.State != null)
        {
            if (!service.CanText(Contact))
            {
                dictation.Stop(); input.Selected = false;
                if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, input)) Game1.keyboardDispatcher.Subscriber = null;
            }
            var thread = service.State.Thread(Contact);
            thread.Unread = false;
            if (lastCount != thread.Messages.Count) { lastCount = thread.Messages.Count; scroll = 0; }
        }
    }
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (dropdownOpen)
        {
            foreach (var button in dropdownButtons.ToArray()) if (button.Bounds.Contains(x, y)) { button.Click(); return; }
            dropdownOpen = false; Focus(); return;
        }
        foreach (var button in buttons.ToArray()) if (button.Bounds.Contains(x, y)) { button.Click(); return; }
        if (Contact != null && R(new(30, 690, 340, 56)).Contains(x, y)) Focus();
    }
    public override void receiveKeyPress(Keys key)
    {
        if (dropdownOpen)
        {
            if (key == Keys.Escape) { dropdownOpen = false; Focus(); return; }
            if (key == Keys.Up || key == Keys.Down)
            {
                dropdownSelection = Math.Clamp(dropdownSelection + (key == Keys.Up ? -1 : 1), 0, Math.Max(0, service.Contacts.Length - 1));
                dropdownOffset = Math.Clamp(dropdownSelection - 5, 0, Math.Max(0, service.Contacts.Length - 6));
            }
            if (key == Keys.Enter && service.Contacts.Length > 0)
            {
                Select(service.Contacts[dropdownSelection]);
                // The keyboard dispatcher may still handle this same Enter after the menu.
                input.Selected = false;
                if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, input)) Game1.keyboardDispatcher.Subscriber = null;
                refocusTicks = 2;
            }
            return;
        }
        if (key == Keys.Escape) { service.Close(); return; }
        if (key == Keys.PageUp) receiveScrollWheelAction(120);
        if (key == Keys.PageDown) receiveScrollWheelAction(-120);
        // TextBox owns Enter and character editing through the keyboard dispatcher.
    }
    public override void receiveScrollWheelAction(int direction)
    {
        if (dropdownOpen) dropdownOffset = Math.Clamp(dropdownOffset + (direction > 0 ? -1 : 1), 0, Math.Max(0, service.Contacts.Length - 6));
        else if (Contact == null) contactOffset = Math.Clamp(contactOffset + (direction > 0 ? -1 : 1), 0, Math.Max(0, service.Contacts.Length - 7));
        else scroll = Math.Clamp(scroll + (direction > 0 ? 90 : -90), 0, Math.Max(0, contentHeight - History.Height));
    }
    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Reflow();
    public override void performHoverAction(int x, int y)
    {
        hover = "";
        if (Contact == null || dropdownOpen) return;
        var thread = service.State!.Thread(Contact);
        for (int i = 0; i < 4 && shortcutPage * 4 + i < thread.Shortcuts.Count; i++)
            if (R(new(30 + i % 2 * 234, 582 + i / 2 * 40, 226, 34)).Contains(x, y))
                hover = "Send again: " + thread.Shortcuts[shortcutPage * 4 + i];
        if (R(new(378, 690, 48, 56)).Contains(x, y)) hover = dictation.Status.Length > 0 ? dictation.Status : "Dictate locally with your microphone. Review the draft before sending.";
        if (R(new(30, 749, 460, 25)).Contains(x, y)) hover = service.Notice.Length > 0 ? service.Notice : dictation.Status;
    }
    public override void draw(SpriteBatch b)
    {
        Reflow(); buttons.Clear();
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * .28f);
        Panel(b, new(0, 0, 520, 780), new Color(17, 38, 66) * .6f);
        Panel(b, new(10, 10, 500, 760), new Color(8, 27, 58) * .65f);
        Fill(b, new(205, 18, 110, 7), new Color(2, 12, 25));
        Text(b, $"{Game1.currentSeason} {Game1.dayOfMonth}  |  {Game1.getTimeOfDayString(Game1.timeOfDay)}", 30, 37, Muted, .75f);
        Button(b, new(444, 32, 46, 38), "X", service.Close);
        if (Contact == null) DrawInbox(b); else DrawChat(b);
        if (dropdownOpen) DrawDropdown(b);
        if (hover.Length > 0) drawHoverText(b, Game1.parseText(hover, Game1.smallFont, 420), Game1.smallFont);
        drawMouse(b);
    }
    private void DrawInbox(SpriteBatch b)
    {
        Text(b, "Messages", 30, 86, Ink, 1.35f);
        Text(b, "Numbers you've exchanged", 30, 126, Muted);
        string[] contacts = service.Contacts;
        contactOffset = Math.Min(contactOffset, Math.Max(0, contacts.Length - 7));
        for (int i = 0; i < Math.Min(7, contacts.Length - contactOffset); i++)
        {
            string name = contacts[i + contactOffset]; int y = 171 + i * 72;
            var thread = service.State!.Threads.GetValueOrDefault(name);
            Panel(b, new(30, y, 460, 66), new Color(21, 49, 84));
            Portrait(b, name, new(36, y + 5, 56, 56));
            Text(b, name + (service.BlockReason(name) != "" ? "  Blocked" : thread?.Unread == true ? "  * New" : ""), 105, y + 4, Ink);
            var last = thread?.Messages.LastOrDefault();
            Text(b, Fit(last == null ? "Start a conversation" : (last.Outgoing ? "You: " : "") + last.Text, 370, .7f), 105, y + 34, Muted, .7f);
            buttons.Add((R(new(30, y, 460, 66)), () => Select(name)));
        }
        if (contacts.Length == 0) Text(b, Wrap("Talk to villagers in person and accept their offer to exchange numbers. Your contacts will appear here.", 440), 30, 190, Muted);
        Text(b, "Scroll for contacts  |  Esc to close", 30, 707, Muted, .75f);
        Text(b, "Phone history belongs to this farm.", 30, 736, Muted, .7f);
    }
    private void DrawChat(SpriteBatch b)
    {
        string name = Contact!;
        var thread = service.State!.Thread(name);
        bool canText = service.CanText(name);
        Button(b, new(30, 88, 44, 48), "", Inbox);
        Fill(b, new(43, 110, 22, 4), Ink);
        for (int row = 0; row < 6; row++) Fill(b, new(42 + Math.Abs(row - 3) * 3, 103 + row * 3, 5, 3), Ink);
        Portrait(b, name, new(90, 80, 64, 64));
        Button(b, new(164, 80, 326, 38), name, ToggleDropdown);
        for (int row = 0; row < 4; row++) Fill(b, new(465 + row * 2, 95 + row * 2, 14 - row * 4, 2), Ink);
        Text(b, !canText ? "Blocked - messages unavailable" : service.Busy ? "Waiting for a reply..." : "Phone conversation", 170, 124, Muted, .7f);
        Fill(b, new(30, 151, 460, 2), Edge * .65f);
        DrawMessages(b, thread);
        if (scroll > 0) Button(b, new(381, 493, 105, 32), "Latest", () => scroll = 0);
        Fill(b, new(30, 539, 460, 2), Edge * .65f);
        Text(b, "Quick messages", 30, 549, Edge);
        int pages = Math.Max(1, (thread.Shortcuts.Count + 3) / 4);
        shortcutPage = Math.Clamp(shortcutPage, 0, pages - 1);
        Button(b, new(360, 546, 130, 30), $"{shortcutPage + 1}/{pages}  Next", () => shortcutPage = (shortcutPage + 1) % pages, pages > 1);
        for (int i = 0; i < 4 && shortcutPage * 4 + i < thread.Shortcuts.Count; i++)
        {
            string text = thread.Shortcuts[shortcutPage * 4 + i];
            Button(b, new(30 + i % 2 * 234, 582 + i / 2 * 40, 226, 34), text, () => Send(text), !service.Busy && canText);
        }
        if (thread.Shortcuts.Count == 0) Text(b, "Texts you send appear here to send again.", 30, 594, Muted, .7f);
        bool failed = thread.Messages.Any(m => m.Status == "failed");
        if (failed)
        {
            Text(b, "Text not delivered", 30, 666, new Color(255, 195, 150), .7f);
            Button(b, new(230, 657, 100, 28), "Retry", () => service.Retry(name), !service.Busy && canText);
            Button(b, new(340, 657, 150, 28), "Dismiss", () => service.Dismiss(name), !service.Busy);
        }
        else Text(b, Fit(dictation.Listening ? "Listening..." : $"{input.Text.Length}/500  |  Scroll to read earlier texts", 455, .65f), 30, 663, Muted, .65f);
        Panel(b, new(30, 690, 340, 56), new Color(27, 49, 81));
        string draft = input.Text;
        while (draft.Length > 0 && Game1.smallFont.MeasureString(draft).X * .8f > 312) draft = draft[1..];
        Text(b, !canText ? "You can't message this contact." : draft.Length == 0 ? "Type a message..." : draft, 42, 705, draft.Length == 0 ? Muted : Ink, .8f);
        if (input.Selected && Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000 < 500)
            Fill(b, new(42 + (int)(Game1.smallFont.MeasureString(draft).X * .8f), 704, 2, 25), Ink);
        Button(b, new(378, 690, 48, 56), "", () => { Focus(); if (dictation.Listening) dictation.Stop(); else dictation.Start(); }, canText);
        var micColor = dictation.Listening ? new Color(255, 168, 156) : Ink;
        Fill(b, new(398, 702, 9, 20), micColor); Fill(b, new(393, 714, 3, 12), micColor);
        Fill(b, new(409, 714, 3, 12), micColor); Fill(b, new(396, 725, 13, 3), micColor);
        Fill(b, new(401, 728, 3, 7), micColor); Fill(b, new(396, 735, 13, 3), micColor);
        Button(b, new(434, 690, 56, 56), "", () => Send(input.Text), !service.Busy && !failed && canText);
        for (int row = 0; row < 13; row++) Fill(b, new(450, 706 + row * 2, 26 - Math.Abs(row - 6) * 3, 2), !service.Busy && !failed && canText ? Ink : Muted);
        string notice = !canText ? "Blocked while this relationship is separated or ended. History is kept." : service.Notice.Length > 0 ? service.Notice : dictation.Status;
        Text(b, Fit(notice, 460, .58f), 30, 749, Muted, .58f);
    }

    private void ToggleDropdown()
    {
        hover = "";
        dropdownOpen = !dropdownOpen;
        if (!dropdownOpen) { Focus(); return; }
        dictation.Stop(); input.Selected = false;
        if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, input)) Game1.keyboardDispatcher.Subscriber = null;
        dropdownSelection = Math.Max(0, Array.IndexOf(service.Contacts, Contact));
        dropdownOffset = Math.Clamp(dropdownSelection - 5, 0, Math.Max(0, service.Contacts.Length - 6));
    }
    private void DrawDropdown(SpriteBatch b)
    {
        dropdownButtons.Clear();
        string[] contacts = service.Contacts;
        dropdownOffset = Math.Clamp(dropdownOffset, 0, Math.Max(0, contacts.Length - 6));
        int count = Math.Min(6, contacts.Length - dropdownOffset);
        Panel(b, new(164, 120, 326, count * 44 + 36), new Color(10, 27, 51));
        for (int i = 0; i < count; i++)
        {
            int index = dropdownOffset + i; string name = contacts[index];
            var row = new Rectangle(168, 124 + i * 44, 318, 42);
            Fill(b, row, name == Contact || index == dropdownSelection ? new Color(34, 86, 143) : new Color(22, 45, 74));
            Text(b, name + (service.BlockReason(name) != "" ? " (blocked)" : ""), row.X + 10, row.Y + 8, Ink);
            dropdownButtons.Add((R(row), () => Select(name)));
        }
        Text(b, "Scroll / arrows to choose; Enter to open", 174, 129 + count * 44, Muted, .6f);
    }
    private void DrawMessages(SpriteBatch b, PhoneThread thread)
    {
        if (thread.Messages.Count == 0) { Text(b, Contact != null && !service.CanText(Contact) ? "This contact has blocked messages." : "Say hello to start your conversation.", 42, 190, Muted, .75f); return; }
        if (!ReferenceEquals(laidOutMessages, thread.Messages) || messageLayouts.Length != thread.Messages.Count)
        {
            laidOutMessages = thread.Messages;
            messageLayouts = thread.Messages.Select(m =>
            {
                string[] lines = Wrap(m.Text, 333).Split('\n');
                return (Message: m, Lines: lines, Height: lines.Length * 24 + 40);
            }).ToArray();
        }
        contentHeight = messageLayouts.Sum(l => l.Height + 14);
        scroll = Math.Clamp(scroll, 0, Math.Max(0, contentHeight - History.Height));
        int y = History.Y + Math.Min(0, History.Height - contentHeight) + scroll;
        foreach (var layout in messageLayouts)
        {
            var m = layout.Message; int x = m.Outgoing ? 125 : 34;
            var bubble = new Rectangle(x, y, 357, layout.Height);
            var clipped = Rectangle.Intersect(bubble, History);
            if (clipped.Height > 0)
            {
                var avatar = new Rectangle(65, y + 4, 48, 48);
                if (m.Outgoing && avatar.Top >= History.Top && avatar.Bottom <= History.Bottom
                    && PlayerPortraits?.Invoke(Game1.player.UniqueMultiplayerID, out var portrait, out var source) == true)
                    b.Draw(portrait, R(avatar), source, Color.White);
                Fill(b, clipped, m.Outgoing ? new Color(39, 109, 186) : new Color(34, 59, 96));
                for (int line = 0; line < layout.Lines.Length; line++)
                {
                    int textY = y + 10 + line * 24;
                    if (textY >= History.Top && textY + 24 <= History.Bottom) Text(b, layout.Lines[line], x + 12, textY, Ink);
                }
                int stampY = y + layout.Height - 24;
                if (stampY >= History.Top && stampY + 20 <= History.Bottom)
                    Text(b, $"Day {m.Day + 1}  {Game1.getTimeOfDayString(m.Time)}" + (m.Outgoing ? "  " + (m.Status == "pending" ? "Waiting..." : m.Status == "failed" ? "Not delivered" : "Sent") : ""), x + 12, stampY, Muted, .6f);
            }
            y += layout.Height + 14;
        }
    }
}
