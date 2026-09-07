using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SvaPersistenceAudit;

public sealed partial class ModEntry : Mod
{
    private static ulong Seed = 202609060713;
    private const string Name = "SvaAudit", Marker = "sva-persistence-marker", ProtectedKey = "David.SolaceWeather/QuickStackProtected";
    private const string OutfitKey = "David.AbigailModern/AbigailAdventureOutfit";
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static ModEntry instance = null!;
    private static string profile = "";
    private readonly List<object> events = new();
    private bool initialized, capturePending, pendingLoad;
    private int sequence, snapshotDelay;
    private string delayedReason = "", lastError = "";
    private Array? artAssets;
    private object? artMod;
    private int artIndex;
    private readonly List<object> artResults = new();
    private int artFailed;

    public override void Entry(IModHelper helper)
    {
        instance = this;
        string seedFile = Path.Combine(helper.DirectoryPath, "audit-seed.txt");
        if (File.Exists(seedFile)) Seed = ulong.Parse(File.ReadAllText(seedFile).Trim(), System.Globalization.CultureInfo.InvariantCulture);
        // Require an explicit on-disk opt-in before installing any commands or save hooks.
        string requestedProfile = File.ReadAllText(Path.Combine(helper.DirectoryPath, "profile-root.txt")).Trim();
        if (!Path.IsPathFullyQualified(requestedProfile)) throw new InvalidOperationException("profile-root.txt must be an absolute path.");
        profile = Path.GetFullPath(requestedProfile).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string allowed = Path.GetFullPath(@"C:\Users\david\SDV\artifacts\new-game-persistence") + Path.DirectorySeparatorChar;
        if (!Path.IsPathFullyQualified(profile) || !profile.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("profile-root.txt must name a child of the disposable repository audit directory.");
        Directory.CreateDirectory(profile);
        AssertNoLinks(profile);
        var harmony = new Harmony(ModManifest.UniqueID);
        foreach (string method in new[] { "GetAppDataFolder", "GetLocalAppDataFolder" })
            harmony.Patch(AccessTools.Method(typeof(Program), method), prefix: new HarmonyMethod(typeof(ModEntry), nameof(RedirectProgramFolder)));
        foreach (string property in new[] { "DataPath", "SavesPath" })
            harmony.Patch(AccessTools.PropertyGetter(typeof(Constants), property) ?? throw new MissingMemberException(property),
                prefix: new HarmonyMethod(typeof(ModEntry), property == "DataPath" ? nameof(RedirectData) : nameof(RedirectSaves)));
        harmony.Patch(AccessTools.Method(typeof(SaveGame), "Save"), prefix: new HarmonyMethod(typeof(ModEntry), nameof(BeforeSave)));
        harmony.Patch(AccessTools.Method(typeof(Game1), "Draw", new[] { typeof(GameTime) }), postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterDraw)));
        VerifyPaths(); initialized = true;
        helper.Events.GameLoop.GameLaunched += (_, _) => { Game1.options.pauseWhenOutOfFocus = false; Record("GameLaunched"); };
        helper.Events.GameLoop.SaveLoaded += (_, _) => { Record("SaveLoaded"); QueueSnapshot("save-loaded", 120); };
        helper.Events.GameLoop.DayStarted += (_, _) => { Record("DayStarted"); QueueSnapshot("day-started", 120); };
        helper.Events.GameLoop.SaveCreated += (_, _) => { Record("SaveCreated"); RegisterSave(); };
        helper.Events.GameLoop.Saving += (_, _) => Record("Saving");
        helper.Events.GameLoop.Saved += (_, _) => { Record("Saved"); RegisterSave(); QueueSnapshot("saved", 120); };
        helper.Events.GameLoop.DayEnding += (_, _) => Record("DayEnding");
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Record("ReturnedToTitle");
        helper.Events.GameLoop.UpdateTicked += (_, e) =>
        {
            if (!initialized) return;
            Guard(WatchLiveAi);
            Guard(WatchCaveRoom);
            if (snapshotDelay > 0 && --snapshotDelay == 0) Guard(() => Snapshot(delayedReason));
            if (pendingLoad && !Context.IsWorldReady && Game1.activeClickableMenu is TitleMenu)
            { pendingLoad = false; Guard(LoadOwnedSave); }
            if (artAssets != null) Guard(NextArt);
            if (!e.IsMultipleOf(15)) return;
            string file = Path.Combine(helper.DirectoryPath, "request.txt");
            if (!File.Exists(file)) return;
            string command = File.ReadAllText(file).Trim().ToLowerInvariant(); File.Delete(file);
            Guard(() => Command(command));
        };
        Record("IsolationInstalled");
    }
    private static bool RedirectProgramFolder(object[] __args, ref string __result)
    {
        string? folder = __args.Length > 0 ? __args[0] as string : null;
        __result = SafePath(folder ?? "");
        if (__args.Length < 2 || __args[1] is not false) Directory.CreateDirectory(__result);
        return false;
    }
    private static bool RedirectData(ref string __result) { __result = profile; return false; }
    private static bool RedirectSaves(ref string __result) { __result = SafePath("Saves"); return false; }
    private static void BeforeSave() { VerifyPaths(); RequireOwnedWorld(); }
    private static string SafePath(string child)
    {
        string path = Path.GetFullPath(Path.Combine(profile, child));
        if (path != profile && !path.StartsWith(profile + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Rejected path outside disposable profile.");
        AssertNoLinks(path); return path;
    }
    private static void AssertNoLinks(string path)
    {
        for (var current = new DirectoryInfo(path); current != null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Disposable profile cannot use a directory link.");
    }
    private static void VerifyPaths()
    {
        if (Path.GetFullPath(Program.GetAppDataFolder()) != profile || Path.GetFullPath(Program.GetLocalAppDataFolder()) != profile
            || Path.GetFullPath(Constants.DataPath) != profile || Path.GetFullPath(Constants.SavesPath) != SafePath("Saves")
            || Path.GetFullPath(Program.GetSavesFolder()) != SafePath("Saves")) throw new InvalidOperationException("Save isolation verification failed.");
    }
    private static void RequireOwnedWorld()
    {
        VerifyPaths();
        if (Game1.player?.Name != Name || Game1.player.farmName.Value != Name || Game1.uniqueIDForThisGame != Seed
            || !File.Exists(SafePath("ownership.json"))) throw new InvalidOperationException("Action requires this harness's disposable native farm.");
    }
    private void Command(string command)
    {
        VerifyPaths(); Record("Request:" + command);
        switch (command)
        {
            case "mining-interactions": MiningInteractions(); break;
            case "phone-portrait-proof": PhonePortraitProof(); break;
            case "phone-portrait-close": ClosePhonePortraitProof(); break;
            case "modern-clothing-render": ModernClothingRender(); break;
            case "modern-clothing-checks": ModernClothingChecks(); break;
            case "modern-clothing-stage": ModernClothingStage(); break;
            case "modern-clothing-verify": ModernClothingVerify(); break;
            case "cave-room": EnterCaveRoom(20); break;
            case "cave-room-stone": EnterCaveRoom(15); break;
            case "cave-room-ice": EnterCaveRoom(60); break;
            case "cave-room-lava": EnterCaveRoom(100); break;
            case "cave-effects-off": SetCaveEffects(false); break;
            case "cave-effects-on": SetCaveEffects(true); break;
            case "cave-home": LeaveCaveRoom(); break;
            case "town-graves":
                RequireOwnedWorld();
                var town = Game1.getLocationFromName("Town");
                var buildings = town.Map.GetLayer("Buildings");
                var graveTiles = new List<object>();
                for (int ty = 0; ty < buildings.LayerHeight; ty++)
                    for (int tx = 0; tx < buildings.LayerWidth; tx++)
                    {
                        var tile = buildings.Tiles[tx, ty];
                        string? action = tile?.Properties.TryGetValue("Action", out var value) == true ? value.ToString() : null;
                        if (action != null && (action.Contains("grave", StringComparison.OrdinalIgnoreCase) || action.Contains("cemet", StringComparison.OrdinalIgnoreCase)))
                            graveTiles.Add(new { X = tx, Y = ty, Action = action, TileIndex = tile!.TileIndex });
                    }
                WriteProfile("town-graves.json", graveTiles); break;
            case "portrait-status": PortraitStatus(); break;
            case "portrait-chat": PortraitChat(); break;
            case "portrait-reuse": PortraitReuseCheck(); break;
            case "portrait-retry": PortraitRetry(); break;
            case "portrait-reference": CapturePortraitReference(); break;
            case "doorway-checks": RunDoorwayChecks(); break;
            case "doorway-exit": StartDoorwayExit(); break;
            case "ai-remember": StartLiveAi("remember"); break;
            case "ai-recall": StartLiveAi("recall"); break;
            case "ai-control": StartLiveAi("control"); break;
            case "status": Snapshot("status"); break;
            case "new":
                if (Context.IsWorldReady || Game1.activeClickableMenu is not TitleMenu title) throw new InvalidOperationException("New requires native title screen.");
                Directory.CreateDirectory(SafePath("Saves"));
                if (Directory.EnumerateFileSystemEntries(SafePath("Saves")).Any()) throw new InvalidOperationException("New requires an empty isolated Saves folder.");
                WriteProfile("ownership.json", new { Farmer = Name, Farm = Name, Seed, NativeCreation = true });
                Game1.resetPlayer();
                _ = new CharacterCustomization(CharacterCustomization.Source.NewGame);
                Game1.player.Name = Name; Game1.player.farmName.Value = Name; Game1.player.favoriteThing.Value = "quiet rain";
                Game1.whichFarm = 0; Game1.startingGameSeed = Seed;
                Snapshot("before-native-new");
                title.createdNewCharacter(true); Record("NativeCreatedNewCharacterInvoked"); break;
            case "stage": Stage(); break;
            case "journal":
                RequireOwnedWorld();
                var services = Services();
                services.Romance.GetType().GetMethod("OpenJournal", Members)!.Invoke(services.Romance, new object[] { "Abigail" });
                capturePending = true; break;
            case "sleep":
                RequireOwnedWorld();
                if (!Context.IsWorldReady || Game1.eventUp || Game1.currentMinigame != null) throw new InvalidOperationException("Sleep requires a settled native world.");
                Game1.exitActiveMenu();
                var home = Utility.getHomeOfFarmer(Game1.player);
                if (Game1.currentLocation != home) throw new InvalidOperationException("Sleep requires the farmer already inside their native farmhouse.");
                // Native new-day fade waits for the bed flag normally set by stepping into bed.
                Game1.player.isInBed.Value = true;
                home.answerDialogueAction("Sleep_Yes", Array.Empty<string>()); break;
            case "load":
                OwnedSaveFolder();
                if (Context.IsWorldReady) { RequireOwnedWorld(); pendingLoad = true; Game1.ExitToTitle(); }
                else LoadOwnedSave();
                break;
            case "capture": capturePending = true; break;
            case "art": StartArt(); break;
            case "quit": Snapshot("quit"); Game1.quit = true; break;
            default: throw new InvalidOperationException("Unsupported request. Allowed: new/status/stage/sleep/load/capture/journal/art/quit.");
        }
    }
    private string OwnedSaveFolder()
    {
        VerifyPaths();
        if (!File.Exists(SafePath("ownership.json"))) throw new InvalidOperationException("No harness ownership record.");
        string folder = Name + "_" + Seed;
        string full = SafePath(Path.Combine("Saves", folder));
        if (!File.Exists(Path.Combine(full, folder)) || !File.Exists(Path.Combine(full, "SaveGameInfo"))) throw new InvalidOperationException("No complete disposable native save found.");
        return folder;
    }
    private void LoadOwnedSave()
    {
        if (Context.IsWorldReady || Game1.activeClickableMenu is not TitleMenu) throw new InvalidOperationException("Load requires title screen.");
        string folder = OwnedSaveFolder(); Record("NativeLoadRequested");
        Game1.currentMinigame = null;
        Game1.exitActiveMenu();
        SaveGame.Load(folder);
    }
    private void RegisterSave() { Guard(() => { RequireOwnedWorld(); WriteProfile("created-save.json", new { Folder = OwnedSaveFolder(), Seed }); }); }
    private static object? Member(object? target, string name) => target == null ? null : target.GetType().GetField(name, Members)?.GetValue(target) ?? target.GetType().GetProperty(name, Members)?.GetValue(target);
    private object Mod(string id)
    {
        object info = Helper.ModRegistry.Get(id) ?? throw new InvalidOperationException("Required installed mod missing: " + id);
        return Member(info, "Mod") ?? throw new InvalidOperationException("Installed mod instance unavailable.");
    }
    private (object Abigail, object Romance, object Runtime) Services()
    {
        object mod = Mod("David.SolaceWeather");
        return (Member(mod, "abigail")!, Member(mod, "romance")!, Member(mod, "runtime")!);
    }
    private void Stage()
    {
        RequireOwnedWorld(); var (abigail, romance, runtime) = Services(); Snapshot("before-stage");
        if (Member(abigail, "Ready") is not true || Member(romance, "Ready") is not true)
            throw new InvalidOperationException("Production services are not ready; baseline preserves the new-farm initialization failure. Harness will not initialize them.");
        var memory = (AbigailMemory)Member(abigail, "memory")!;
        string statement = "I enjoy quiet rain and purple flowers.";
        abigail.GetType().GetMethod("RememberExchange", Members)!.Invoke(abigail, new object[] { statement, "I will remember your quiet rain." });
        memory.Personal.Apply(Game1.Date.TotalDays, statement, new[] { new MemoryProposal { Topic = Marker, Kind = "preference", Quote = "quiet rain", Timing = "unspecified" } });
        if (!memory.Promises.Offer("fish", Game1.Date.TotalDays) || !memory.Promises.Accept("fish", Game1.Date.TotalDays, 0, false))
            throw new InvalidOperationException("Native promise API rejected staging.");
        if (!memory.Promises.Complete("fish", "(O)145", "Sunfish", Game1.Date.TotalDays)) throw new InvalidOperationException("Promise completion rejected staging.");
        memory.Tree.Observe(memory.Promises, Game1.Date.TotalDays, Array.Empty<int>());
        if (!memory.Experiences.Record(Marker, "promise", Game1.Date.TotalDays, "The disposable audit completed the Sunfish promise through the promise API.", "quiet rain promise fish"))
            throw new InvalidOperationException("Experience API rejected staging.");
        var state = (RomanceSaveState)Member(romance, "State")!;
        RomanceRules.Talk(state, "Abigail", Game1.Date.TotalDays);
        if (!RomanceRules.RecordIncident(state, Marker, "Abigail", Game1.Date.TotalDays, "flirt")
            || !RomanceRules.LearnIncident(state, Marker, "Leah", Game1.Date.TotalDays, "Leah")) throw new InvalidOperationException("Romance incident API rejected staging.");
        Item item = ItemRegistry.Create("(O)80", 3); item.modData[ProtectedKey] = "true"; item.modData[ModManifest.UniqueID] = Marker;
        if (!Game1.player.addItemToInventoryBool(item)) throw new InvalidOperationException("No room for protected marker item.");
        // Native adventure progression flag; this is save data, not a claim of a completed adventure.
        Game1.player.mailReceived.Add("guildMember");
        Game1.player.modData[OutfitKey] = "true";
        Game1.getCharacterFromName("Abigail")?.ChooseAppearance();
        runtime.GetType().GetMethod("RequestEnabled", Members)!.Invoke(runtime, new object[] { true });
        WriteProfile("expected.json", new { Exchange = statement, Topic = Marker, Promise = "fish", PromiseStatus = "completed", TreeUnlock = "root", Experience = Marker, Incident = Marker, Knower = "Leah", Item = "(O)80", Stack = 3, ProtectedKey, AdventureFlag = "guildMember", OutfitKey, StageDay = Game1.Date.TotalDays, WeatherRequested = true });
        Snapshot("staged");
    }
    private void Snapshot(string reason)
    {
        var (abigail, romance, runtime) = Services();
        var memory = Member(abigail, "memory") as AbigailMemory;
        var farm = Member(romance, "farm"); var state = Member(romance, "State") as RomanceSaveState;
        bool owned = Context.IsWorldReady && Game1.uniqueIDForThisGame == Seed && Game1.player.Name == Name;
        var checks = new Dictionary<string, bool>();
        object? context = owned && Member(abigail, "Ready") is true
            ? abigail.GetType().GetMethod("GetConversationContext", Members)!.Invoke(abigail, new object[] { "Do you remember quiet rain?" }) : null;
        string contextJson = JsonSerializer.Serialize(context);
        bool blank = memory != null && memory.Exchanges.Count == 0 && memory.Personal.Details.Count == 0
            && memory.Promises.Records.All(p => p.Status != "completed" && p.Status != "active") && memory.Tree.Unlocked.Count == 0
            && memory.Experiences.Entries.Count == 0 && state?.Incidents.Count == 0;
        if (owned && File.Exists(SafePath("expected.json")))
        {
            checks["Exchange"] = memory?.Exchanges.Any(e => e.Farmer == "I enjoy quiet rain and purple flowers.") == true;
            checks["Personal"] = memory?.Personal.Details.Any(d => d.Topic == Marker && d.Quote == "quiet rain") == true;
            checks["Promise"] = memory?.Promises.Records.Any(p => p.Id == "fish" && p.Status == "completed") == true;
            checks["TreeRoot"] = memory?.Tree.Unlocked.Contains("root") == true;
            checks["Experience"] = memory?.Experiences.Entries.Any(e => e.Id == Marker) == true;
            checks["Incident"] = state?.Incidents.Any(i => i.Id == Marker) == true;
            checks["WitnessKnowledge"] = state?.Knowledge.Any(k => k.IncidentId == Marker && k.KnowerNpc == "Leah") == true;
            using var document = JsonDocument.Parse(contextJson);
            checks["ConversationContextReadsPreference"] = document.RootElement.TryGetProperty("PersistentDetails", out var details)
                && details.EnumerateArray().Any(d => d.GetProperty("Topic").GetString() == Marker && d.GetProperty("Quote").GetString() == "quiet rain");
            checks["ProtectedItem"] = Game1.player.Items.Any(i => i?.QualifiedItemId == "(O)80" && i.Stack == 3 && i.modData.ContainsKey(ProtectedKey) && i.modData.ContainsKey(ModManifest.UniqueID));
            checks["AdventureFlag"] = Game1.player.mailReceived.Contains("guildMember");
            checks["OutfitFlag"] = Game1.player.modData.TryGetValue(OutfitKey, out var outfit) && outfit == "true";
            checks["OutfitAppearanceSelected"] = Game1.getCharacterFromName("Abigail")?.LastAppearanceId?.StartsWith("David.AbigailModern/Adventure", StringComparison.Ordinal) == true;
            using var expected = JsonDocument.Parse(File.ReadAllText(SafePath("expected.json")));
            int stageDay = expected.RootElement.GetProperty("StageDay").GetInt32();
            var weather = Member(runtime, "State") as WeatherSaveState;
            checks["WeatherOptIn"] = Game1.Date.TotalDays > stageDay
                ? weather?.Enabled == true && weather.PendingEnabled == null
                : weather != null && (weather.Enabled || weather.PendingEnabled == true);
        }
        var snapshot = new { Reason = reason, Sequence = sequence, WorldReady = Context.IsWorldReady, OwnedWorld = owned,
            Day = owned ? Game1.Date.TotalDays : -1, Seed, InitialMemoryBlank = blank,
            CrossSaveMarkersAbsent = owned && blank && !contextJson.Contains(Marker, StringComparison.Ordinal)
                && !Game1.player.Items.Any(i => i?.modData.ContainsKey(ModManifest.UniqueID) == true) && !Game1.player.mailReceived.Contains("guildMember")
                && !Game1.player.modData.ContainsKey(OutfitKey) && Member(runtime, "State") is WeatherSaveState { Enabled: false, PendingEnabled: null },
            OutfitFlagPresent = owned && Game1.player.modData.ContainsKey(OutfitKey),
            AbigailAppearance = owned ? Game1.getCharacterFromName("Abigail")?.LastAppearanceId : null,
            ConversationContext = context,
            AbigailReady = Member(abigail, "Ready"), RomanceReady = Member(romance, "Ready"), WeatherEnabled = Member(runtime, "Enabled"),
            AbigailMemoryNull = memory == null, RomanceFarmNull = farm == null, WeatherStateNull = Member(runtime, "State") == null,
            Memory = memory, RomanceFarm = farm, WeatherState = Member(runtime, "State"), Checks = checks,
            PersistencePassed = checks.Count > 0 && checks.Values.All(v => v), LastError = lastError,
            Limits = "No AI calls; no existing saves. Art validated separately using art request. Markers are deliberate test inputs, not player history." };
        Helper.Data.WriteJsonFile("status.json", snapshot);
        WriteProfile($"snapshot-{sequence:D4}-{reason}.json", snapshot);
    }
    private void StartArt()
    {
        RequireOwnedWorld(); artMod = Mod("David.AbigailModern");
        var modHelper = (IModHelper)Member(artMod, "Helper")!;
        Type entry = artMod.GetType().GetNestedType("ArtAsset")!;
        var read = modHelper.Data.GetType().GetMethods().First(m => m.Name == "ReadJsonFile" && m.IsGenericMethodDefinition);
        artAssets = (Array)read.MakeGenericMethod(entry.MakeArrayType()).Invoke(modHelper.Data, new object[] { "artwork.json" })!;
        artIndex = 0; artFailed = 0; artResults.Clear();
    }
    private void NextArt()
    {
        if (artAssets == null || artMod == null) return;
        object asset = artAssets.GetValue(artIndex)!; string? failure = null;
        try { artMod.GetType().GetMethod("CheckAsset", Members)!.Invoke(artMod, new[] { asset }); }
        catch (Exception ex) { failure = ex.GetBaseException().Message; artFailed++; }
        artResults.Add(new { Name = Member(asset, "Name"), Passed = failure == null, Error = failure }); artIndex++;
        if (artIndex < artAssets.Length) return;
        Helper.Data.WriteJsonFile("art-checks.json", new { Passed = artFailed == 0 && artIndex == 547, Count = artIndex, Failures = artFailed, Cases = artResults }); artAssets = null;
    }
    private static void AfterDraw()
    {
        if (!instance.capturePending) return; instance.capturePending = false;
        instance.Guard(() =>
        {
            var device = Game1.graphics.GraphicsDevice; var p = device.PresentationParameters;
            if ((long)p.BackBufferWidth * p.BackBufferHeight > 16000000) throw new InvalidOperationException("Screenshot exceeds bounded 16 MP limit.");
            var colors = new Color[p.BackBufferWidth * p.BackBufferHeight]; device.GetBackBufferData(colors);
            using var texture = new Texture2D(device, p.BackBufferWidth, p.BackBufferHeight); texture.SetData(colors);
            string output=instance.namedCapture??$"capture-{instance.sequence:D4}.png";instance.namedCapture=null;
            using var stream = File.Create(SafePath(output)); texture.SaveAsPng(stream, p.BackBufferWidth, p.BackBufferHeight);
        });
    }
    private void QueueSnapshot(string reason, int delay) { delayedReason = reason; snapshotDelay = delay; }
    private void Record(string name)
    {
        events.Add(new { Sequence = ++sequence, Event = name, Utc = DateTime.UtcNow, WorldReady = Context.IsWorldReady });
        Helper.Data.WriteJsonFile("events.json", events);
    }
    private static void WriteProfile(string file, object value) => File.WriteAllText(SafePath(file), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception ex) { lastError = ex.GetBaseException().Message; Monitor.Log("Persistence audit: " + lastError, LogLevel.Error); Helper.Data.WriteJsonFile("error.json", new { Error = lastError, Sequence = sequence }); }
    }
}
