using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using SolaceWeather.Integration;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TokenizableStrings;

namespace SolaceWeather.Relationships;

internal sealed class TownChatter
{
    private const string SaveKey = "town-chatter";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25), MaxResponseContentBufferSize = 32768 };
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly WeatherRuntime weather;
    private readonly Func<bool> conversationBusy;
    private TownChatterState? state;
    private Task<ChatterReply>? pending;
    private CancellationTokenSource? cancel;
    private NPC? first, second, speaking;
    private ChatterContext? snapshot;
    private ChatterReply? exchange;
    private int lineIndex;
    private double elapsed, lineEnds, nextAttempt;
    private string ownBubble = "";
    internal bool Ready => state != null && Context.IsWorldReady && !Context.IsMultiplayer && config.EnableTownChatter && config.EnableAbigailMemory;
    internal bool Busy => pending != null || exchange != null;
    internal TownChatterState? State => state;
    internal object PublicMemories(string name) => new { Heard = state?.ForCharacter(name) ?? Array.Empty<HeardChatter>(),
        Rule = "These are public words the farmer heard these speakers say, not proof of extra events. Do not invent farmer participation or private disclosures." };

    internal TownChatter(IModHelper helper, IMonitor monitor, ModConfig config, WeatherRuntime weather, Func<bool> conversationBusy)
    {
        this.helper = helper; this.monitor = monitor; this.config = config; this.weather = weather; this.conversationBusy = conversationBusy;
        helper.Events.GameLoop.SaveLoaded += (_, _) => Load();
        helper.Events.GameLoop.Saving += (_, _) => { Stop(); if (Ready && state!.IsValid(Game1.player.UniqueMultiplayerID)) helper.Data.WriteSaveData(SaveKey, state); };
        helper.Events.GameLoop.DayEnding += (_, _) => Stop();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { Stop(); state = null; };
        helper.Events.Player.Warped += (_, e) => { if (e.IsLocalPlayer) Stop(); };
        helper.Events.GameLoop.UpdateTicked += (_, _) => Tick();
        helper.Events.Display.RenderedWorld += (_, e) => DrawBubble(e.SpriteBatch);
    }
    private void Load()
    {
        Stop(); state = null; elapsed = 0; nextAttempt = 10;
        if (Context.IsMultiplayer || !config.EnableAbigailMemory) return;
        try
        {
            var loaded = helper.Data.ReadSaveData<TownChatterState>(SaveKey);
            if (loaded != null && !loaded.IsValid(Game1.player.UniqueMultiplayerID)) throw new InvalidDataException();
            state = loaded ?? new() { FarmerId = Game1.player.UniqueMultiplayerID };
        }
        catch { monitor.Log("Town chatter save unavailable; existing data preserved.", LogLevel.Warn); }
    }
    private bool Available() => Ready && config.EnableAbigailAi && Game1.currentLocation?.Name == "Town" && Game1.currentLocation.IsOutdoors
        && !Game1.eventUp && Game1.activeClickableMenu == null && Context.IsPlayerFree && Game1.shouldTimePass() && !conversationBusy();
    private int BubbleTime(NPC npc) => helper.Reflection.GetField<int>(npc, "textAboveHeadTimer").GetValue();
    private bool Eligible(NPC npc) => RomanceRules.IsCandidate(npc.Name) && npc.currentLocation == Game1.currentLocation
        && !npc.IsInvisible && !npc.isSleeping.Value && !npc.isMoving() && !npc.isEmoting
        && Vector2.Distance(npc.Tile, Game1.player.Tile) <= 6
        && new Rectangle(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height).Contains(npc.StandingPixel)
        && BubbleTime(npc) <= 0;
    private bool PairAvailable() => first != null && second != null && Eligible(first) && Eligible(second)
        && Vector2.Distance(first.Tile, second.Tile) <= 3 && snapshot?.Day == Game1.Date.TotalDays
        && snapshot.Facts.FirstOrDefault(f => f.Id == "weather")?.Fact == WeatherFact().Fact;

    private void Tick()
    {
        elapsed += Game1.currentGameTime?.ElapsedGameTime.TotalSeconds ?? 1d / 60;
        try
        {
            if (!Available()) { if (Busy) Stop(); return; }
            if (Busy && !PairAvailable()) { Stop(); return; }
            if (pending != null)
            {
                if (!pending.IsCompleted) return;
                var result = pending.GetAwaiter().GetResult(); pending = null;
                cancel?.Dispose(); cancel = null; exchange = result; lineIndex = 0; ShowLine(); return;
            }
            if (exchange != null)
            {
                if (elapsed < lineEnds) return;
                ClearBubble();
                if (++lineIndex < exchange.Lines.Length) ShowLine();
                else { state!.Record(snapshot!.Day, "Town", exchange.FactId, exchange.Lines); Stop(); }
                return;
            }
            if (elapsed < nextAttempt) return;
            // Scan at most once per second; a failed pair does not consume a paid attempt.
            nextAttempt = elapsed + 1;
            var nearby = Game1.currentLocation.characters.Where(Eligible).OrderBy(n => n.Name).ToArray();
            for (int a = 0; a < nearby.Length; a++)
                for (int b = a + 1; b < nearby.Length; b++)
                    if (Vector2.Distance(nearby[a].Tile, nearby[b].Tile) <= 3 && Start(nearby[a], nearby[b])) return;
        }
        catch { Stop(); nextAttempt = elapsed + 60; monitor.Log("A town chatter attempt was skipped; no dialogue or memory was invented.", LogLevel.Trace); }
    }
    private bool Start(NPC a, NPC b)
    {
        string key = Environment.GetEnvironmentVariable("GEMINI_API_KEY", EnvironmentVariableTarget.User) ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        if (key.Length == 0 || !Available() || Busy || !Eligible(a) || !Eligible(b) || Vector2.Distance(a.Tile, b.Tile) > 3) return false;
        var context = BuildContext(a.Name, b.Name);
        if (!state!.TryAttempt(a.Name, b.Name, Game1.Date.TotalDays, WeatherRuntime.ToMinutes(Game1.timeOfDay))) return false;
        first = a; second = b; snapshot = context; nextAttempt = elapsed + 60;
        cancel = new CancellationTokenSource(); pending = new GeminiChatter(Http).Generate(key, config.GeminiModel, context, cancel.Token);
        return true;
    }
    private ChatterFact WeatherFact()
    {
        string current = Game1.currentLocation.IsGreenRainingHere() ? "Green rain" : weather.Enabled ? weather.GetCurrent(Region.Town).Kind.ToString()
            : Game1.currentLocation.IsLightningHere() ? "Storm" : Game1.currentLocation.IsRainingHere() ? "Rain"
            : Game1.currentLocation.IsSnowingHere() ? "Snow" : Game1.currentLocation.IsDebrisWeatherHere() ? "Wind" : "Clear";
        return new("weather", "The current weather here in Town is " + current + ". This is current weather, not a forecast.");
    }
    private ChatterContext BuildContext(string a, string b)
    {
        var facts = new List<ChatterFact> { WeatherFact() };
        if (Game1.MasterPlayer.mailReceived.Contains("ccIsComplete")) facts.Add(new("community-center", "The Community Center has been restored. Its restoration date and today's activities are not known."));
        if (Game1.MasterPlayer.mailReceived.Contains("JojaMember")) facts.Add(new("joja", "The town's Joja membership route is active. No new construction or event is established by this fact."));
        for (int offset = 0; offset <= 7; offset++)
        {
            var date = new WorldDate(Game1.Date) { TotalDays = Game1.Date.TotalDays + offset };
            string season = date.Season.ToString().ToLowerInvariant();
            if (Utility.isFestivalDay(date.DayOfMonth, date.Season))
            {
                var data = helper.GameContent.Load<Dictionary<string, string>>("Data/Festivals/" + season + date.DayOfMonth);
                if (data.TryGetValue("name", out var name)) facts.Add(new("festival:" + season + date.DayOfMonth, $"The calendar lists {name} on {season} {date.DayOfMonth}, {(offset == 0 ? "today" : $"in {offset} days")}. It has not been witnessed here."));
            }
            if (Utility.TryGetPassiveFestivalDataForDay(date.DayOfMonth, date.Season, null, out var id, out var passive)
                && passive?.ShowOnCalendar == true && !facts.Any(f => f.Id == "passive:" + id))
                facts.Add(new("passive:" + id, $"The calendar lists {TokenParser.ParseText(passive.DisplayName)} on {season} {date.DayOfMonth}, {(offset == 0 ? "today" : $"in {offset} days")}. Do not invent attendance or activities."));
        }
        return new(a, b, Game1.Date.TotalDays, Game1.currentSeason, Game1.dayOfMonth, "Town", facts.ToArray());
    }
    private void ShowLine()
    {
        var line = exchange!.Lines[lineIndex]; speaking = lineIndex == 0 ? first : second; ownBubble = line.Text;
        int duration = Math.Clamp(2500 + line.Text.Length * 35, 4000, 6000);
        lineEnds = elapsed + duration / 1000d;
    }
    private void DrawBubble(SpriteBatch b)
    {
        if (!Ready || speaking == null || ownBubble.Length == 0 || Game1.eventUp || Game1.activeClickableMenu != null) return;
        // Integer-scale pixel glyphs stay legible over moving rain and busy street textures.
        float oldZoom = StardewValley.BellsAndWhistles.SpriteText.fontPixelZoom;
        try
        {
            StardewValley.BellsAndWhistles.SpriteText.fontPixelZoom = 2f;
            DrawReadableBubble(b);
        }
        finally { StardewValley.BellsAndWhistles.SpriteText.fontPixelZoom = oldZoom; }
    }
    private void DrawReadableBubble(SpriteBatch b)
    {
        const int textWidth = 420;
        int width = Math.Min(textWidth, StardewValley.BellsAndWhistles.SpriteText.getWidthOfString(ownBubble, textWidth)) + 32;
        int height = StardewValley.BellsAndWhistles.SpriteText.getHeightOfString(ownBubble, textWidth) + 24;
        Vector2 anchor = Game1.GlobalToLocal(new Vector2(speaking!.StandingPixel.X, speaking.StandingPixel.Y - 100));
        int x = Math.Clamp((int)anchor.X - width / 2, 8, Math.Max(8, Game1.viewport.Width - width - 8));
        int y = Math.Max(8, (int)anchor.Y - height);
        var shade = new Color(10, 20, 28) * .45f;
        const int radius = 12;
        for (int row = 0; row < height; row++)
        {
            int edge = Math.Min(row, height - row - 1);
            int inset = edge >= radius ? 0 : radius - (int)Math.Sqrt(radius * radius - (radius - edge) * (radius - edge));
            b.Draw(Game1.staminaRect, new Rectangle(x + inset, y + row, width - inset * 2, 1), shade);
        }
        int tail = Math.Clamp((int)anchor.X, x + 12, x + width - 12);
        for (int row = 0; row < 9; row++) b.Draw(Game1.staminaRect, new Rectangle(tail - 9 + row, y + height + row, 18 - row * 2, 1), shade);
        foreach (var offset in new[] { new Point(-2, 0), new Point(2, 0), new Point(0, -2), new Point(0, 2) })
            StardewValley.BellsAndWhistles.SpriteText.drawString(b, ownBubble, x + 16 + offset.X, y + 12 + offset.Y, width: textWidth, color: Color.Black);
        StardewValley.BellsAndWhistles.SpriteText.drawString(b, ownBubble, x + 16, y + 12, width: textWidth, color: Color.White);
    }
    private void ClearBubble()
    {
        speaking = null; ownBubble = "";
    }
    private void Stop()
    {
        cancel?.Cancel(); cancel?.Dispose(); cancel = null;
        if (pending != null) _ = pending.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        pending = null; exchange = null; ClearBubble(); first = second = null; snapshot = null;
    }
}
