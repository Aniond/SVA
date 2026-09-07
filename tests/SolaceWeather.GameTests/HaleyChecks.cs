using System.Text.Json;
using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private object Haley() { Phone(); return PhoneGet<object>(AiMod(Helper), "haley"); }
    private object Fashion() { Phone(); return PhoneGet<object>(AiMod(Helper), "fashion"); }
    private void HaleyState()
    {
        var haley = Haley(); var romance = PhoneGet<object>(Phone(), "romance");
        Helper.Data.WriteJsonFile("haley-state.json", new { Ready = PhoneGet<bool>(haley, "Ready"), State = PhoneGet<HaleyLifeState>(haley, "State"),
            Context = Call(haley, "ContextFor", "Haley", false), Day = Game1.Date.TotalDays, Time = Game1.timeOfDay, Game1.player.CanMove,
            Menu = Game1.activeClickableMenu?.GetType().Name, HaleyLocation = Game1.getCharacterFromName("Haley").currentLocation?.Name,
            Memory = JsonSerializer.Serialize(Call(romance, "GetPhoneContext", "Haley", "our photo walk and sunflower promise")),
            Fashion = PhoneGet<FashionMemoryState>(Fashion(), "State"), Outfit = Call(Fashion(), "Outfit") });
    }
    private void HaleyStage()
    {
        var haley = Haley(); StageBackgroundProgress(); Call(Chatter(), "Stop"); PhoneSet(Chatter(), "nextAttempt", double.MaxValue);
        Game1.exitActiveMenu(); Game1.player.isInBed.Value = false; Game1.player.CanMove = true; Game1.timeOfDay = 1000;
        Game1.isRaining = false; Game1.isGreenRain = false;
        var npc = Game1.getCharacterFromName("Haley"); npc.ignoreScheduleToday = false; npc.followSchedule = true; npc.TryLoadSchedule();
        npc.Halt(); npc.controller = null; npc.temporaryController = null; npc.isSleeping.Value = false; npc.doingEndOfRouteAnimation.Value = false;
        Game1.warpFarmer("Town", 35, 62, false); Game1.warpCharacter(npc, "Town", new Point(35,61));
        Call(PhoneGet<object>(Phone(), "romance"), "Contact", npc);
        if (!Game1.player.Items.Any(i => i?.QualifiedItemId == "(O)421") && !Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)421"))) throw new InvalidOperationException("No room for guarded sunflower fixture.");
        Call(haley, "ObservePromise"); Call(haley, "FindPhotoSpot"); HaleyState();
    }
    private void HaleyPromise()
    {
        var haley = Haley(); Call(haley, "Reply", "Haley", new ConversationReply { Reply = "A sunflower could work for a still life.", QuestRequest = "haley-sunflower" });
        var state = PhoneGet<HaleyLifeState>(haley, "State"); string stamp = $"haley:promise:{state.Promise.Status}:{state.Promise.OfferedDay}:{state.Promise.DueDay}:";
        if (Call(haley, "Apply", "Haley", stamp + "accept3") == null) throw new InvalidOperationException("Promise acceptance unavailable.");
        int before = Game1.player.Items.Where(i => i?.QualifiedItemId == "(O)421").Sum(i => i.Stack);
        var item = Game1.player.Items.OfType<StardewValley.Object>().Where(i => i.QualifiedItemId == "(O)421").OrderBy(i => i.Quality).First();
        stamp = $"haley:promise:{state.Promise.Status}:{state.Promise.OfferedDay}:{state.Promise.DueDay}:";
        bool delivered = Call(haley, "Apply", "Haley", stamp + "give:" + item.Quality) != null;
        int after = Game1.player.Items.Where(i => i?.QualifiedItemId == "(O)421").Sum(i => i.Stack);
        Helper.Data.WriteJsonFile("haley-delivery.json", new { Passed = delivered && after == before-1 && state.Promise.Status == "completed", Before = before, After = after, state.Promise });
        HaleyState();
    }
    private void HaleyOffer()
    {
        Call(Haley(), "Reply", "Haley", new ConversationReply { Reply = "Let's take a photo walk.", QuestRequest = "haley-photo" }); HaleyState();
    }
    private void HaleyAccept()
    {
        var haley = Haley(); var outing = PhoneGet<HaleyLifeState>(haley, "State").Outing;
        if (Call(haley, "Apply", "Haley", $"haley:photo:{outing.Status}:{outing.OfferDay}:{outing.MeetingDay}:accept") == null) throw new InvalidOperationException("Photo acceptance unavailable.");
        HaleyState();
    }
    private void HaleyMeet()
    {
        var haley = Haley(); var state = PhoneGet<HaleyLifeState>(haley, "State");
        if (state.Outing.Status != "accepted") throw new InvalidOperationException("Accept a photo invitation first.");
        var date = new WorldDate(Game1.Date) { TotalDays = state.Outing.MeetingDay };
        Game1.year = date.Year; Game1.season = date.Season; Game1.dayOfMonth = date.DayOfMonth; Game1.timeOfDay = 1700;
        Game1.exitActiveMenu(); var npc = Game1.getCharacterFromName("Haley"); npc.ignoreScheduleToday = false; npc.followSchedule = true; npc.TryLoadSchedule();
        npc.Halt(); npc.controller = null; npc.temporaryController = null; npc.isSleeping.Value = false; npc.doingEndOfRouteAnimation.Value = false;
        var tile = PhoneGet<Point?>(haley, "photoSpot")!.Value; Game1.warpFarmer("Town", tile.X, tile.Y+1, false);
        Call(haley, "TickPhoto"); HaleyState();
    }
    private void HaleyTalk()
    {
        var npc = Game1.getCharacterFromName("Haley");
        Game1.activeClickableMenu = new DialogueBox(new Dialogue(npc, null, "Ready to look around?")); Game1.player.CanMove = false;
        Call(Haley(), "TryBegin", npc); HaleyState(); captureRequested = true; captureDelay = 10;
    }
    private void HaleyAdvance()
    {
        var haley = Haley(); int step = PhoneGet<HaleyLifeState>(haley, "State").Outing.Step;
        Call(haley, "AdvancePhoto", step == 0 ? "wide" : step == 1 ? "candid" : "finish"); HaleyState(); captureRequested = true; captureDelay = 2;
    }
    private void HaleyLive()
    {
        var conversation = PhoneGet<object>(AiMod(Helper), "abigailConversation");
        Call(conversation, "Start", Game1.getCharacterFromName("Haley"));
        if (Game1.activeClickableMenu is not NamingMenu input) throw new InvalidOperationException("Accept the native phone exchange first, then retry the request.");
        string message = File.ReadAllText(Path.Combine(Helper.DirectoryPath, "haley-message.txt")).Trim();
        if (message.Length is < 1 or > 500) throw new InvalidOperationException("Use a bounded test message.");
        input.textBox.Text = message; input.textBoxEnter(input.textBox);
    }
    private void HaleyTree()
    {
        Call(Haley(), "OpenTree"); captureRequested = true; captureDelay = 10;
    }
    private void HaleyGuards()
    {
        var haley = Haley(); var state = PhoneGet<HaleyLifeState>(haley, "State");
        var original = state.Promise;
        var checks = new Dictionary<string, bool>();
        try
        {
            state.Promise = new HaleyPromiseState(); state.Promise.Observe("(O)421");
            state.Promise.Offer(Game1.Date.TotalDays);
            string stamp = $"haley:promise:offered:{state.Promise.OfferedDay}:{state.Promise.DueDay}:";
            checks["WrongNpcCannotAccept"] = Call(haley, "Apply", "Abigail", stamp + "accept3") == null && state.Promise.Status == "offered";
            checks["StaleChoiceCannotAccept"] = Call(haley, "Apply", "Haley", "haley:promise:offered:-99::accept3") == null && state.Promise.Status == "offered";
            checks["DeclineCreatesNoPromise"] = Call(haley, "Apply", "Haley", stamp + "decline") != null && state.Promise.Status == "declined";
            state.Promise = new HaleyPromiseState(); state.Promise.Observe("(O)421"); state.Promise.Offer(Game1.Date.TotalDays); state.Promise.Accept(Game1.Date.TotalDays, 3);
            var flowers = Game1.player.Items.OfType<StardewValley.Object>().Where(i => i.QualifiedItemId == "(O)421").ToArray();
            var protectedBefore = flowers.Select(i => i.questItem.Value).ToArray();
            int before = flowers.Sum(i => i.Stack);
            try
            {
                foreach (var flower in flowers) flower.questItem.Value = true;
                stamp = $"haley:promise:active:{state.Promise.OfferedDay}:{state.Promise.DueDay}:";
                checks["ProtectedFlowersNotConsumed"] = Call(haley, "Apply", "Haley", stamp + "give:0") == null && flowers.Sum(i => i.Stack) == before && state.Promise.Status == "active";
            }
            finally { for (int i = 0; i < flowers.Length; i++) flowers[i].questItem.Value = protectedBefore[i]; }
        }
        finally { state.Promise = original; Call(haley, "EnsureQuests"); }
        Helper.Data.WriteJsonFile("haley-guards.json", new { Passed = checks.Values.All(v => v), Checks = checks });
    }

    private void FashionCheck()
    {
        var fashion = Fashion(); var state = PhoneGet<FashionMemoryState>(fashion, "State");
        var checks = new Dictionary<string, bool>();
        var outfit = (FashionPiece[])Call(fashion, "Outfit")!;
        checks["NativeOutfitRead"] = outfit.Any(p => p.Slot == "shirt") && outfit.Any(p => p.Slot == "pants");
        Call(fashion, "ContextFor", "Haley", false);
        string before = JsonSerializer.Serialize(Call(fashion, "ContextFor", "Haley", true));
        var pants = Game1.player.pantsItem.Value; var color = pants?.clothesColor.Value;
        try
        {
            if (pants != null) pants.clothesColor.Value = Color.Magenta;
            checks["PhoneRetainsOnlyLastSeen"] = before == JsonSerializer.Serialize(Call(fashion, "ContextFor", "Haley", true));
            checks["PhoneDoesNotInventOtherNpcObservation"] = state.LastSeen("UnseenFashionAuditNpc") == null;
        }
        finally { if (pants != null && color.HasValue) pants.clothesColor.Value = color.Value; }
        Helper.Data.WriteJsonFile("fashion-checks.json", new { Passed = checks.Values.All(v => v), Checks = checks, Outfit = outfit, Phone = before });
    }
    private void ModernFashionStage()
    {
        Phone();
        const string stem = "David.AbigailModern_Modern_CleanMonochrome_";
        var shirt = ItemRegistry.Create("(S)" + stem + "Shirt") as StardewValley.Objects.Clothing;
        var pants = ItemRegistry.Create("(P)" + stem + "Pants") as StardewValley.Objects.Clothing;
        if (shirt == null || pants == null || shirt.Name == "Error Item" || pants.Name == "Error Item") throw new InvalidOperationException("The verified modern clothing pack is required.");
        Game1.player.shirtItem.Value = shirt; Game1.player.pantsItem.Value = pants;
        var pieces = (FashionPiece[])Call(Fashion(), "Outfit")!;
        if (pieces.Count(p => p.Definition != null) < 2) throw new InvalidOperationException("Modern clothing metadata was not loaded.");
        Helper.Data.WriteJsonFile("modern-fashion.json", new { Passed = true, Fixture = "Native clothing equipped only in SvaAudit profile", Outfit = pieces });
    }

    private void NativeFashionCheck()
    {
        var fashion = Fashion(); var state = PhoneGet<FashionMemoryState>(fashion, "State");
        var npc = Game1.getCharacterFromName("Caroline"); var origin = npc.currentLocation; var position = npc.Position;
        try
        {
            Game1.exitActiveMenu(); Game1.warpCharacter(npc, Game1.currentLocation, Game1.player.Tile + new Vector2(1,0));
            var dialogue = new Dialogue(npc, null, "Good morning. It's nice to see you.");
            var box = new StardewValley.Menus.DialogueBox(dialogue); Game1.activeClickableMenu = box;
            Call(fashion, "NativeComment", box);
            bool preserved = dialogue.dialogues[0].Text == "Good morning. It's nice to see you.";
            bool appended = dialogue.dialogues.Count == 2;
            Call(fashion, "MarkDisplayedNativeComment");
            bool notConsumed = state.LastSeen("Caroline")?.LastCommentDay != Game1.Date.TotalDays;
            Helper.Data.WriteJsonFile("native-fashion-checks.json", new { Passed = preserved && appended && notConsumed, OriginalPreserved = preserved, CommentAppended = appended, UnreadCommentNotConsumed = notConsumed });
            captureRequested = true; captureDelay = 10;
        }
        finally { Game1.warpCharacter(npc, origin, position / 64); npc.Position = position; }
    }
    private void HaleyRestChecks()
    {
        var haley = Haley(); var state = PhoneGet<HaleyLifeState>(haley, "State");
        var originalTree = state.Tree; var originalOuting = state.Outing;
        var npc = Game1.getCharacterFromName("Haley"); bool sleeping = npc.isSleeping.Value;
        float stamina = Game1.player.Stamina; int time = Game1.timeOfDay;
        var checks = new Dictionary<string, bool>();
        try
        {
            state.Tree = new(); state.Outing = new();
            for (int i = 0; i < 3; i++) state.Tree.RecordPhoto(i, "wide", "candid");
            state.Tree.Refresh(true, 3);
            Game1.exitActiveMenu(); Game1.timeOfDay = 1700; Game1.player.Stamina = 30;
            npc.isSleeping.Value = true;
            checks["SleepingNpcCannotRest"] = Call(haley, "Apply", "Haley", "haley:tree:rest") == null && Game1.timeOfDay == 1700 && Game1.player.Stamina == 30;
            npc.isSleeping.Value = false; Game1.player.Stamina = Game1.player.MaxStamina;
            checks["FullEnergyDoesNotConsumeRest"] = Call(haley, "Apply", "Haley", "haley:tree:rest") == null && state.Tree.LastRestDay < 0 && Game1.timeOfDay == 1700;
            Game1.player.Stamina = 30; npc.Halt(); npc.controller = null; npc.temporaryController = null; npc.doingEndOfRouteAnimation.Value = false;
            checks["RestCompletesTwentyMinutesThirtyEnergy"] = Call(haley, "Apply", "Haley", "haley:tree:rest") != null && Game1.timeOfDay == 1720 && Game1.player.Stamina == 60;
            checks["RepeatedRestRejected"] = Call(haley, "Apply", "Haley", "haley:tree:rest") == null && Game1.timeOfDay == 1720 && Game1.player.Stamina == 60;
        }
        finally { state.Tree = originalTree; state.Outing = originalOuting; npc.isSleeping.Value = sleeping; Game1.player.Stamina = stamina; Game1.timeOfDay = time; }
        Helper.Data.WriteJsonFile("haley-rest-checks.json", new { Passed = checks.Values.All(v => v), Fixture = "Milestones staged only for availability and cooldown checks", Checks = checks });
    }
    private void HaleyPhotoGuards()
    {
        var haley = Haley(); var state = PhoneGet<HaleyLifeState>(haley, "State");
        var original = state.Outing; int time = Game1.timeOfDay;
        var npc = Game1.getCharacterFromName("Haley"); var checks = new Dictionary<string, bool>();
        try
        {
            Game1.exitActiveMenu(); state.Outing = new(); state.Outing.Offer(Game1.Date.TotalDays, Game1.Date.TotalDays);
            Game1.timeOfDay = 1700;
            checks["InvitationAloneCannotBegin"] = !(bool)Call(haley, "TryBegin", npc)! && state.Outing.Status == "offered";
            state.Outing.Answer(Game1.Date.TotalDays, true); Game1.timeOfDay = 1650;
            checks["TooEarlyCannotBegin"] = !(bool)Call(haley, "TryBegin", npc)! && state.Outing.Status == "accepted";
            Game1.timeOfDay = 1700; npc.Halt(); npc.controller = null; npc.temporaryController = null;
            bool follow = npc.followSchedule, ignore = npc.ignoreScheduleToday;
            checks["AcceptedWalkBegins"] = (bool)Call(haley, "TryBegin", npc)! && !Game1.player.CanMove;
            Call(haley, "CancelPhoto");
            checks["CancelRestoresMovementAndSchedule"] = Game1.player.CanMove && npc.followSchedule == follow && npc.ignoreScheduleToday == ignore && state.Outing.Status == "missed" && state.Outing.Step == 0;
            state.Outing = new(); state.Outing.Offer(Game1.Date.TotalDays, Game1.Date.TotalDays); state.Outing.Answer(Game1.Date.TotalDays, true);
            Game1.timeOfDay = 1740; Call(haley, "TickPhoto");
            checks["LateArrivalRecordsNoCompletion"] = state.Outing.Status == "missed" && state.Outing.Step == 0;
        }
        finally { Call(haley, "CleanupPhoto"); state.Outing = original; Game1.timeOfDay = time; Call(haley, "EnsureQuests"); }
        Helper.Data.WriteJsonFile("haley-photo-guards.json", new { Passed = checks.Values.All(v => v), Fixture = "Temporary invitation states, no completion fabricated", Checks = checks });
    }
    private void SocialStageCapture()
    {
        Phone(); Game1.activeClickableMenu = new GameMenu(GameMenu.socialTab, -1, playOpeningSound:false);
        captureRequested = true; captureDelay = 10;
    }
    private void SocialNavigationChecks()
    {
        Phone(); var checks = new Dictionary<string, bool>();
        var points = Game1.player.friendshipData.Pairs.ToDictionary(p => p.Key, p => p.Value.Points);
        var menu = new GameMenu(GameMenu.socialTab, -1, playOpeningSound:false); Game1.activeClickableMenu = menu;
        var social = (SocialPage)menu.GetCurrentPage();
        int index = social.SocialEntries.FindIndex(e => e.InternalName == "Haley");
        if (index < 0) throw new InvalidOperationException("Haley is missing from the social list.");
        social.slotPosition = Math.Clamp(index, 0, Math.Max(0, social.SocialEntries.Count - 5)); social.updateSlots();
        int slot = social.slotPosition; var hit = social.characterSlots[index].bounds.Center;
        social.receiveLeftClick(hit.X, hit.Y);
        checks["HaleyRowOpensRelationshipJournal"] = !ReferenceEquals(Game1.activeClickableMenu, menu);
        Game1.activeClickableMenu.exitFunction?.Invoke();
        checks["ReturnPreservesScroll"] = Game1.activeClickableMenu is GameMenu returned && returned.GetCurrentPage() is SocialPage page && page.slotPosition == slot;
        checks["FriendshipPointsUnchanged"] = points.All(p => Game1.player.friendshipData[p.Key].Points == p.Value);
        Helper.Data.WriteJsonFile("social-navigation.json", new { Passed = checks.Values.All(v => v), Checks = checks });
        captureRequested = true; captureDelay = 10;
    }
}
