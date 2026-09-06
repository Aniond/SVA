using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using SolaceWeather.Core;
using StardewValley;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void RomanceGlobalChecks()
    {
        RequireWorld();
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        var results = new List<object>();
        void Check(string name, bool passed) => results.Add(new { Name = name, Passed = passed });
        object mod = AiMod(Helper);
        var serviceField = mod.GetType().GetFields(fields).Single(f => f.FieldType.FullName == "SolaceWeather.Relationships.RomanceService");
        object service = serviceField.GetValue(mod) ?? throw new InvalidOperationException("Global relationship service is unavailable.");
        var farmField = service.GetType().GetField("farm", fields)!;
        object? originalFarm = farmField.GetValue(service);
        object observer = mod.GetType().GetField("abigail", fields)!.GetValue(mod)!;
        var memoryField = observer.GetType().GetField("memory", fields)!;
        object? originalAbigail = memoryField.GetValue(observer);
        var observerTransient = new[] { "offeredReminder", "selectedExperiences" }
            .Select(name => observer.GetType().GetField(name, fields)).Where(f => f != null)
            .ToDictionary(f => f!, f => f!.GetValue(observer));
        var selections = (Dictionary<string, HashSet<string>>)service.GetType().GetField("selectedMemories", fields)!.GetValue(service)!;
        var originalSelections = selections.ToDictionary(p => p.Key, p => new HashSet<string>(p.Value));
        var friendshipBefore = Game1.player.friendshipData.Pairs.ToDictionary(p => p.Key, p => (p.Value.Points, p.Value.Status));
        int day = Game1.Date.TotalDays;
        var state = new RomanceSaveState();
        var memories = new Dictionary<string, AbigailMemory>();
        foreach (string name in RomanceRules.Candidates)
        {
            RomanceRules.Talk(state, name, day);
            memories[name] = new AbigailMemory { Exchanges = new() { new() { Day = day, Farmer = "PRIVATE_" + name + "_ONLY", Reply = "I remember." } } };
        }
        var farmType = farmField.FieldType;
        object fixture = Activator.CreateInstance(farmType, true)!;
        farmType.GetProperty("FarmerId")!.SetValue(fixture, Game1.player.UniqueMultiplayerID);
        farmType.GetProperty("State")!.SetValue(fixture, state);
        farmType.GetProperty("Memories")!.SetValue(fixture, memories);
        JsonObject ContextFor(string name) => (JsonObject)Call(service, "GetContext", name, "Hi")!;
        try
        {
            farmField.SetValue(service, fixture);
            memoryField.SetValue(observer, memories["Abigail"]);
            foreach (string name in RomanceRules.Candidates)
            {
                var context = ContextFor(name);
                string json = context.ToJsonString();
                Check(name + " receives only their own conversation", json.Contains("PRIVATE_" + name + "_ONLY")
                    && RomanceRules.Candidates.Where(n => n != name).All(n => !json.Contains("PRIVATE_" + n + "_ONLY")));
                if (name != "Abigail")
                    Check(name + " receives no Abigail perks or delivery promises", !context.ContainsKey("RelationshipTree") && !context.ContainsKey("AvailableRequests") && !context.ContainsKey("CanDeliver"));
            }
            RomanceRules.ExpressInterest(state, "Leah", day);
            Check("Private interest does not create gossip", state.Incidents.Count == 0 && state.Knowledge.Count == 0);
            Call(service, "Signal", "Leah", "gift");
            Check("Ordinary gift is not a romantic incident", state.Incidents.Count == 0);
            RomanceRules.RecordIncident(state, "known-romance", "Alex", day, "flirt");
            RomanceRules.LearnIncident(state, "known-romance", "Emily", day, "Emily");
            RomanceRules.SendReport(state, "Emily", "Leah", "known-romance", day);
            var known = ContextFor("Leah")["RomanceJourney"]!["KnownIncidents"]!.AsArray();
            Check("Hearsay keeps named source and original witness", known.Count == 1 && known[0]!["Source"]!.GetValue<string>() == "Emily"
                && known[0]!["OriginalEyewitness"]!.GetValue<string>() == "Emily" && known[0]!["Hops"]!.GetValue<int>() == 1);
            Check("Uninformed NPC does not receive incident", ContextFor("Penny")["RomanceJourney"]!["KnownIncidents"]!.AsArray().Count == 0);
            RomanceRules.Book(state, "Leah", day + 1, 1080, day, 600, "town-walk");
            Check("Upcoming plan is restricted to its participant", ContextFor("Leah")["RomanceJourney"]!["UpcomingPlan"] != null
                && ContextFor("Penny")["RomanceJourney"]!["UpcomingPlan"] == null);

            string encoded = JsonSerializer.Serialize(fixture, farmType);
            object restored = JsonSerializer.Deserialize(encoded, farmType)!;
            Check("Global save round trip remains valid", (bool)farmType.GetMethod("IsValid")!.Invoke(restored, new object[] { Game1.player.UniqueMultiplayerID })!);
            Helper.Data.WriteJsonFile("romance-global-roundtrip.json", fixture);
            var readJson = typeof(StardewModdingAPI.IDataHelper).GetMethods().Single(m => m.Name == "ReadJsonFile" && m.IsGenericMethodDefinition);
            restored = readJson.MakeGenericMethod(farmType).Invoke(Helper.Data, new object[] { "romance-global-roundtrip.json" })!;
            Check("SMAPI JSON restores the farm-owned state", (bool)farmType.GetMethod("IsValid")!.Invoke(restored, new object[] { Game1.player.UniqueMultiplayerID })!);
            farmField.SetValue(service, restored);
            Check("Restored state retains all twelve independent conversations", RomanceRules.Candidates.All(name => ContextFor(name).ToJsonString().Contains("PRIVATE_" + name + "_ONLY")));
            Check("Wrong farmer cannot adopt the save", !(bool)farmType.GetMethod("IsValid")!.Invoke(restored, new object[] { Game1.player.UniqueMultiplayerID + 1 })!);

            farmField.SetValue(service, fixture);
            try { ContextFor("Lewis"); } catch (TargetInvocationException) { }
            Check("Unsupported character context cannot corrupt save", !memories.ContainsKey("Lewis") && (bool)farmType.GetMethod("IsValid")!.Invoke(fixture, new object[] { Game1.player.UniqueMultiplayerID })!);
            Check("Read-only checks do not change native friendship", friendshipBefore.All(p => Game1.player.friendshipData.TryGetValue(p.Key, out var current) && current.Points == p.Value.Points && current.Status == p.Value.Status));
        }
        catch (Exception ex)
        {
            results.Add(new { Name = "Global checks completed", Passed = false, Error = ex.ToString() });
        }
        finally
        {
            farmField.SetValue(service, originalFarm);
            memoryField.SetValue(observer, originalAbigail);
            foreach (var pair in observerTransient) pair.Key.SetValue(observer, pair.Value);
            selections.Clear();
            foreach (var pair in originalSelections) selections[pair.Key] = pair.Value;
        }
        Helper.Data.WriteJsonFile("romance-global-results.json", results);
    }
}
