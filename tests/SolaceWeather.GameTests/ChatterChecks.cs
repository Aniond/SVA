using System.Text.Json;
using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewValley;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private bool watchingChatter;
    private int watchedLine = -1;
    private long copyChatterAt;
    private string chatterImage = "";
    private TownChatterState? chatterReplayOriginal;
    private object Chatter()
    {
        Phone(); // Enforces the disposable SvaAudit phone profile guard.
        return PhoneGet<object>(AiMod(Helper), "chatter");
    }
    private void ChatterStage()
    {
        StageBackgroundProgress();
        var chatter = Chatter(); Call(chatter, "Stop");
        PhoneSet(chatter, "nextAttempt", double.MaxValue);
        Game1.exitActiveMenu(); Game1.warpFarmer("Town", 35, 61, false);
        foreach (var pair in new[] { ("Abigail", 36), ("Sam", 38) })
        {
            var npc = Game1.getCharacterFromName(pair.Item1); npc.Halt(); npc.controller = null; npc.temporaryController = null;
            npc.followSchedule = false; npc.ignoreScheduleToday = true;
            Game1.warpCharacter(npc, "Town", new Point(pair.Item2, 61));
            npc.faceDirection(pair.Item1 == "Abigail" ? 1 : 3);
        }
        Game1.timeOfDay = 1000; captureRequested = true; captureDelay = 15;
    }
    private void ChatterState()
    {
        var chatter = Chatter();
        Helper.Data.WriteJsonFile("chatter-state.json", new { Ready = PhoneGet<bool>(chatter, "Ready"), Busy = PhoneGet<bool>(chatter, "Busy"),
            State = PhoneGet<TownChatterState>(chatter, "State"),
            Context = JsonSerializer.Serialize(Call(chatter, "BuildContext", "Abigail", "Sam")),
            LivingMemory = JsonSerializer.Serialize(Call(PhoneGet<object>(Phone(), "romance"), "GetPhoneContext", "Abigail", "What did you and Sam talk about in town?")) });
    }
    private void ChatterChecks()
    {
        var chatter = Chatter(); var original = PhoneGet<TownChatterState>(chatter, "State");
        var fixtureConfig = PhoneGet<ModConfig>(chatter, "config"); bool originalAiEnabled = fixtureConfig.EnableAbigailAi;
        var checks = new Dictionary<string, bool>();
        var a = Game1.getCharacterFromName("Abigail"); var b = Game1.getCharacterFromName("Sam");
        var position = Game1.player.Position;
        var state = new TownChatterState { FarmerId = Game1.player.UniqueMultiplayerID };
        ChatterContext context = (ChatterContext)Call(chatter, "BuildContext", "Abigail", "Sam")!;
        var reply = new ChatterReply("weather", new[] { new ChatterLine("Abigail", "This weather makes me want to explore."), new ChatterLine("Sam", "I'll stick to a walk around town.") });
        void Queue()
        {
            PhoneSet(chatter, "first", a); PhoneSet(chatter, "second", b); PhoneSet(chatter, "snapshot", context);
            PhoneSet(chatter, "pending", Task.FromResult(reply));
        }
        try
        {
            fixtureConfig.EnableAbigailAi = true; Call(chatter, "Stop"); PhoneSet(chatter, "state", state); PhoneSet(chatter, "nextAttempt", double.MaxValue);
            checks["nearby visible pair eligible"] = Call(chatter, "Eligible", a) is true && Call(chatter, "Eligible", b) is true;
            string publicJson = JsonSerializer.Serialize(context);
            checks["public snapshot excludes private memory and farmer claims"] = !publicJson.Contains("PersistentDetails") && !publicJson.Contains("RomanceJourney") && !publicJson.Contains("SvaAudit");
            checks["current weather is grounded"] = context.Facts.Any(f => f.Id == "weather");
            var oldSeason = Game1.season; int oldDay = Game1.dayOfMonth;
            try
            {
                Game1.season = Season.Spring; Game1.dayOfMonth = 6;
                var upcoming = (ChatterContext)Call(chatter, "BuildContext", "Abigail", "Sam")!;
                checks["native Egg Festival appears exactly seven days ahead"] = upcoming.Facts.Any(f => f.Id == "festival:spring13" && f.Fact.Contains("in 7 days"));
                Game1.dayOfMonth = 5;
                var outside = (ChatterContext)Call(chatter, "BuildContext", "Abigail", "Sam")!;
                checks["festival beyond seven days is absent"] = !outside.Facts.Any(f => f.Id == "festival:spring13");
            }
            finally { Game1.season = oldSeason; Game1.dayOfMonth = oldDay; }
            Helper.Data.WriteJsonFile("chatter-availability.json", new { Ready = PhoneGet<bool>(chatter, "Ready"), Available = Call(chatter, "Available"), Game1.player.CanMove, Game1.eventUp, Menu = Game1.activeClickableMenu?.GetType().Name, Free = StardewModdingAPI.Context.IsPlayerFree, TimePass = Game1.shouldTimePass(), ConversationBusy = PhoneGet<Func<bool>>(chatter, "conversationBusy")(), Pair = Call(chatter, "PairAvailable") }); Queue(); Call(chatter, "Tick");
            checks["first bubble belongs to Abigail without menu"] = PhoneGet<NPC>(chatter, "speaking") == a && Game1.activeClickableMenu == null;
            checks["unfinished exchange not remembered"] = state.Heard.Count == 0;
            PhoneSet(chatter, "elapsed", PhoneGet<double>(chatter, "lineEnds") + .01); Call(chatter, "Tick");
            checks["second bubble belongs to Sam"] = PhoneGet<NPC>(chatter, "speaking") == b;
            PhoneSet(chatter, "elapsed", PhoneGet<double>(chatter, "lineEnds") + .01); Call(chatter, "Tick");
            checks["completed witnessed exchange remembered once"] = state.Heard.Count == 1 && !PhoneGet<bool>(chatter, "Busy");
            var romance = PhoneGet<object>(Phone(), "romance");
            checks["public attribution reaches Abigail living context"] = JsonSerializer.Serialize(Call(romance, "GetPhoneContext", "Abigail", "town")).Contains("This weather makes me want to explore.");
            Helper.Data.WriteJsonFile("chatter-availability.json", new { Ready = PhoneGet<bool>(chatter, "Ready"), Available = Call(chatter, "Available"), Game1.player.CanMove, Game1.eventUp, Menu = Game1.activeClickableMenu?.GetType().Name, Free = StardewModdingAPI.Context.IsPlayerFree, TimePass = Game1.shouldTimePass(), ConversationBusy = PhoneGet<Func<bool>>(chatter, "conversationBusy")(), Pair = Call(chatter, "PairAvailable") }); Queue(); Call(chatter, "Tick"); Game1.player.Position += new Vector2(640, 0); Call(chatter, "Tick");
            checks["walking away cancels without adding memory"] = !PhoneGet<bool>(chatter, "Busy") && state.Heard.Count == 1;
            Game1.player.Position = position;
            Queue(); PhoneSet(chatter, "snapshot", context with { Day = context.Day - 1 }); Call(chatter, "Tick");
            checks["stale day discards queued reply"] = !PhoneGet<bool>(chatter, "Busy") && state.Heard.Count == 1;
            Queue(); PhoneSet(chatter, "snapshot", context with { Facts = new[] { new ChatterFact("weather", "Different weather at request time.") } }); Call(chatter, "Tick");
            checks["changed weather discards queued reply"] = !PhoneGet<bool>(chatter, "Busy") && state.Heard.Count == 1;
            checks["witnessed state validates"] = state.IsValid(Game1.player.UniqueMultiplayerID);
        }
        finally { fixtureConfig.EnableAbigailAi = originalAiEnabled; Call(chatter, "Stop"); PhoneSet(chatter, "state", original); PhoneSet(chatter, "nextAttempt", double.MaxValue); Game1.player.Position = position; }
        Helper.Data.WriteJsonFile("chatter-checks.json", new { Passed = checks.Values.All(v => v), Checks = checks });
    }
    private void ChatterLive()
    {
        var chatter = Chatter();
        if (Call(chatter, "Start", Game1.getCharacterFromName("Abigail"), Game1.getCharacterFromName("Sam")) is not true)
            throw new InvalidOperationException("Chatter pair could not start.");
        watchingChatter = true; watchedLine = -1;
    }
    private void ChatterReplay()
    {
        var chatter = Chatter(); Call(chatter, "Stop");
        var restored = PhoneGet<TownChatterState>(chatter, "State");
        var reply = Helper.Data.ReadJsonFile<ChatterReply>("chatter-live-reply.json") ?? throw new InvalidDataException("No recorded public dialogue to replay.");
        if (reply.Lines.Length != 2 || !reply.Lines.All(TownChatterState.ValidLine)) throw new InvalidDataException("Invalid visual fixture.");
        chatterReplayOriginal = restored;
        PhoneSet(chatter, "state", JsonSerializer.Deserialize<TownChatterState>(JsonSerializer.Serialize(restored)));
        State.Enabled = true; Call(Runtime, "Force", Region.Town, WeatherKind.Rain); Call(Runtime, "Refresh");
        PhoneSet(chatter, "first", Game1.getCharacterFromName("Abigail")); PhoneSet(chatter, "second", Game1.getCharacterFromName("Sam"));
        PhoneSet(chatter, "snapshot", Call(chatter, "BuildContext", "Abigail", "Sam"));
        PhoneSet(chatter, "pending", Task.FromResult(reply));
        watchingChatter = true; watchedLine = -1;
    }
    private void WatchChatter()
    {
        if (copyChatterAt > 0 && drawCount >= copyChatterAt)
        {
            File.Copy(Path.Combine(Helper.DirectoryPath, "capture.png"), Path.Combine(Helper.DirectoryPath, chatterImage), true); copyChatterAt = 0;
        }
        if (!watchingChatter) return;
        var chatter = Chatter();
        var field = chatter.GetType().GetField("exchange", PhoneFlags)!;
        if (field.GetValue(chatter) is ChatterReply reply)
        {
            int current = PhoneGet<int>(chatter, "lineIndex");
            if (current != watchedLine)
            {
                watchedLine = current; Helper.Data.WriteJsonFile("chatter-live-reply.json", reply);
                captureRequested = true; captureDelay = 8; copyChatterAt = drawCount + 11; chatterImage = "chatter-line-" + current + ".png";
            }
        }
        if (!PhoneGet<bool>(chatter, "Busy"))
        {
            watchingChatter = false;
            if (chatterReplayOriginal != null) { PhoneSet(chatter, "state", chatterReplayOriginal); chatterReplayOriginal = null; }
            ChatterState();
        }
    }
}
