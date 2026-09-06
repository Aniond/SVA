using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void RequireRomanceFixture()
    {
        RequireWorld();
        if (Game1.player.farmName.Value != "Solace") throw new InvalidOperationException("Destructive romance fixtures are restricted to the backed-up Solace test copy.");
    }

    private (object Service, RomanceSaveState State, object Native, object Dates) LiveRomanceFixture()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        object mod = AiMod(Helper);
        object service = mod.GetType().GetField("romance", flags)!.GetValue(mod)!;
        var type = service.GetType();
        return (service, (RomanceSaveState)type.GetProperty("State", flags)!.GetValue(service)!,
            type.GetProperty("Native", flags)!.GetValue(service)!, type.GetProperty("Dates", flags)!.GetValue(service)!);
    }

    // These methods deliberately persist fixtures. Restore the entire external save backup after testing.
    private void RomanceOvernightStage()
    {
        RequireRomanceFixture();
        var live = LiveRomanceFixture();
        int day = Game1.Date.TotalDays;
        if (!live.State.IsValid()) throw new InvalidOperationException("Load the managed relationship save first.");
        live.State.Characters["Abigail"] = new RomanceCharacterState { IsDating = true, InterestExpressed = true,
            FirstContactDay = Math.Max(0, day - 28), DatingSinceDay = day, ConflictFreeSinceDay = day };
        if (!Game1.player.friendshipData.ContainsKey("Abigail")) Game1.player.friendshipData["Abigail"] = new Friendship();
        Game1.player.friendshipData["Abigail"].Status = FriendshipStatus.Dating;
        live.State.LatestDay = day;
        Call(live.Native, "ImportNative", day);

        // Put the actual witness beside the farmer, record line-of-sight evidence through production Signal,
        // then place the informed partner beside that witness before delivering the attributed report.
        Game1.exitActiveMenu();
        Game1.currentLocation = Game1.getLocationFromName("Town");
        Game1.player.currentLocation = Game1.currentLocation;
        Game1.player.Position = new Vector2(35, 60) * 64;
        Game1.player.isInBed.Value = false;
        NPC robin = Game1.getCharacterFromName("Robin");
        NPC leah = Game1.getCharacterFromName("Leah");
        NPC abigail = Game1.getCharacterFromName("Abigail");
        Game1.warpCharacter(abigail, "Farm", new Point(64, 15));
        Game1.warpCharacter(robin, "Town", new Point(36, 60));
        Game1.warpCharacter(leah, "Town", new Point(35, 61));
        robin.isSleeping.Value = false;
        int before = live.State.NextIncidentSequence;
        Call(live.Service, "Signal", "Leah", "flirt");
        var incident = live.State.Incidents.Single(i => i.Sequence == before);
        if (!live.State.Knowledge.Any(k => k.IncidentId == incident.Id && k.KnowerNpc == "Robin" && k.Hops == 0))
            throw new InvalidOperationException("The staged Robin did not actually witness the action; no gossip fixture was fabricated.");
        Game1.warpCharacter(abigail, "Town", new Point(36, 61));
        live.State.ReporterLastDay.Remove("Robin");
        if (!RomanceRules.SendReport(live.State, "Robin", "Abigail", incident.Id, day))
            throw new InvalidOperationException("Could not deliver the witnessed report to Abigail.");
        if (!RomanceRules.DeliverWarning(live.State, "Abigail", day) || !RomanceRules.AcknowledgeConflict(live.State, "Abigail", day))
            throw new InvalidOperationException("The conflict warning and repair agreement were not accepted.");
        live.State.Booking = null;
        if (!RomanceRules.Book(live.State, "Leah", day + 3, 1080, day, 600, "saloon-conversation"))
            throw new InvalidOperationException("The future continuity appointment could not be staged.");
        Call(live.Dates, "EnsureQuest");
        Helper.Data.WriteJsonFile("romance-overnight-expected.json", new RomanceOvernightExpectation
        {
            FarmerId = Game1.player.UniqueMultiplayerID, StageDay = day, BookingDay = day + 3,
            IncidentId = incident.Id, HistoryCount = live.State.History.Count,
            Children = Game1.player.getChildren().Select(c => c.Name).OrderBy(n => n).ToArray()
        });
    }

    private void RomanceOvernightCheck()
    {
        RequireRomanceFixture();
        var expected = Helper.Data.ReadJsonFile<RomanceOvernightExpectation>("romance-overnight-expected.json")!;
        var state = LiveRomanceFixture().State;
        var character = state.Characters.GetValueOrDefault("Abigail");
        var knowledge = state.Knowledge.FirstOrDefault(k => k.KnowerNpc == "Abigail" && k.IncidentId == expected.IncidentId);
        bool twoOvernights = Game1.Date.TotalDays >= expected.StageDay + 2;
        bool appointment = state.Booking is { Npc: "Leah", Type: "saloon-conversation", StartMinute: 1080, Arrived: false } b && b.Day == expected.BookingDay;
        bool repairedAgreement = character is { InConflict: true, RepairAcknowledged: true } && character.WarningDeliveredDay == expected.StageDay;
        bool attribution = knowledge is { Source: "Robin", OriginalEyewitness: "Robin", Hops: 1 };
        bool nativeDating = Game1.player.friendshipData["Abigail"].IsDating();
        bool children = expected.Children.SequenceEqual(Game1.player.getChildren().Select(c => c.Name).OrderBy(n => n));
        Helper.Data.WriteJsonFile("romance-overnight-results.json", new
        {
            Passed = expected.FarmerId == Game1.player.UniqueMultiplayerID && twoOvernights && state.IsValid() && appointment && repairedAgreement && attribution && nativeDating && children && state.History.Count >= expected.HistoryCount,
            ActualDay = Game1.Date.TotalDays, expected.StageDay, TwoOvernights = twoOvernights, AppointmentPreserved = appointment,
            ConflictAndRepairAgreementPreserved = repairedAgreement, SourcePreserved = attribution, NativeDating = nativeDating,
            ChildrenPreserved = children, HistoryPreserved = state.History.Count >= expected.HistoryCount, Valid = state.IsValid()
        });
    }

    private void RomanceMarriageStage()
    {
        RequireRomanceFixture();
        var live = LiveRomanceFixture();
        int day = Game1.Date.TotalDays;
        if (day < 56) throw new InvalidOperationException("Use a Solace fixture at least 56 days old for the real proposal eligibility test.");
        if (Game1.player.spouse != null) throw new InvalidOperationException("The marriage fixture requires no existing spouse or engagement.");
        var tomorrow = new WorldDate(Game1.Date) { TotalDays = day + 1 };
        if (!Game1.canHaveWeddingOnDay(tomorrow.DayOfMonth, tomorrow.Season))
            throw new InvalidOperationException("Tomorrow is not a native wedding day; stage this fixture on another day.");
        var home = Utility.getHomeOfFarmer(Game1.player);
        var baseline = new RomanceMarriageExpectation { FarmerId = Game1.player.UniqueMultiplayerID, StageDay = day,
            Children = Game1.player.getChildren().Select(c => c.Name).OrderBy(n => n).ToArray(), PreviousHouseLevel = Game1.player.HouseUpgradeLevel };
        if (Game1.player.HouseUpgradeLevel < 2)
        {
            home.moveObjectsForHouseUpgrade(2);
            Game1.player.HouseUpgradeLevel = 2;
            home.setMapForUpgradeLevel(2);
        }
        live.State.Characters["Sam"] = new RomanceCharacterState { FirstContactDay = day - 56, TalkDays = 24,
            CompletedActivities = 12, ActivityTypes = new() { "town-walk", "saloon-conversation" }, RomanticDates = 8,
            IsDating = true, InterestExpressed = true, DatingSinceDay = day - 56, ConflictFreeSinceDay = day - 14 };
        if (!Game1.player.friendshipData.ContainsKey("Sam")) Game1.player.friendshipData["Sam"] = new Friendship();
        Game1.player.friendshipData["Sam"].Status = FriendshipStatus.Dating;
        live.State.LatestDay = day;
        Call(live.Native, "ImportNative", day);
        int slot = Enumerable.Range(0, Game1.player.Items.Count).FirstOrDefault(i => Game1.player.Items[i] == null, -1);
        if (slot < 0) throw new InvalidOperationException("Leave one backpack slot empty for the test pendant.");
        Game1.player.Items[slot] = ItemRegistry.Create("(O)460");
        var args = new object?[] { Game1.getCharacterFromName("Sam"), null };
        bool accepted = (bool)live.Native.GetType().GetMethod("TryPropose")!.Invoke(live.Native, args)!;
        if (!accepted) throw new InvalidOperationException("Native proposal fixture failed: " + args[1]);
        var friendship = Game1.player.friendshipData["Sam"];
        // The proposal itself used the real native handler. Shorten only its waiting period for this test.
        friendship.WeddingDate = tomorrow;
        baseline.WeddingDay = day + 1;
        baseline.PendantConsumed = Game1.player.Items[slot] == null;
        Helper.Data.WriteJsonFile("romance-marriage-expected.json", baseline);
        Helper.Data.WriteJsonFile("romance-marriage-stage.json", new { Accepted = accepted, NativeEngaged = friendship.IsEngaged(),
            Spouse = Game1.player.spouse, WeddingDay = friendship.WeddingDate.TotalDays, baseline.PendantConsumed });
    }

    private void RomanceMarriageFinishCeremony()
    {
        RequireRomanceFixture();
        var ceremony = Game1.currentLocation.currentEvent;
        if (ceremony?.isWedding != true) throw new InvalidOperationException("The native wedding ceremony must already be active.");
        // Generic skipEvent calls endBehaviors without the wedding argument, so invoke the actual
        // wedding end command to retain spouse porch placement and the native post-wedding dialogue.
        ceremony.endBehaviors(new[] { "End", "wedding" }, Game1.currentLocation);
    }

    private void RomanceMarriageCheck()
    {
        RequireRomanceFixture();
        var expected = Helper.Data.ReadJsonFile<RomanceMarriageExpectation>("romance-marriage-expected.json")!;
        var live = LiveRomanceFixture();
        var home = Utility.getHomeOfFarmer(Game1.player);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        string? room = (string?)typeof(FarmHouse).GetField("lastSpouseRoom", flags)!.GetValue(home);
        bool displaying = (bool)typeof(FarmHouse).GetField("displayingSpouseRoom", flags)!.GetValue(home)!;
        bool native = Game1.player.spouse == "Sam" && Game1.player.friendshipData["Sam"].IsMarried();
        bool core = live.State.Characters.GetValueOrDefault("Sam") is { IsMarried: true, IsEngaged: false };
        bool children = expected.Children.SequenceEqual(Game1.player.getChildren().Select(c => c.Name).OrderBy(n => n));
        Helper.Data.WriteJsonFile("romance-marriage-results.json", new { Passed = expected.FarmerId == Game1.player.UniqueMultiplayerID
            && Game1.Date.TotalDays >= expected.WeddingDay && !Game1.eventUp && native && core && displaying && room == "Sam" && children && expected.PendantConsumed,
            Day = Game1.Date.TotalDays, NativeMarried = native, CoreMarried = core, CeremonyFinished = !Game1.eventUp,
            SpouseRoomVisible = displaying, SpouseRoom = room, ChildrenPreserved = children, Children = Game1.player.getChildren().Select(c => c.Name).ToArray(),
            SamLocation = Game1.getCharacterFromName("Sam").currentLocation.Name, expected.PendantConsumed });
    }

    private void RomanceDivorceStage() => StageRomanceDivorce(false, false);
    private void RomanceDivorceWithChildStage() => StageRomanceDivorce(true, false);
    private void RomancePendingBirthStage() => StageRomanceDivorce(false, true);

    private static string RomanceInventorySnapshot() => JsonSerializer.Serialize(Game1.player.Items.Select((item, slot) => new
    {
        Slot = slot, Id = item?.QualifiedItemId, Stack = item?.Stack ?? 0,
        Protected = item?.modData.ContainsKey(SolaceWeather.Controls.QuickStack.ProtectedKey) == true,
        ProtectionValue = item != null && item.modData.TryGetValue(SolaceWeather.Controls.QuickStack.ProtectedKey, out var value) ? value : null
    }).ToArray());

    private void StageRomanceDivorce(bool addChild, bool pendingBirth)
    {
        RequireRomanceFixture();
        if (Game1.player.spouse != "Sam" || !Game1.player.friendshipData["Sam"].IsMarried())
            throw new InvalidOperationException("Complete the actual Sam wedding before staging this lifecycle test.");
        var live = LiveRomanceFixture();
        var home = Utility.getHomeOfFarmer(Game1.player);
        if (addChild && !Game1.player.getChildren().Any(c => c.Name == "RomanceFixtureChild"))
            home.characters.Add(new Child("RomanceFixtureChild", true, false, Game1.player));
        if (pendingBirth) Game1.player.friendshipData["Sam"].NextBirthingDate = new WorldDate(Game1.Date) { TotalDays = Game1.Date.TotalDays + 3 };
        var character = live.State.Characters["Sam"];
        character.InConflict = true;
        character.PendingTransition = "divorce";
        character.LastViolationDay = Game1.Date.TotalDays;
        Helper.Data.WriteJsonFile("romance-divorce-expected.json", new RomanceDivorceExpectation { FarmerId = Game1.player.UniqueMultiplayerID,
            FarmOwnerId = Game1.MasterPlayer.UniqueMultiplayerID, FarmName = Game1.player.farmName.Value,
            Money = Game1.player.Money, Inventory = RomanceInventorySnapshot(),
            StageDay = Game1.Date.TotalDays, PendingBirth = pendingBirth, Children = Game1.player.getChildren().Select(c => c.Name).OrderBy(n => n).ToArray() });
    }

    private void RomanceDivorceCheck()
    {
        RequireRomanceFixture();
        var expected = Helper.Data.ReadJsonFile<RomanceDivorceExpectation>("romance-divorce-expected.json")!;
        var live = LiveRomanceFixture();
        var friendship = Game1.player.friendshipData["Sam"];
        var character = live.State.Characters["Sam"];
        bool children = expected.Children.SequenceEqual(Game1.player.getChildren().Select(c => c.Name).OrderBy(n => n));
        bool delayed = expected.PendingBirth && friendship.NextBirthingDate != null && Game1.player.spouse == "Sam" && friendship.IsMarried() && character.PendingTransition == "divorce";
        NPC formerSpouse = Game1.getCharacterFromName("Sam");
        bool homeRestored = formerSpouse.DefaultMap == "SamHouse" && formerSpouse.currentLocation.Name != "FarmHouse";
        bool divorced = homeRestored && Game1.player.spouse == null && friendship.IsDivorced() && !character.IsMarried && !character.IsDating && character.PendingTransition == null;
        var home = Utility.getHomeOfFarmer(Game1.player);
        bool roomVisible = (bool)typeof(FarmHouse).GetField("displayingSpouseRoom", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(home)!;
        bool ownership = expected.FarmerId == Game1.player.UniqueMultiplayerID && expected.FarmOwnerId == Game1.MasterPlayer.UniqueMultiplayerID && expected.FarmName == Game1.player.farmName.Value;
        bool money = expected.Money == Game1.player.Money;
        bool inventory = expected.Inventory == RomanceInventorySnapshot();
        Helper.Data.WriteJsonFile("romance-divorce-results.json", new { Passed = ownership && money && inventory
            && Game1.Date.TotalDays > expected.StageDay && children && (expected.PendingBirth ? delayed : divorced && !roomVisible),
            Day = Game1.Date.TotalDays, expected.PendingBirth, DivorceDeferredForBirth = delayed, NativeAndCoreDivorced = divorced,
            ChildrenPreserved = children, OwnershipPreserved = ownership, MoneyPreserved = money, InventoryAndProtectionPreserved = inventory,
            ExpectedMoney = expected.Money, ActualMoney = Game1.player.Money, SpouseRoomVisible = roomVisible, Spouse = Game1.player.spouse, NativeStatus = friendship.Status.ToString(), FormerSpouseLocation = formerSpouse.currentLocation.Name, FormerSpouseDefaultMap = formerSpouse.DefaultMap, HomeRestored = homeRestored });
    }

    public sealed class RomanceOvernightExpectation
    {
        public long FarmerId { get; set; }
        public int StageDay { get; set; }
        public int BookingDay { get; set; }
        public string IncidentId { get; set; } = "";
        public int HistoryCount { get; set; }
        public string[] Children { get; set; } = Array.Empty<string>();
    }
    public sealed class RomanceMarriageExpectation
    {
        public long FarmerId { get; set; }
        public int StageDay { get; set; }
        public int WeddingDay { get; set; }
        public int PreviousHouseLevel { get; set; }
        public bool PendantConsumed { get; set; }
        public string[] Children { get; set; } = Array.Empty<string>();
    }
    public sealed class RomanceDivorceExpectation
    {
        public long FarmerId { get; set; }
        public long FarmOwnerId { get; set; }
        public string FarmName { get; set; } = "";
        public int Money { get; set; }
        public string Inventory { get; set; } = "";
        public int StageDay { get; set; }
        public bool PendingBirth { get; set; }
        public string[] Children { get; set; } = Array.Empty<string>();
    }
}
