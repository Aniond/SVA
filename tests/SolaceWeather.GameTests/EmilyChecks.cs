using System.Text.Json;
using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private object Emily() { Phone(); return PhoneGet<object>(AiMod(Helper), "emily"); }
    private object Tailoring() { Phone(); return PhoneGet<object>(AiMod(Helper), "tailoring"); }
    private void EmilyState()
    {
        var service = Emily();
        Helper.Data.WriteJsonFile("emily-state.json", new { Ready = PhoneGet<bool>(service, "Ready"), State = PhoneGet<EmilyLifeState>(service, "State"),
            Context = Call(service, "ContextFor", "Emily", false), Day = Game1.Date.TotalDays, Time = Game1.timeOfDay, Game1.player.CanMove,
            Menu = Game1.activeClickableMenu?.GetType().Name, Location = Game1.getCharacterFromName("Emily").currentLocation?.Name,
            Debug = new { Boundaries = Call(service, "DesignBoundaries"), Spot = PhoneGet<Point?>(service, "designSpot"), PlayerTile = Game1.player.Tile,
                Available = Call(PhoneGet<object>(PhoneGet<object>(Phone(), "romance"), "Dates"), "Available", Game1.getCharacterFromName("Emily"), Game1.Date.TotalDays, 660, "town-walk"),
                Actor = PhoneGet<NPC?>(service, "designActor")?.Name, Game1.getCharacterFromName("Emily").ignoreScheduleToday,
                Moving = Game1.getCharacterFromName("Emily").isMoving(), Controller = Game1.getCharacterFromName("Emily").controller != null,
                Schedule = Game1.getCharacterFromName("Emily").Schedule?.Select(p => new { p.Key, p.Value.targetLocationName }).ToArray() },
            Memory = JsonSerializer.Serialize(Call(PhoneGet<object>(Phone(), "romance"), "GetPhoneContext", "Emily", "our design sessions and cloth promise")) });
    }
    private void EmilyStage()
    {
        Emily(); StageBackgroundProgress(); Call(Chatter(), "Stop"); PhoneSet(Chatter(), "nextAttempt", double.MaxValue);
        Game1.exitActiveMenu(); Game1.player.isInBed.Value = false; Game1.player.CanMove = true; Game1.timeOfDay = 1000;
        Game1.isRaining = false; Game1.isGreenRain = false;
        var npc = Game1.getCharacterFromName("Emily"); npc.ignoreScheduleToday = false; npc.followSchedule = true; npc.TryLoadSchedule();
        npc.Halt(); npc.controller = null; npc.temporaryController = null; npc.isSleeping.Value = false; npc.doingEndOfRouteAnimation.Value = false;
        Game1.warpFarmer("Town", 35, 62, false); Game1.warpCharacter(npc, "Town", new Point(35, 61));
        Call(PhoneGet<object>(Phone(), "romance"), "Contact", npc);
        if (!Game1.player.Items.Any(i => i?.QualifiedItemId == "(O)428")) Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)428"));
        Call(Emily(), "ObservePromise"); Call(Emily(), "FindDesignSpot"); EmilyState();
    }
    private void EmilyPromise()
    {
        var service = Emily(); var state = PhoneGet<EmilyLifeState>(service, "State");
        Call(service, "Reply", "Emily", new ConversationReply { Reply = "Some cloth could help.", QuestRequest = "emily-cloth" });
        string Stamp() => $"emily:promise:{state.Promise.Status}:{state.Promise.OfferedDay}:{state.Promise.DueDay}:";
        bool accepted = Call(service, "Apply", "Emily", Stamp() + "accept3") != null;
        int before = Game1.player.Items.Where(i => i?.QualifiedItemId == "(O)428").Sum(i => i.Stack);
        var item = Game1.player.Items.OfType<StardewValley.Object>().Where(i => i.QualifiedItemId == "(O)428").OrderBy(i => i.Quality).First();
        bool delivered = Call(service, "Apply", "Emily", Stamp() + "give:" + item.Quality) != null;
        int after = Game1.player.Items.Where(i => i?.QualifiedItemId == "(O)428").Sum(i => i.Stack);
        Helper.Data.WriteJsonFile("emily-delivery.json", new { Passed = accepted && delivered && after == before - 1 && state.Promise.Status == "completed", Before = before, After = after, state.Promise }); EmilyState();
    }
    private void EmilyOffer() { Call(Emily(), "Reply", "Emily", new ConversationReply { Reply = "Let's explore a design.", QuestRequest = "emily-design" }); EmilyState(); }
    private void EmilyAccept()
    {
        var service = Emily(); var session = PhoneGet<EmilyLifeState>(service, "State").Session;
        if (Call(service, "Apply", "Emily", $"emily:design:{session.Status}:{session.OfferDay}:{session.MeetingDay}:accept") == null) throw new InvalidOperationException("No accepted session."); EmilyState();
    }
    private void EmilyMeet()
    {
        var service = Emily(); var session = PhoneGet<EmilyLifeState>(service, "State").Session;
        if (session.Status != "accepted") throw new InvalidOperationException("Accept first.");
        var date = new WorldDate(Game1.Date) { TotalDays = session.MeetingDay };
        Game1.year = date.Year; Game1.season = date.Season; Game1.dayOfMonth = date.DayOfMonth; Game1.timeOfDay = 1100;
        Game1.exitActiveMenu(); var npc = Game1.getCharacterFromName("Emily"); npc.ignoreScheduleToday = false; npc.followSchedule = true; npc.TryLoadSchedule();
        npc.Halt(); npc.controller = null; npc.temporaryController = null; npc.isSleeping.Value = false; npc.doingEndOfRouteAnimation.Value = false;
        var tile = PhoneGet<Point?>(service, "designSpot")!.Value; Game1.warpFarmer("Town", tile.X, tile.Y + 1, false);
        Call(service, "TickDesign"); EmilyState();
    }
    private void EmilyTalk()
    {
        var npc = Game1.getCharacterFromName("Emily"); Game1.activeClickableMenu = new DialogueBox(new Dialogue(npc, null, "Ready to explore?")); Game1.player.CanMove = false;
        Call(Emily(), "TryBegin", npc); EmilyState(); captureRequested = true; captureDelay = 10;
    }
    private void EmilyAdvance()
    {
        var service = Emily(); int step = PhoneGet<EmilyLifeState>(service, "State").Session.Step;
        Call(service, "AdvanceDesign", step == 0 ? "playful" : step == 1 ? "geometric" : "finish"); EmilyState(); captureRequested = true; captureDelay = 2;
    }
    private void EmilyLive()
    {
        var conversation = PhoneGet<object>(AiMod(Helper), "abigailConversation"); Call(conversation, "Start", Game1.getCharacterFromName("Emily"));
        if (Game1.activeClickableMenu is not NamingMenu input) throw new InvalidOperationException("Accept phone exchange then retry.");
        input.textBox.Text = File.ReadAllText(Path.Combine(Helper.DirectoryPath, "emily-message.txt")).Trim(); input.textBoxEnter(input.textBox);
    }
    private void EmilyFixture()
    {
        EmilyStage(); var tree = PhoneGet<EmilyLifeState>(Emily(), "State").Tree;
        tree.RecordDesign(0, "quiet", "natural"); tree.RecordDesign(3, "playful", "geometric"); tree.Refresh(true, 3);
        Helper.Data.WriteJsonFile("emily-fixture.json", new { Fixture = "Higher milestone setup only; not evidence of natural progression", tree.Unlocked });
    }
    private void EmilyMovementStart() { if (!(bool)Call(Emily(), "StartMovement")!) throw new InvalidOperationException("Movement start rejected."); EmilyState(); captureRequested = true; captureDelay = 12; }
    private void EmilyMovementAdvance() { Call(Emily(), "AdvanceMovement"); EmilyState(); }
}
