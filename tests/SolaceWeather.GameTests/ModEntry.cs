using System.Collections;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry : Mod
{
    private long drawCount;
    private bool muteTestingAudio = true;
    private bool captureRequested;
    private int captureDelay;
    private float? originalUiScale;
    private float originalDesiredUiScale;
    private bool? originalPauseWhenOutOfFocus;
    private Action? captureAfterDraw;
    private static ModEntry instance = null!;
    private static void AfterDraw() => instance.captureAfterDraw?.Invoke();
    private static readonly Type Patches = typeof(SolaceWeather.ModEntry).Assembly.GetType("SolaceWeather.Integration.GamePatches", true)!;
    private static object Runtime => Patches.GetField("runtime", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    private static object Ui => Patches.GetField("ui", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    private static object? Call(object target, string method, params object?[] args)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo selected = args.Length == 0
            ? target.GetType().GetMethod(method, flags, null, Type.EmptyTypes, null)!
            : target.GetType().GetMethods(flags).Single(m => m.Name == method && m.GetParameters().Length == args.Length);
        return selected.Invoke(target, args);
    }
    private static object? Get(object target, string property) => target.GetType().GetProperty(property)!.GetValue(target);
    private static WeatherSaveState State => (WeatherSaveState)Get(Runtime, "State")!;

    public override void Entry(IModHelper helper)
    {
        instance = this;
        helper.Events.Specialized.LoadStageChanged += (_, e) => { if (e.NewStage == StardewModdingAPI.Enums.LoadStage.SaveParsed) tailoringLoadEvents.Add("SaveParsed"); };
        new Harmony(ModManifest.UniqueID + ".TailoringAudit").Patch(AccessTools.Method(typeof(SaveGame), "loadDataToFarmer"), prefix: new HarmonyMethod(typeof(ModEntry), nameof(BeforeFarmerRestore)));
        helper.Events.GameLoop.UpdateTicked += (_, _) =>
        {
            if (muteTestingAudio)
                Microsoft.Xna.Framework.Audio.SoundEffect.MasterVolume = 0f;
        };
        helper.Events.GameLoop.UpdateTicked += (_, _) => WatchAiCheck();
        helper.Events.GameLoop.UpdateTicked += (_, _) => WatchChatter();
        captureAfterDraw = () =>
        {
            drawCount++;
            if (!captureRequested) return;
            if (captureDelay-- > 0) return;
            captureRequested = false;
            try
            {
                GraphicsDevice device = Game1.game1.GraphicsDevice;
                int width = device.PresentationParameters.BackBufferWidth;
                int height = device.PresentationParameters.BackBufferHeight;
                var pixels = new Color[width * height];
                device.GetBackBufferData(pixels);
                using var texture = new Texture2D(device, width, height);
                texture.SetData(pixels);
                using var stream = File.Create(Path.Combine(Helper.DirectoryPath, "capture.png"));
                texture.SaveAsPng(stream, width, height);
                Helper.Data.WriteJsonFile("capture.json", new { Success = true, Width = width, Height = height, DrawCount = drawCount, Source = "Game1.Draw postfix backbuffer", RowsReversed = false, Ambient = Game1.ambientLight.ToString(), Outdoors = Game1.outdoorLight.ToString() });
            }
            catch (Exception ex)
            {
                Helper.Data.WriteJsonFile("capture.json", new { Success = false, Error = ex.GetBaseException().Message, DrawCount = drawCount });
            }
        };
        new Harmony(ModManifest.UniqueID).Patch(AccessTools.Method(typeof(Game1), "Draw", new[] { typeof(GameTime) }), postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterDraw)));
        helper.Events.GameLoop.UpdateTicked += (_, e) =>
        {
            if (!e.IsMultipleOf(30)) return;
            string path = Path.Combine(Helper.DirectoryPath, "request.txt");
            if (!File.Exists(path)) return;
            string request = File.ReadAllText(path).Trim().ToLowerInvariant();
            File.Delete(path);
            try
            {
                switch (request)
                {
                    case "profilechecks": ProfileNativeChecks(); break;
                    case "emilystate": EmilyState(); break;
                    case "emilystage": EmilyStage(); break;
                    case "emilypromise": EmilyPromise(); break;
                    case "emilyoffer": EmilyOffer(); break;
                    case "emilyaccept": EmilyAccept(); break;
                    case "emilymeet": EmilyMeet(); break;
                    case "emilytalk": EmilyTalk(); break;
                    case "emilyadvance": EmilyAdvance(); break;
                    case "emilylive": EmilyLive(); break;
                    case "emilyfixture": EmilyFixture(); break;
                    case "emilymove": EmilyMovementStart(); break;
                    case "emilymoveadvance": EmilyMovementAdvance(); break;
                    case "emilytree": Call(Emily(), "OpenTree"); captureRequested = true; captureDelay = 10; break;
                    case "tailoringgenerate": TailoringGenerateFixture(); break;
                    case "tailoringcraft": TailoringCraftChecks(); break;
                    case "tailoringstate": TailoringSnapshot(); break;
                    case "tailoringnext": TailoringNextDay(); break;
                    case "tailoringrender": TailoringRender(); break;
                    case "tailoringrecovery": TailoringRecoveryChecks(); break;
                    case "tailoringisolation": TailoringIsolationChecks(); break;
                    case "haleystate": HaleyState(); break;
                    case "haleystage": HaleyStage(); break;
                    case "haleypromise": HaleyPromise(); break;
                    case "haleyoffer": HaleyOffer(); break;
                    case "haleyaccept": HaleyAccept(); break;
                    case "haleymeet": HaleyMeet(); break;
                    case "haleytalk": HaleyTalk(); break;
                    case "haleyadvance": HaleyAdvance(); break;
                    case "haleylive": HaleyLive(); break;
                    case "haleytree": HaleyTree(); break;
                    case "haleyguards": HaleyGuards(); break;
                    case "haleyrestchecks": HaleyRestChecks(); break;
                    case "fashioncheck": FashionCheck(); break;
                    case "modernfashion": ModernFashionStage(); break;
                    case "nativefashion": NativeFashionCheck(); break;
                    case "socialstages": SocialStageCapture(); break;
                    case "socialnavigation": SocialNavigationChecks(); break;
                    case "haleyhigh": Haley(); if (!Game1.player.friendshipData.ContainsKey("Haley")) Game1.player.friendshipData.Add("Haley", new Friendship()); Game1.player.friendshipData["Haley"].Points = 2500; break;
                    case "haleyphotoguards": HaleyPhotoGuards(); break;
                    case "dialogueadvance": Phone(); if (Game1.activeClickableMenu is StardewValley.Menus.DialogueBox dialogue) dialogue.receiveLeftClick(dialogue.xPositionOnScreen + dialogue.width / 2, dialogue.yPositionOnScreen + dialogue.height / 2); break;
                    case "cemeterystate": CemeteryState(); break;
                    case "cemeterychecks": CemeteryChecks(); break;
                    case "cemeterylive": CemeteryLive(); break;
                    case "cemeterystage": CemeteryStage(); break;
                    case "cemeteryoffer": CemeteryOffer(); break;
                    case "cemeteryaccept": CemeteryAccept(); break;
                    case "cemeterymeet": CemeteryMeet(); break;
                    case "cemeterytalk": CemeteryTalk(); break;
                    case "cemeteryadvance": CemeteryAdvance(); break;
                    case "cemeterycancel": CemeteryCancel(); break;
                    case "chatterstage": ChatterStage(); break;
                    case "chatterchecks": ChatterChecks(); break;
                    case "chatterlive": ChatterLive(); break;
                    case "chatterreplay": ChatterReplay(); break;
                    case "chattersleep": Chatter(); if (Game1.currentLocation is not StardewValley.Locations.FarmHouse) throw new InvalidOperationException("Move to the native farmhouse first."); Game1.player.isInBed.Value = true; Game1.exitActiveMenu(); Game1.currentLocation.answerDialogueAction("Sleep_Yes", null); break;
                    case "chatterstate": ChatterState(); break;
                    case "chatterhome": Chatter(); TrustBed(); break;
                    case "phonechecks": PhoneChecks(); break;
                    case "phonecapture": PhoneCapture(); break;
                    case "phonecompact": PhoneCompact(); break;
                    case "phoneai": PhoneAi(false); break;
                    case "phoneshortcut": PhoneAi(true); break;
                    case "phoneinitiative": PhoneInitiative(); break;
                    case "phonestate": PhoneSnapshot(); break;
                    case "phoneclose": Call(Phone(), "Close"); break;
                    case "phonemic": PhoneMic(); break;
                    case "phoneclick": PhoneClick(); break;
                    case "phoneexchangechecks": PhoneExchangeChecks(); break;
                    case "phonenumber": PhoneNumber(); break;
                    case "phonesamnumber": PhoneNumber("Sam"); break;
                    case "phoneaccept": PhoneAnswer(true); break;
                    case "phonedecline": PhoneAnswer(false); break;
                    case "phoneblockstage": Phone(); Game1.player.friendshipData["Sam"].Status = FriendshipStatus.Divorced; break;
                    case "phonesamchat": Call(Phone(), "Open"); Call(Game1.activeClickableMenu, "Select", "Sam"); captureRequested = true; captureDelay = 3; break;
                    case "phonedropdown": Call(Game1.activeClickableMenu, "ToggleDropdown"); captureRequested = true; captureDelay = 3; break;
                    case "load": Game1.currentMinigame = null; Game1.exitActiveMenu(); SaveGame.Load("Solace_448236644"); break;
                    case "quit": RestoreUiScale(); RestoreFocusPause(); Game1.game1.Exit(); break;
                    case "status": WriteStatus(); break;
                    case "mute": muteTestingAudio = true; Microsoft.Xna.Framework.Audio.SoundEffect.MasterVolume = 0f; break;
                    case "unmute": muteTestingAudio = false; Microsoft.Xna.Framework.Audio.SoundEffect.MasterVolume = 1f; break;
                    case "abigailchecks": CheckAbigail(); break;
                    case "aistart": StartAiCheck(); break;
                    case "personalmemory": CheckPersonalMemory(); break;
                    case "aiclick": CheckAiClick(); break;
                    case "aiinspect": InspectAiCheck(); break;
                    case "gotoabigail":
                        RequireWorld();
                        StageBackgroundProgress();
                        var abigail = Game1.getCharacterFromName("Abigail");
                        var location = abigail.currentLocation;
                        var offsets = new[] { new Point(0, 1), new Point(1, 0), new Point(-1, 0), new Point(0, -1) };
                        var target = Game1.player.currentLocation == location && Vector2.Distance(Game1.player.Tile, abigail.Tile) <= 3
                            ? Game1.player.TilePoint
                            : offsets.Select(offset => abigail.TilePoint + offset).First(tile => location.CanItemBePlacedHere(tile.ToVector2()));
                        Game1.exitActiveMenu();
                        Game1.player.isInBed.Value = false;
                        Game1.player.FarmerSprite.StopAnimation();
                        Game1.player.CanMove = true;
                        Game1.warpFarmer(location.NameOrUniqueName, target.X, target.Y, false);
                        Helper.Data.WriteJsonFile("abigail-travel.json", new { Location = location.NameOrUniqueName, Tile = target.ToString() });
                        break;
                    case "capture": captureRequested = true; captureDelay = 2; WriteStatus(); break;
                    case "feedbackcapture":
                    case "feedbackcompact":
                    case "feedbackwalkcapture":
                        RequireWorld();
                        if (request == "feedbackcompact")
                        {
                            if (!originalUiScale.HasValue) { originalUiScale = Game1.options.baseUIScale; originalDesiredUiScale = Game1.options.desiredUIScale; }
                            Game1.options.desiredUIScale = 1.5f; Game1.options.baseUIScale = 1.5f;
                            Game1.game1.refreshWindowSettings();
                        }
                        if (request == "feedbackwalkcapture")
                        {
                            object info = Helper.ModRegistry.Get("David.SolaceWeather")!;
                            object mod = info.GetType().GetProperty("Mod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(info)!;
                            object movement = typeof(SolaceWeather.ModEntry).GetField("movement", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(mod)!;
                            Call(movement, "TryWalkTo", new Point(Game1.player.TilePoint.X + 2, Game1.player.TilePoint.Y));
                        }
                        Game1.setMousePosition((int)((Game1.player.TilePoint.X * 64 + 32 - Game1.viewport.X) * Game1.options.zoomLevel),
                            (int)((Game1.player.TilePoint.Y * 64 + 32 - Game1.viewport.Y) * Game1.options.zoomLevel));
                        captureRequested = true; captureDelay = 4; break;
                    case "questinput":
                    case "questreply":
                    case "questthanks": QuestUiChecks(request); break;
                    case "deliverychecks": CheckDelivery(); break;
                    case "treechecks": CheckTree(); break;
                    case "romancewarningstart": StartRomanceWarningCheck(); break;
                    case "romancewarningfinish": FinishRomanceWarningCheck(); break;
                    case "romancebirthdue":
                        RequireRomanceFixture();
                        Game1.player.friendshipData["Sam"].NextBirthingDate = new WorldDate(Game1.Date);
                        break;
                    case "romancebirthcontinue":
                        RequireRomanceFixture();
                        if (Game1.farmEvent is StardewValley.Events.BirthingEvent)
                        {
                            if (Game1.activeClickableMenu is StardewValley.Menus.NamingMenu birthName)
                            { birthName.textBox.Text = "SolaceArrival"; birthName.textBoxEnter(birthName.textBox); }
                            else if (Game1.activeClickableMenu is StardewValley.Menus.DialogueBox birthMessage) birthMessage.closeDialogue();
                        }
                        break;
                    case "romanceweatherchecks": RomanceWeatherTravelChecks(); break;
                    case "romanceconversationchecks": RomanceConversationChecks(); break;
                    case "romancewitnesschecks": RomanceWitnessChecks(); break;
                    case "romanceovernightstage": RomanceOvernightStage(); break;
                    case "romanceovernightcheck": RomanceOvernightCheck(); break;
                    case "romancemarriagestage": Game1.year = 2; RomanceMarriageStage(); break;
                    case "romanceceremony": RomanceMarriageFinishCeremony(); break;
                    case "romancemarriagecheck": RomanceMarriageCheck(); break;
                    case "romancedivorcestage": RomanceDivorceStage(); break;
                    case "romancedivorcechildstage": RomanceDivorceWithChildStage(); break;
                    case "romancebirthstage": RomancePendingBirthStage(); break;
                    case "romancedivorcecheck": RomanceDivorceCheck(); break;
                    case "romancescenechecks": RomanceSceneChecks(); break;
                    case "romanceaichecks": StartRomanceAiChecks(); break;
                    case "romanceopen": Call(typeof(SolaceWeather.ModEntry).GetField("romance", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AiMod(Helper))!, "OpenJournal", "Abigail"); break;
                    case "romanceglobalchecks": RomanceGlobalChecks(); break;
                    case "romancenativechecks": RomanceNativeChecks(); break;
                    case "romancedatechecks": RomanceDateChecks(); break;
                    case "experiencechecks": ExperienceChecks(); break;
                    case "experienceai": ExperienceAi(); break;
                    case "treeprepareai": TreePrepareAi(); break;
                    case "treeopen":
                    case "treecompact":
                    case "treeperks":
                    case "treesocial":
                    case "treesocialcapture":
                    case "treescroll": TreeUiChecks(request); break;
                    case "treestage": StageTreeSave(); break;
                    case "stagefish": StageFishRequest(); break;
                    case "trustbed": TrustBed(); break;
                    case "trustsleep": TrustSleep(); break;
                    case "trustsavecheck": TrustSaveCheck(); break;
                    case "trustaccept": ClickTrustChoice("accept0"); break;
                    case "trustgive": ClickTrustChoice("give"); break;
                    case "questcompactinput":
                    case "questcompact":
                        originalUiScale ??= Game1.options.baseUIScale; originalDesiredUiScale = Game1.options.desiredUIScale;
                        Game1.options.baseUIScale = 1.5f; Game1.options.desiredUIScale = 1.5f; Game1.game1.refreshWindowSettings();
                        Game1.PushUIMode(); try { QuestUiChecks(request == "questcompactinput" ? "questinput" : "questreply"); } finally { Game1.PopUIMode(); } break;
                    case "checks": RequireWorld(); RunChecks(); break;
                    case "movementchecks": RequireWorld(); MovementChecks(); break;
                    case "smarttools": RequireWorld(); SmartToolChecks(); break;
                    case "quickstack": RequireWorld(); QuickStackChecks(); break;
                    case "cropprotection": RequireWorld(); CropProtectionChecks(); break;
                    case "machineindicators": RequireWorld(); MachineIndicatorChecks(); break;
                    case "nearbycrafting": RequireWorld(); NearbyCraftingChecks(); break;
                    case "machinecapture": RequireWorld(); StageMachineIndicators(); break;
                    case "interactions": RequireWorld(); InteractionChecks(); break;
                    case "journal": RequireWorld(); Call(Ui, "OpenJournal"); break;
                    case "journalcapture": RequireWorld(); Call(Ui, "OpenJournal"); captureRequested = true; captureDelay = 2; break;
                    case "journalcompact":
                        RequireWorld();
                        if (!originalUiScale.HasValue) { originalUiScale = Game1.options.baseUIScale; originalDesiredUiScale = Game1.options.desiredUIScale; }
                        Game1.options.desiredUIScale = 1.5f; Game1.options.baseUIScale = 1.5f;
                        Game1.game1.refreshWindowSettings();
                        Game1.PushUIMode();
                        try { Call(Ui, "OpenJournal"); } finally { Game1.PopUIMode(); }
                        captureRequested = true; captureDelay = 2; break;
                    case "uirestore": RestoreUiScale(); break;
                    case "journalqa":
                        RequireWorld(); Call(Ui, "OpenJournal");
                        var menu = Game1.activeClickableMenu;
                        var controls = (List<(Rectangle Bounds, Action Action)>)menu.GetType().GetField("controls", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(menu)!;
                        Rectangle regionButton = controls[(int)Region.Desert].Bounds;
                        menu.receiveLeftClick(regionButton.Center.X, regionButton.Center.Y, false);
                        bool regionPassed = (Region)menu.GetType().GetField("region", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(menu)! == Region.Desert;
                        bool originalUnits = (bool)Get(Runtime, "UseFahrenheit")!;
                        Rectangle unitButton = controls[7].Bounds;
                        menu.receiveLeftClick(unitButton.Center.X, unitButton.Center.Y, false);
                        bool unitsPassed = (bool)Get(Runtime, "UseFahrenheit")! != originalUnits;
                        menu.receiveLeftClick(unitButton.Center.X, unitButton.Center.Y, false);
                        Helper.Data.WriteJsonFile("journal-qa.json", new { RegionPassed = regionPassed, UnitsPassed = unitsPassed, UnitsRestored = (bool)Get(Runtime, "UseFahrenheit")! == originalUnits });
                        captureRequested = true; captureDelay = 2; break;
                    case "rain":
                        StageBackgroundProgress();
                        RequireWorld(); State.Enabled = true; Game1.season = Season.Spring; Game1.dayOfMonth = 6; Game1.stats.DaysPlayed = 6; Game1.timeOfDay = 900;
                        Call(Runtime, "Force", Region.Farm, WeatherKind.Rain); Call(Runtime, "Force", Region.Town, WeatherKind.Rain);
                        Game1.exitActiveMenu(); Game1.warpFarmer("Farm", 64, 15, false); break;
                    case "festival":
                        StageBackgroundProgress();
                        RequireWorld(); State.Enabled = true; Game1.year = 1; Game1.stats.DaysPlayed = 13;
                        Game1.exitActiveMenu(); DebugCommands.TryHandle(new[] { "festival", "spring13" });
                        Call(Runtime, "Force", Region.Town, WeatherKind.Rain); break;
                    case "festivalqa": RequireEggFestival(); FestivalChecks(); break;
                    case "festivalclear":
                    case "festivalrain":
                        RequireEggFestival(); Call(Runtime, "Force", Region.Town, request == "festivalclear" ? WeatherKind.Clear : WeatherKind.Rain);
                        Color expectedAmbient = request == "festivalclear" ? Color.White : new Color(255, 200, 80);
                        Helper.Data.WriteJsonFile("festival-lighting.json", new { Request = request, Passed = Game1.ambientLight == expectedAmbient, Actual = Game1.ambientLight.ToString(), Expected = expectedAmbient.ToString() });
                        captureRequested = true; captureDelay = 2; break;
                    case "festivalhunt":
                        RequireEggFestival();
                        if (!Game1.CurrentEvent.eventSwitched) Game1.CurrentEvent.forceFestivalContinue();
                        captureRequested = true; captureDelay = 2; break;
                    case "festivaladvance":
                        RequireEggFestival();
                        if (Game1.activeClickableMenu is StardewValley.Menus.DialogueBox dialogueBox)
                            dialogueBox.receiveLeftClick(dialogueBox.xPositionOnScreen + dialogueBox.width / 2, dialogueBox.yPositionOnScreen + dialogueBox.height / 2);
                        break;
                    case "huntqa":
                        RequireEggFestival();
                        Helper.Data.WriteJsonFile("hunt-qa.json", new
                        {
                            NativeHuntActive = Game1.CurrentEvent.playerControlSequenceID == "eggHunt",
                            DecorationHidden = Game1.CurrentEvent.eventSwitched || !Game1.CurrentEvent.playerControlSequence || Game1.CurrentEvent.playerControlSequenceID == "eggHunt",
                            Timer = Game1.CurrentEvent.festivalTimer, Props = Game1.CurrentEvent.festivalProps.Count,
                            Score = Game1.player.festivalScore, Sequence = Game1.CurrentEvent.playerControlSequenceID
                        });
                        captureRequested = true; captureDelay = 2; break;
                    default: Monitor.Log("Unrecognized test request ignored.", LogLevel.Warn); break;
                }
                Helper.Data.WriteJsonFile("last-request.json", new { Request = request, Success = true });
            }
            catch (Exception ex)
            {
                Monitor.Log($"Harness request failed: {ex.GetBaseException().Message}", LogLevel.Error);
                Helper.Data.WriteJsonFile("last-request.json", new { Request = request, Success = false, Error = ex.GetBaseException().Message });
            }
        };
        Monitor.Log("Test-only file command harness ready. Only the explicit trustsleep request saves the Solace test farm.", LogLevel.Info);
    }

    private static void RequireWorld()
    {
        if (!Context.IsWorldReady) throw new InvalidOperationException("Load the test farm first.");
    }

    private void RestoreUiScale()
    {
        if (!originalUiScale.HasValue) return;
        Game1.options.baseUIScale = originalUiScale.Value; Game1.options.desiredUIScale = originalDesiredUiScale;
        originalUiScale = null;
        Game1.game1.refreshWindowSettings();
    }

    private void StageBackgroundProgress()
    {
        originalPauseWhenOutOfFocus ??= Game1.options.pauseWhenOutOfFocus;
        Game1.options.pauseWhenOutOfFocus = false;
    }

    private void RestoreFocusPause()
    {
        if (!originalPauseWhenOutOfFocus.HasValue) return;
        Game1.options.pauseWhenOutOfFocus = originalPauseWhenOutOfFocus.Value;
        originalPauseWhenOutOfFocus = null;
    }

    private static void RequireEggFestival()
    {
        RequireWorld();
        if (Game1.CurrentEvent?.isSpecificFestival("spring13") != true) throw new InvalidOperationException("Enter the Egg Festival first.");
    }

    private void FestivalChecks()
    {
        Event festival = Game1.CurrentEvent;
        if (festival.eventSwitched) throw new InvalidOperationException("Run gathering checks before starting the hunt.");
        object controller = Patches.GetField("festival", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        object states = controller.GetType().GetField("states", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(controller)!;
        object?[] stateArgs = { festival, null };
        bool stateFound = (bool)states.GetType().GetMethod("TryGetValue")!.Invoke(states, stateArgs)!;
        if (!stateFound) throw new InvalidOperationException("Wait for the festival gathering to render first.");
        object state = stateArgs[1]!;
        var remarked = (HashSet<string>)state.GetType().GetField("RemarkedNpcs")!.GetValue(state)!;
        var anchors = (List<Vector2>)state.GetType().GetField("ShelterAnchors")!.GetValue(state)!;
        var results = new List<object>();
        var props = festival.festivalProps.ToArray();
        var positions = festival.actors.ToDictionary(n => n.Name, n => n.Position);
        string[] commands = festival.eventCommands;
        int timer = festival.festivalTimer;
        results.Add(new { Name = "Both gathering shelters initialized", Passed = anchors.Count == 2 });
        foreach (string name in new[] { "Pierre", "Gus", "Abigail", "Penny", "Emily", "Lewis" })
        {
            NPC? actor = festival.actors.FirstOrDefault(n => n.Name == name);
            if (actor is null) { results.Add(new { Name = name + " present", Passed = false }); continue; }
            bool wasRemarked = remarked.Remove(name);
            try
            {
                festival.TryGetFestivalDataForYear(name, out string text);
                var original = new Dialogue(actor, "test/native", text);
                festival.TryGetFestivalDialogueForYear(actor, name, out Dialogue first);
                festival.TryGetFestivalDialogueForYear(actor, name, out Dialogue second);
                int extra = name == "Lewis" ? 0 : 1;
                bool appended = first.dialogues.Count == original.dialogues.Count + extra;
                bool prefixPreserved = original.dialogues.Select(d => d.Text).SequenceEqual(first.dialogues.Take(original.dialogues.Count).Select(d => d.Text));
                results.Add(new { Name = name + " native text preserved, remark once, host unchanged", Passed = appended && prefixPreserved && second.dialogues.Count == original.dialogues.Count });
            }
            finally { remarked.Remove(name); if (wasRemarked) remarked.Add(name); }
        }
        results.Add(new
        {
            Name = "Dialogue hook leaves native scripts, props, actor positions and timer intact",
            Passed = ReferenceEquals(commands, festival.eventCommands) && timer == festival.festivalTimer
                && props.SequenceEqual(festival.festivalProps) && festival.actors.All(n => positions[n.Name] == n.Position)
        });
        Helper.Data.WriteJsonFile("festival-qa.json", new { Checks = results, Rain = Game1.currentLocation.IsRainingHere(), Anchors = anchors.Select(p => new { p.X, p.Y }).ToArray() });
        captureRequested = true; captureDelay = 2;
    }

    private void WriteStatus()
    {
        Helper.Data.WriteJsonFile("status.json", new
        {
            Ready = Context.IsWorldReady, Mode = Game1.gameMode, Day = Game1.dayOfMonth, Year = Game1.year,
            DaysPlayed = Game1.stats?.DaysPlayed, DrawCount = drawCount, CapturePending = captureRequested,
            IsActive = Game1.game1.IsActive, PauseWhenOutOfFocus = Game1.options.pauseWhenOutOfFocus,
            Season = Game1.season.ToString(), Time = Game1.timeOfDay,
            Location = Game1.currentLocation?.Name, Viewport = Game1.viewport.ToString(),
            Event = Game1.CurrentEvent?.id, Sequence = Game1.CurrentEvent?.playerControlSequenceID,
            MainEvent = Game1.CurrentEvent?.eventSwitched,
            Menu = Game1.activeClickableMenu?.GetType().Name,
            Minigame = Game1.currentMinigame?.GetType().Name,
            Enabled = Context.IsWorldReady ? Get(Runtime, "Enabled") : null,
            Raining = Context.IsWorldReady && Game1.currentLocation?.IsRainingHere() == true,
            Outdoors = Game1.currentLocation?.IsOutdoors,
            Actors = Game1.CurrentEvent?.actors.Select(n => new { n.Name, X = n.Position.X, Y = n.Position.Y }).ToArray()
        });
    }

    private void RunChecks()
    {
        var results = new List<object>();
        int failures = 0;
        void Check(string name, Func<bool> test)
        {
            try
            {
                bool passed = test(); if (!passed) failures++;
                results.Add(new { Name = name, Passed = passed });
                Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
            }
            catch (Exception ex)
            {
                failures++; results.Add(new { Name = name, Passed = false, Error = ex.GetBaseException().Message });
                Monitor.Log($"FAIL: {name}: {ex.GetBaseException().Message}", LogLevel.Error);
            }
        }

        int day = Game1.dayOfMonth, year = Game1.year, time = Game1.timeOfDay;
        uint daysPlayed = Game1.stats.DaysPlayed;
        Season season = Game1.season;
        bool enabled = State.Enabled; bool? pending = State.PendingEnabled;
        bool rain = Game1.isRaining, snow = Game1.isSnowing, lightning = Game1.isLightning, debris = Game1.isDebrisWeather, green = Game1.isGreenRain;
        string tomorrow = Game1.weatherForTomorrow;
        var drops = Game1.rainDrops;
        var weatherDebris = Game1.debrisWeather.ToArray();
        bool wasRaining = Game1.wasRainingYesterday;
        var random = Game1.random;
        string? song = Game1.currentSong?.Name;
        var native = new Dictionary<string, LocationWeather>();
        foreach (string context in Game1.netWorldState.Value.LocationWeather.Keys.ToArray())
        {
            var copy = new LocationWeather(); copy.CopyFrom(Game1.netWorldState.Value.GetWeatherForLocation(context)); native[context] = copy;
        }
        var privateMaps = new Dictionary<IDictionary, List<DictionaryEntry>>();
        foreach (string field in new[] { "forced", "forecasts", "protections", "localWeather", "originals" })
        {
            var map = (IDictionary)Runtime.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Runtime)!;
            var entries = new List<DictionaryEntry>();
            foreach (DictionaryEntry entry in map) entries.Add(entry);
            privateMaps[map] = entries;
            if (field != "originals") map.Clear();
        }
        var runtimeFields = new Dictionary<FieldInfo, object?>();
        foreach (string name in new[] { "endingDay", "begunDay", "faulted" })
        {
            FieldInfo field = Runtime.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
            runtimeFields[field] = field.GetValue(Runtime);
        }
        object watered = Runtime.GetType().GetField("watered", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Runtime)!;
        object[] wateredEntries = ((IEnumerable)watered).Cast<object>().ToArray();
        var soilStates = new Dictionary<HoeDirt, int>();
        Utility.ForEachLocation(location =>
        {
            foreach (var feature in location.terrainFeatures.Values)
                if (feature is HoeDirt soil) soilStates[soil] = soil.state.Value;
            return true;
        });
        object? oldVisual = Runtime.GetType().GetField("lastVisual", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Runtime);
        GameLocation farm = Game1.getFarm();
        Vector2 tile = new(64, 15);
        farm.terrainFeatures.TryGetValue(tile, out TerrainFeature? original);
        try
        {
            Game1.random = new Random(102);
            Game1.season = Season.Spring; Game1.year = 1; Game1.dayOfMonth = 6; Game1.timeOfDay = 630;
            Game1.stats.DaysPlayed = 6;
            State.Enabled = false; State.PendingEnabled = null;
            Check("Disabled weather returns the native weather object", () => ReferenceEquals(farm.GetWeather(), Game1.netWorldState.Value.GetWeatherForLocation(farm.GetLocationContextId())));
            Call(Runtime, "RequestEnabled", true);
            Check("Enable request waits until morning", () => !State.Enabled && State.PendingEnabled == true);
            State.ApplyPendingEnable();
            Check("Morning applies requested enable", () => State.Enabled && State.PendingEnabled == null);
            Call(Runtime, "Force", Region.Farm, WeatherKind.Rain);
            Call(Runtime, "Force", Region.Town, WeatherKind.Rain);
            Call(Runtime, "Force", Region.Island, WeatherKind.Snow);
            Check("Town receives local rain", () => Game1.getLocationFromName("Town").IsRainingHere());
            Check("Island snow remains independent of town rain", () => Game1.getLocationFromName("IslandSouth").IsSnowingHere() && !Game1.getLocationFromName("IslandSouth").IsRainingHere());
            farm.terrainFeatures.Remove(tile);
            Check("Native tilling succeeds with the actual patch", () => farm.makeHoeDirt(tile, true));
            Check("New soil remains dry after 30 minutes of rain", () => ((HoeDirt)farm.terrainFeatures[tile]).state.Value == 0);
            Game1.timeOfDay = 700;
            Call(Runtime, "AdjustNewSoil", farm, tile);
            Check("New soil waters after 60 minutes of rain", () => ((HoeDirt)farm.terrainFeatures[tile]).state.Value == 1);
            farm.terrainFeatures.Remove(tile);
            Check("Native tilling patch waters newly created soil at threshold", () => farm.makeHoeDirt(tile, true) && ((HoeDirt)farm.terrainFeatures[tile]).state.Value == 1);
            Check("Ordinary-day TV uses simulated forecast", () =>
            {
                State.Enabled = false;
                string nativeReport = (string)Call(new TV(), "getWeatherForecast")!;
                State.Enabled = true;
                string report = (string)Call(new TV(), "getWeatherForecast")!;
                return report.Length > 0 && report != nativeReport;
            });
            State.Enabled = true; Game1.dayOfMonth = 23; Game1.stats.DaysPlayed = 23;
            Check("Flower Dance TV retains native special announcement", () =>
            {
                State.Enabled = false;
                string nativeReport = (string)Call(new TV(), "getWeatherForecast")!;
                State.Enabled = true;
                string report = (string)Call(new TV(), "getWeatherForecast")!;
                return report.StartsWith(nativeReport + "^^", StringComparison.Ordinal) && report.Length > nativeReport.Length + 2;
            });
            Check("Real overnight pipeline applies pending enable and prevents native rain watering", () =>
            {
                Game1.dayOfMonth = 6; Game1.stats.DaysPlayed = 6; Game1.timeOfDay = 630;
                State.Enabled = false; State.PendingEnabled = null;
                Call(Runtime, "RequestEnabled", true);
                Call(Runtime, "EndDay");
                Game1.dayOfMonth = 7; Game1.stats.DaysPlayed = 7;
                Game1.UpdateWeatherForNewDay();
                Call(Runtime, "Force", Region.Farm, WeatherKind.Rain);
                bool held = State.Enabled && !farm.IsRainingHere();
                Call(Runtime, "StartDay");
                return held && farm.IsRainingHere();
            });
            Check("Subthreshold rain preserves manually watered soil", () =>
            {
                Game1.timeOfDay = 630;
                ((HoeDirt)farm.terrainFeatures[tile]).state.Value = 1;
                Call(Runtime, "WaterThrough", 390);
                return ((HoeDirt)farm.terrainFeatures[tile]).state.Value == 1;
            });
            Check("Early sleep credits remaining rain before native crop growth", () =>
            {
                ((HoeDirt)farm.terrainFeatures[tile]).state.Value = 0;
                Call(Runtime, "EndDay");
                return ((HoeDirt)farm.terrainFeatures[tile]).state.Value == 1;
            });
            Check("Real next-morning disable restores native forecast progression", () =>
            {
                var originals = (IDictionary)Runtime.GetType().GetField("originals", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Runtime)!;
                string expected = ((LocationWeather)originals["Default"]!).WeatherForTomorrow;
                Call(Runtime, "RequestEnabled", false);
                if (!State.Enabled || State.PendingEnabled != false) return false;
                Game1.dayOfMonth = 8; Game1.stats.DaysPlayed = 8;
                Game1.UpdateWeatherForNewDay();
                Call(Runtime, "StartDay");
                var weather = Game1.netWorldState.Value.GetWeatherForLocation("Default");
                return !State.Enabled && State.PendingEnabled == null && ReferenceEquals(farm.GetWeather(), weather)
                    && weather.Weather == Game1.getWeatherModificationsForDate(Game1.Date, expected);
            });
            Check("Active Desert Festival preserves sunny desert weather", () =>
            {
                State.Enabled = true; Game1.dayOfMonth = 15; Game1.stats.DaysPlayed = 15;
                bool vault = Game1.player.mailReceived.Contains("ccVault"), joja = Game1.player.mailReceived.Contains("jojaVault");
                try
                {
                    if (!vault) Game1.player.mailReceived.Add("ccVault");
                    if (!joja) Game1.player.mailReceived.Add("jojaVault");
                    var data = DataLoader.PassiveFestivals(Game1.content).Values.First(p => p.MapReplacements?.ContainsKey("Desert") == true);
                    if (!GameStateQuery.CheckConditions(data.Condition)) throw new InvalidOperationException("Desert Festival fixture did not satisfy condition: " + data.Condition);
                    return (string?)Call(Runtime, "GetSpecialWeather", Region.Desert, 0) == "Sun";
                }
                finally
                {
                    if (!vault) Game1.player.mailReceived.Remove("ccVault");
                    if (!joja) Game1.player.mailReceived.Remove("jojaVault");
                }
            });
            Check("Tomorrow's NPC wedding appears in special-weather forecast", () =>
            {
                Game1.dayOfMonth = 6; Game1.stats.DaysPlayed = 6;
                string? spouse = Game1.player.spouse;
                Game1.player.friendshipData.TryGetValue("Abigail", out Friendship? friendship);
                try
                {
                    Game1.player.spouse = "Abigail";
                    Game1.player.friendshipData["Abigail"] = new Friendship(2500)
                    {
                        Status = FriendshipStatus.Engaged,
                        WeddingDate = new WorldDate(Game1.Date) { TotalDays = Game1.Date.TotalDays + 1 }
                    };
                    return (string?)Call(Runtime, "GetSpecialWeather", Region.Farm, 1) == "Wedding";
                }
                finally
                {
                    Game1.player.spouse = spouse;
                    if (friendship is null) Game1.player.friendshipData.Remove("Abigail"); else Game1.player.friendshipData["Abigail"] = friendship;
                }
            });
            Check("Forced storm remains active for previous-day overnight lightning", () =>
            {
                Game1.dayOfMonth = 8; Game1.stats.DaysPlayed = 8; Game1.timeOfDay = 630;
                State.Enabled = true;
                Call(Runtime, "StartDay");
                Call(Runtime, "Force", Region.Farm, WeatherKind.Storm);
                int ending = Game1.Date.TotalDays;
                var engine = (WeatherEngine)Get(Runtime, "Engine")!;
                int minute = Enumerable.Range(6, 18).Select(h => h * 60).First(m => engine.GetWeather(ending, m, Region.Farm).Kind != WeatherKind.Storm);
                Call(Runtime, "EndDay");
                Game1.dayOfMonth = 9; Game1.stats.DaysPlayed = 9;
                bool wasEvent = Game1.eventUp;
                try
                {
                    Game1.eventUp = false;
                    return ((WeatherSample)Call(Runtime, "Sample", ending, minute, Region.Farm)!).Kind == WeatherKind.Storm
                        && (bool)Call(Runtime, "AllowLightning", minute / 60 * 100)!;
                }
                finally { Game1.eventUp = wasEvent; }
            });
        }
        finally
        {
            if (original is null) farm.terrainFeatures.Remove(tile); else farm.terrainFeatures[tile] = original;
            Game1.dayOfMonth = day; Game1.year = year; Game1.season = season; Game1.timeOfDay = time; Game1.stats.DaysPlayed = daysPlayed;
            State.Enabled = enabled; State.PendingEnabled = pending;
            foreach (var (map, entries) in privateMaps) { map.Clear(); foreach (DictionaryEntry entry in entries) map.Add(entry.Key, entry.Value); }
            foreach (var (field, value) in runtimeFields) field.SetValue(Runtime, value);
            watered.GetType().GetMethod("Clear")!.Invoke(watered, null);
            foreach (object entry in wateredEntries) watered.GetType().GetMethod("Add")!.Invoke(watered, new[] { entry });
            foreach (var (soil, value) in soilStates) { soil.state.Value = value; soil.updateNeighbors(); }
            Runtime.GetType().GetField("lastVisual", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(Runtime, oldVisual);
            foreach (var (context, weather) in native) Game1.netWorldState.Value.GetWeatherForLocation(context).CopyFrom(weather);
            Game1.isRaining = rain; Game1.isSnowing = snow; Game1.isLightning = lightning; Game1.isDebrisWeather = debris; Game1.isGreenRain = green;
            Game1.weatherForTomorrow = tomorrow; Game1.rainDrops = drops; Game1.random = random; Game1.updateWeatherIcon();
            Game1.wasRainingYesterday = wasRaining; Game1.debrisWeather.Clear(); Game1.debrisWeather.AddRange(weatherDebris);
            if (Game1.currentSong?.Name != song) Game1.changeMusicTrack(song ?? "none");
            Helper.Data.WriteJsonFile("results.json", new { Failures = failures, Checks = results, Restored = true });
        }
        WriteStatus();
    }
}

