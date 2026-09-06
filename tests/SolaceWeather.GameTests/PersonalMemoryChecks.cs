using System.Reflection;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Locations;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void CheckPersonalMemory()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object mod = AiMod(Helper);
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        var field = observer.GetType().GetField("memory", flags)!;
        var original = field.GetValue(observer);
        GameLocation location = Game1.player.currentLocation;
        int points = Game1.player.friendshipData.TryGetValue("Abigail", out var friendship) ? friendship.Points : 0;
        var results = new List<object>();
        void Check(string name, bool pass) => results.Add(new { Name = name, Passed = pass });
        try
        {
            var ledger = new AbigailMemory();
            field.SetValue(observer, ledger);
            string message = "I love fishing. I'm going to the mines tomorrow.";
            Call(observer, "RememberReply", message, new ConversationReply { Reply = "What are you hoping to find?", Memories = new() {
                new MemoryProposal { Topic = "fishing", Kind = "preference", Quote = "I love fishing." },
                new MemoryProposal { Topic = "mines", Kind = "plan", Quote = "I'm going to the mines tomorrow.", Timing = "tomorrow" }
            } });
            Check("Completed reply stores sourced personal details", ledger.Personal.Details.Count == 2 && ledger.Exchanges.Count == 1);
            Check("Plan does not invent a mine visit", ledger.Personal.MineVisitStatus(Game1.Date.TotalDays) != "visited");
            Game1.player.currentLocation = new MineShaft(1);
            Call(observer, "RecordActivity");
            Check("Native mine location records a visit", ledger.Personal.MineVisitStatus(Game1.Date.TotalDays) == "visited");
            Game1.player.currentLocation = location;
            object context = Call(observer, "GetContext")!;
            Check("Context includes persistent details and activity evidence", Get(context, "PersistentDetails") != null && Get(context, "ActivityEvidence") != null);
            Helper.Data.WriteJsonFile("personal-memory-roundtrip.json", ledger);
            var restored = Helper.Data.ReadJsonFile<AbigailMemory>("personal-memory-roundtrip.json")!;
            Check("SMAPI JSON restores detail and game record", restored.IsValid() && restored.Personal.Details.Count == 2
                && restored.Personal.MineVisitStatus(Game1.Date.TotalDays) == "visited");
            Check("Friendship remains unchanged", (Game1.player.friendshipData.TryGetValue("Abigail", out var current) ? current.Points : 0) == points);
        }
        finally
        {
            field.SetValue(observer, original);
            Game1.player.currentLocation = location;
        }
        Helper.Data.WriteJsonFile("personal-memory-results.json", results);
    }
}
