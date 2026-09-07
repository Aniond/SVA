using System.Text.Json;
using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

/// <summary>Hidden, deterministic outfit observations. It never changes friendship, inventory prices or combat stats.</summary>
internal sealed class FashionService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<bool> enabled;
    private FashionMemoryState? state;
    private FashionCatalog catalog = new();
    private readonly HashSet<Dialogue> amended = new();
    private DialogueBox? pendingBox;
    private string pendingNpc = "", pendingFingerprint = "", pendingLine = "";
    private static int Today => Game1.Date.TotalDays;
    internal bool Ready => enabled() && Context.IsWorldReady && !Context.IsMultiplayer && state != null;
    internal FashionMemoryState? State => state;
    internal Action<string, string>? OutfitObserved { get; set; }
    internal Func<string, FashionDefinition?>? CustomFashion { get; set; }
    internal FashionService(IModHelper helper, IMonitor monitor, Func<bool> enabled)
    {
        this.helper = helper; this.monitor = monitor; this.enabled = enabled;
        helper.Events.GameLoop.SaveLoaded += (_, _) => Load();
        helper.Events.GameLoop.Saving += (_, _) => { if (Ready && state!.IsValid(Game1.player.UniqueMultiplayerID)) helper.Data.WriteSaveData("fashion-observations", state); };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { state = null; amended.Clear(); pendingBox = null; };
        helper.Events.Display.MenuChanged += (_, e) => { if (e.NewMenu is DialogueBox box) NativeComment(box); };
        helper.Events.Display.RenderedActiveMenu += (_, _) => MarkDisplayedNativeComment();
    }
    private void Load()
    {
        state = null; amended.Clear(); pendingBox = null;
        if (!enabled() || Context.IsMultiplayer) return;
        try
        {
            var loaded = helper.Data.ReadSaveData<FashionMemoryState>("fashion-observations");
            if (loaded != null && !loaded.IsValid(Game1.player.UniqueMultiplayerID)) throw new InvalidDataException();
            state = loaded ?? new() { FarmerId = Game1.player.UniqueMultiplayerID };
            var authored = helper.Data.ReadJsonFile<FashionCatalog>("assets/fashion.json");
            catalog = authored?.IsValid() == true ? authored : new();
            if (authored != null && !authored.IsValid()) monitor.Log("Fashion metadata is invalid; unknown clothing uses neutral observations.", LogLevel.Warn);
            if (helper.ModRegistry.IsLoaded("David.AbigailModern"))
            {
                try
                {
                    var shared = JsonSerializer.Deserialize<FashionCatalog>(helper.GameContent.Load<string>("David.AbigailModern/FashionCatalog"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (shared?.IsValid() == true) foreach (var pair in shared.Items) catalog.Items[pair.Key] = pair.Value;
                }
                catch (Microsoft.Xna.Framework.Content.ContentLoadException) { /* Older artwork packs have no fashion catalog. */ }
                catch (JsonException) { monitor.Log("Shared clothing fashion metadata could not be read; existing metadata remains available.", LogLevel.Warn); }
            }
        }
        catch { state = null; monitor.Log("Fashion observations unavailable; existing save data preserved.", LogLevel.Warn); }
    }
    internal FashionPiece[] Outfit()
    {
        var farmer = Game1.player; var pieces = new List<FashionPiece>();
        void Add(string slot, string id, string dye, bool prismatic)
        {
            var data = ItemRegistry.GetDataOrErrorItem(id);
            var definition = CustomFashion?.Invoke(id) ?? catalog.Items.GetValueOrDefault(id);
            if (definition?.Slot != slot) definition = null;
            pieces.Add(new(slot, id, data.DisplayName, dye, prismatic, definition));
        }
        static string Rgb(Color color) => $"{color.R:X2}{color.G:X2}{color.B:X2}";
        Add("shirt", "(S)" + farmer.GetShirtId(), Rgb(farmer.GetShirtColor()), !farmer.IsOverridingShirt(out _) && farmer.shirtItem.Value?.isPrismatic.Value == true);
        Add("pants", "(P)" + farmer.GetPantsId(), Rgb(farmer.GetPantsColor()), !farmer.IsOverridingPants(out _, out _) && farmer.pantsItem.Value?.isPrismatic.Value == true);
        if (farmer.hat.Value is { } hat) Add("hat", hat.QualifiedItemId, "", hat.isPrismatic.Value);
        if (farmer.boots.Value is { } boots) Add("boots", boots.QualifiedItemId, boots.GetBootsColorString(), false);
        return pieces.ToArray();
    }
    private static bool CanSee(NPC npc) => npc.currentLocation == Game1.player.currentLocation && Vector2.Distance(npc.Tile, Game1.player.Tile) <= 4
        && !Game1.eventUp && Game1.currentMinigame == null && !Game1.player.swimming.Value;
    private static string Describe(FashionPiece[] pieces)
    {
        string description = string.Join("; ", pieces.Select(p => p.Slot + ": " + p.Name
            + (p.Prismatic ? " (prismatic)" : p.Dye.Length > 0 && p.Slot != "boots" ? " (dye RGB " + p.Dye + ")" : "")));
        return description[..Math.Min(1000, description.Length)];
    }
    internal object ContextFor(string name, bool phone)
    {
        if (!Ready) return new { Available = false };
        if (phone)
        {
            var seen = state!.LastSeen(name);
            return new { Available = seen != null, LastSeen = seen?.Description, Day = seen?.SeenDay,
                Rule = "This is a dated outfit personally seen earlier, not a view of the farmer now. Do not assume they still wear it. No unsolicited outfit comment by phone." };
        }
        var npc = Game1.getCharacterFromName(name);
        if (npc == null || !CanSee(npc)) return new { Available = false };
        var pieces = Outfit(); string fingerprint = FashionRules.Fingerprint(pieces); string description = Describe(pieces);
        state!.Observe(name, fingerprint, description, Today);
        OutfitObserved?.Invoke(name, fingerprint);
        var profile = NpcFashionProfiles.Get(name); var result = FashionRules.Evaluate(pieces, profile?.Taste ?? new() { Importance = 0 });
        return new { Available = true, Fingerprint = fingerprint, Day = Today, VisibleOutfit = description, result.Reaction, result.KnownStyleTags,
            MayComment = profile != null && profile.Taste.Importance > 0 && result.HasKnownStyle && state.CanComment(name, fingerprint, Today, 4 - profile.Taste.Importance),
            Rule = "Outfit facts are observed now; preference is subjective. Never reveal scores, ratings or hidden mechanics, invent garment details, infer wealth or hygiene, insult the person's worth, or change friendship. At most one brief optional outfit observation when MayComment is true; otherwise answer the conversation normally. Set commentedOutfit true only when you actually comment on this outfit." };
    }
    internal void RememberReply(string name, bool commented, string snapshot)
    {
        if (!Ready || !commented) return;
        try
        {
            using var document = JsonDocument.Parse(snapshot);
            var root = document.RootElement;
            if (root.TryGetProperty("Relationship", out var relationship)) root = relationship;
            if (!root.TryGetProperty("Fashion", out var fashion) || !fashion.TryGetProperty("MayComment", out var permitted) || !permitted.GetBoolean()
                || fashion.GetProperty("Day").GetInt32() != Today) return;
            state!.MarkComment(name, fashion.GetProperty("Fingerprint").GetString()!, Today);
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
        catch (KeyNotFoundException) { }
    }
    private void NativeComment(DialogueBox box)
    {
        if (!Ready || box.isQuestion || box.responses.Length > 0 || Game1.eventUp || box.characterDialogue?.speaker is not { } npc
            || RomanceRules.IsCandidate(npc.Name) || !CanSee(npc) || NpcFashionProfiles.Get(npc.Name) is not { } profile
            || amended.Contains(box.characterDialogue) || box.characterDialogue.dialogues.Count != 1
            || box.characterDialogue.dialogues.Any(line => line.SideEffects != null)
            || npc.Name == "Dwarf" && !Game1.player.canUnderstandDwarves) return;
        var pieces = Outfit(); string fingerprint = FashionRules.Fingerprint(pieces);
        state!.Observe(npc.Name, fingerprint, Describe(pieces), Today);
        var assessment = FashionRules.Evaluate(pieces, profile.Taste);
        if (assessment.Reaction == "neutral" || !state.CanComment(npc.Name, fingerprint, Today, 4 - profile.Taste.Importance)) return;
        string line = assessment.Reaction == "admiring" ? profile.Compliment : profile.Dislike;
        box.characterDialogue.dialogues.Add(new DialogueLine(line));
        box.characterDialogue.isCurrentStringContinuedOnNextScreen = true;
        amended.Add(box.characterDialogue); pendingBox = box; pendingNpc = npc.Name; pendingFingerprint = fingerprint; pendingLine = line;
    }
    private void MarkDisplayedNativeComment()
    {
        if (!Ready || pendingBox == null) return;
        if (!ReferenceEquals(Game1.activeClickableMenu, pendingBox)) { pendingBox = null; return; }
        string shown = pendingBox.getCurrentString();
        if (pendingBox.transitioning || pendingBox.characterIndexInDialogue < shown.Length || !shown.Contains(pendingLine, StringComparison.Ordinal)) return;
        state!.MarkComment(pendingNpc, pendingFingerprint, Today); pendingBox = null;
    }
}
