using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Shirts;
using StardewValley.GameData.Pants;

namespace SolaceWeather.Tailoring;

internal sealed partial class TailoringService
{
    internal const string SaveKey = "David.SolaceWeather/TailoringV1";
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<bool> canCraft, canRecover;
    private readonly Action<int, string> crafted;
    private TailoringSaveState? state;
    private long farmerId;
    private readonly Dictionary<string, TailoringOrder> catalog = new();
    private readonly Dictionary<string, byte[]> images = new();
    private readonly HashSet<string> damaged = new();
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };
    private Task<byte[]>? request;
    private CancellationTokenSource? cancellation;
    private string requestId = "";
    private int lastHealth;
    private double damageUntil;
    internal TailoringSaveState? State => state;
    private bool Ready => Context.IsWorldReady && !Context.IsMultiplayer && state != null && Game1.player.UniqueMultiplayerID == farmerId;
    private static int Today => Game1.Date.TotalDays;
    internal TailoringService(IModHelper helper, IMonitor monitor, Func<bool> canCraft, Func<bool> canRecover, Action<int, string> crafted)
    {
        this.helper = helper; this.monitor = monitor; this.canCraft = canCraft; this.canRecover = canRecover; this.crafted = crafted;
        helper.Events.Content.AssetRequested += Assets;
        helper.Events.Specialized.LoadStageChanged += (_, e) => { if (e.NewStage == LoadStage.SaveParsed) Restore(SaveGame.loaded.player); };
        helper.Events.GameLoop.SaveLoaded += (_, _) =>
        {
            if (Context.IsMultiplayer) { Reset(); return; }
            if (state == null && !Game1.player.modData.ContainsKey(SaveKey)) Restore(Game1.player);
            lastHealth = Game1.player.health; damageUntil = 0;
        };
        helper.Events.GameLoop.Saving += (_, _) => Persist();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { Reset(); Publish(); };
        helper.Events.GameLoop.UpdateTicked += (_, _) => SafeTick();
    }
    private static string Route(TailoringOrder order) => "David.SolaceWeather/Tailoring/" + order.Id;
    private void Restore(Farmer farmer)
    {
        Reset(); farmerId = farmer.UniqueMultiplayerID;
        try
        {
            if (farmer.modData.TryGetValue(SaveKey, out var raw))
            {
                if (raw.Length > 3000000) throw new InvalidDataException();
                state = JsonSerializer.Deserialize<TailoringSaveState>(raw);
                if (state?.IsValid(farmerId) != true) throw new InvalidDataException();
            }
            else state = new() { FarmerId = farmerId };
            if (state.Pending?.Status == "generating") state.Fail(state.Pending.Id, "interrupted");
            foreach (var order in state.Orders.Where(o => o.Status is "ready" or "fulfilled")) Register(order);
        }
        catch { state = null; monitor.Log("Custom tailoring data is unavailable. The saved record is preserved; no new orders or generation will run.", LogLevel.Warn); }
        Publish();
    }
    private Texture2D Template(string slot) => helper.ModContent.Load<Texture2D>("assets/tailoring/" + slot + ".png");
    private void Register(TailoringOrder order)
    {
        catalog[order.ItemId] = order;
        try
        {
            if (!order.HasValidImage()) throw new InvalidDataException();
            var bytes = Convert.FromBase64String(order.Png);
            using var stream = new MemoryStream(bytes);
            using var texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
            var template = Template(order.Slot);
            if (texture.Width != template.Width || texture.Height != template.Height) throw new InvalidDataException();
            var actual = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
            var expected = new Microsoft.Xna.Framework.Color[actual.Length]; texture.GetData(actual); template.GetData(expected);
            if (actual.Where((p, i) => p.A != expected[i].A).Any()) throw new InvalidDataException();
            images[Route(order)] = bytes; damaged.Remove(order.Id);
        }
        catch { damaged.Add(order.Id); monitor.Log("A saved custom garment has damaged artwork. Its item ID is retained with the fixed template; no image request was made.", LogLevel.Warn); }
    }
    private void Assets(object? sender, AssetRequestedEventArgs e)
    {
        var match = catalog.Values.FirstOrDefault(o => e.NameWithoutLocale.IsEquivalentTo(Route(o)));
        if (match != null)
        {
            if (images.TryGetValue(Route(match), out var bytes)) e.LoadFrom(() => { using var stream = new MemoryStream(bytes); return Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream); }, AssetLoadPriority.Exclusive);
            else e.LoadFromModFile<Texture2D>("assets/tailoring/" + match.Slot + ".png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shirts")) e.Edit(asset =>
        {
            var rows = asset.AsDictionary<string, ShirtData>().Data;
            foreach (var o in catalog.Values.Where(o => o.Slot == "shirt")) rows[o.ItemId] = new ShirtData { Name = o.DisplayName, DisplayName = o.DisplayName, Description = Description(o), Texture = Route(o), SpriteIndex = 0, DefaultColor = o.Color, CanBeDyed = true, HasSleeves = true, Price = 0, CanChooseDuringCharacterCustomization = false };
        });
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Pants")) e.Edit(asset =>
        {
            var rows = asset.AsDictionary<string, PantsData>().Data;
            foreach (var o in catalog.Values.Where(o => o.Slot == "pants")) rows[o.ItemId] = new PantsData { Name = o.DisplayName, DisplayName = o.DisplayName, Description = Description(o), Texture = Route(o), SpriteIndex = 0, DefaultColor = o.Color, CanBeDyed = true, Price = 0, CanChooseDuringCharacterCustomization = false };
        });
    }
    private static string Description(TailoringOrder o) => "A custom fabric design made with Emily. " + (o.Recipe == "mend" ? "While worn: 1 health per active minute, up to 20 per day." : o.Recipe == "renew" ? "While worn: 2 energy per active minute, up to 40 per day." : "Wear it your way.");
    private void Publish()
    {
        // Invalidation can immediately reload a worn texture. Refresh only after
        // Register has populated the new image catalog, so a prior fallback
        // cannot remain cached when valid saved artwork is restored.
        foreach (var order in catalog.Values) helper.GameContent.InvalidateCache(Route(order));
        helper.GameContent.InvalidateCache("Data/Shirts"); helper.GameContent.InvalidateCache("Data/Pants");
        Game1.shirtData = DataLoader.Shirts(Game1.content); Game1.pantsData = DataLoader.Pants(Game1.content); ItemRegistry.ResetCache();
    }
    private void Persist()
    {
        if (Ready && state!.IsValid(farmerId)) Game1.player.modData[SaveKey] = JsonSerializer.Serialize(state);
    }
    private void Reset()
    {
        cancellation?.Cancel(); cancellation?.Dispose(); cancellation = null;
        if (request != null) _ = request.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        request = null; requestId = ""; state = null; farmerId = 0;
        foreach (var order in catalog.Values) helper.GameContent.InvalidateCache(Route(order));
        catalog.Clear(); images.Clear(); damaged.Clear();
    }
    internal FashionDefinition? Fashion(string id) => Ready && catalog.Values.FirstOrDefault(o => o.QualifiedId == id && o.Status == "fulfilled") is { } o
        ? new() { Slot = o.Slot, FashionValue = 60, StyleTags = new[] { "creative", "handmade", "craft" }, Practicality = 2, Statement = 2 } : null;
    private void SafeTick()
    {
        if (!Ready) return;
        try { CompleteRequest(); Recover(); }
        catch { monitor.Log("A tailoring operation could not complete; no automatic provider retry will run.", LogLevel.Warn); }
    }
    private void Recover()
    {
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (Game1.player.health < lastHealth) damageUntil = now + 10;
        bool active = Game1.shouldTimePass() && Game1.activeClickableMenu == null && !Game1.eventUp && Game1.currentMinigame == null && Game1.player.health > 0;
        bool Worn(string? id, string recipe) => id != null && catalog.TryGetValue(id, out var order) && order.Status == "fulfilled" && order.Recipe == recipe && !damaged.Contains(order.Id);
        var award = state!.Recovery.Tick(Today, active,
            Game1.player.health < Game1.player.maxHealth && Worn(Game1.player.shirtItem.Value?.ItemId, "mend"),
            Game1.player.Stamina < Game1.player.MaxStamina && Worn(Game1.player.pantsItem.Value?.ItemId, "renew"), now < damageUntil,
            Game1.currentGameTime.ElapsedGameTime.TotalSeconds);
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + award.Health);
        Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + award.Energy);
        lastHealth = Game1.player.health;
    }
}
