using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SolaceWeather.Core;
using StardewValley;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private Task? romanceAiRun;
    private sealed record RomanceAiSnapshot(string Name, string Stage, string Message, string Context);

    // Capture and restore every game object before starting network work. Background workers receive only strings.
    private void StartRomanceAiChecks()
    {
        RequireWorld();
        if (romanceAiRun is { IsCompleted: false }) throw new InvalidOperationException("Romance AI sampling is already running.");
        string key = Environment.GetEnvironmentVariable("GEMINI_API_KEY", EnvironmentVariableTarget.User)
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Gemini key is unavailable.");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object mod = AiMod(Helper);
        object service = mod.GetType().GetField("romance", flags)!.GetValue(mod)!;
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        object config = mod.GetType().GetField("config", flags)!.GetValue(mod)!;
        string model = (string)config.GetType().GetProperty("GeminiModel")!.GetValue(config)!;
        var farmField = service.GetType().GetField("farm", flags)!;
        object? originalFarm = farmField.GetValue(service);
        var memoryField = observer.GetType().GetField("memory", flags)!;
        object? originalMemory = memoryField.GetValue(observer);
        var transient = new[] { "offeredReminder", "selectedExperiences" }.Select(n => observer.GetType().GetField(n, flags))
            .Where(f => f != null).ToDictionary(f => f!, f => f!.GetValue(observer));
        var selections = (Dictionary<string, HashSet<string>>)service.GetType().GetField("selectedMemories", flags)!.GetValue(service)!;
        var originalSelections = selections.ToDictionary(p => p.Key, p => new HashSet<string>(p.Value));
        var snapshots = new List<RomanceAiSnapshot>();
        int day = Game1.Date.TotalDays;
        try
        {
            foreach (string name in RomanceRules.Candidates)
            foreach (string stage in new[] { "friendship", "friendship-only", "dating", "conflict", "separated", "married" })
            {
                var character = new RomanceCharacterState
                {
                    FirstContactDay = Math.Max(0, day - 84), TalkDays = 24, CompletedActivities = 10,
                    ActivityTypes = new() { "town-walk", "lake-time" },
                    FriendshipOnly = stage == "friendship-only", InterestExpressed = stage is not ("friendship" or "friendship-only"),
                    IsDating = stage is "dating" or "conflict" or "separated" or "married", IsMarried = stage == "married",
                    DatingSinceDay = stage is "friendship" or "friendship-only" ? null : Math.Max(0, day - (stage == "dating" ? 7 : 56)),
                    RomanticDates = stage is "friendship" or "friendship-only" ? 0 : 4,
                    ConflictFreeSinceDay = Math.Max(0, day - 14)
                };
                var state = new RomanceSaveState { LatestDay = day, Characters = new() { [name] = character } };
                if (stage is "conflict" or "separated")
                {
                    string other = name == "Alex" ? "Leah" : "Alex";
                    RomanceRules.RecordIncident(state, "sample:flirt", other, day, "flirt");
                    // A candidate's own knowledge explicitly remains hearsay from a named third party.
                    RomanceRules.LearnIncident(state, "sample:flirt", name, day, "Robin", 1, "Robin");
                    character.WarningDeliveredDay = day;
                    character.WarningIncidentSequence = 1;
                    if (stage == "separated") character.SeparationUntilDay = day + 14;
                }
                var memory = new AbigailMemory();
                memory.Experiences.Record("sample:walk", "shared-time", day,
                    "We completed a quiet town walk together and stopped to look at light reflecting in the river. No gifts changed hands.", "walk river shared time");
                object fixture = Activator.CreateInstance(farmField.FieldType, true)!;
                farmField.FieldType.GetProperty("FarmerId")!.SetValue(fixture, Game1.player.UniqueMultiplayerID);
                farmField.FieldType.GetProperty("State")!.SetValue(fixture, state);
                farmField.FieldType.GetProperty("Memories")!.SetValue(fixture, new Dictionary<string, AbigailMemory> { [name] = memory });
                farmField.SetValue(service, fixture);
                memoryField.SetValue(observer, memory);
                string message = stage switch
                {
                    "friendship" => "What would you enjoy doing on a free afternoon? I liked our walk by the river.",
                    "friendship-only" => "I want us to stay friends. What did you enjoy about our walk by the river?",
                    "dating" => "I liked our walk by the river. What does spending time together mean to you?",
                    "conflict" => "What did you hear, and from whom? I'm sorry. Can an apology fix this, or what should I actually do?",
                    "separated" => "I'm sorry about the boundary violation. Can we skip the time apart and count our relationship as fully repaired right now?",
                    _ => "What small thing would you enjoy sharing at home this week? I still think about our walk by the river."
                };
                var context = (JsonObject)Call(service, "GetContext", name, message)!;
                // Synthetic fixture context only: no persistence, commitment, game clock or native friendship is changed.
                context["SampleFixture"] = "This context is a test fixture of recorded relationship evidence; apply the same character and evidence rules.";
                snapshots.Add(new(name, stage, message, context.ToJsonString()));
            }
        }
        finally
        {
            farmField.SetValue(service, originalFarm);
            memoryField.SetValue(observer, originalMemory);
            foreach (var pair in transient) pair.Key.SetValue(observer, pair.Value);
            selections.Clear();
            foreach (var pair in originalSelections) selections[pair.Key] = pair.Value;
        }
        string output = Path.Combine(Helper.DirectoryPath, "romance-ai-results.json");
        File.WriteAllText(output, JsonSerializer.Serialize(new { Running = true, Expected = snapshots.Count, Completed = 0, Samples = Array.Empty<object>() }));
        romanceAiRun = Task.Run(() => RunRomanceAiSnapshots(snapshots.ToArray(), model, key, output));
    }

    private static async Task RunRomanceAiSnapshots(RomanceAiSnapshot[] snapshots, string model, string key, string output)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        using var concurrency = new SemaphoreSlim(3);
        using var writeGate = new SemaphoreSlim(1);
        var results = new List<object>();
        int errors = 0;
        async Task Save(bool running)
        {
            string json = JsonSerializer.Serialize(new { Running = running, Model = model, Expected = snapshots.Length, Completed = results.Count,
                Errors = errors, NeedsManualReview = true, Samples = results }, new JsonSerializerOptions { WriteIndented = true });
            string temporary = output + ".tmp";
            await File.WriteAllTextAsync(temporary, json).ConfigureAwait(false);
            File.Move(temporary, output, true);
        }
        await Task.WhenAll(snapshots.Select(async snapshot =>
        {
            await concurrency.WaitAsync().ConfigureAwait(false);
            object result;
            bool error = false;
            try
            {
                var reply = await new GeminiConversation(http).ReplyForCharacter(key, model, snapshot.Context, snapshot.Message, snapshot.Name, CancellationToken.None).ConfigureAwait(false);
                bool groundedRecall = string.IsNullOrEmpty(reply.RecalledExperienceId) || reply.RecalledExperienceId == "sample:walk";
                bool sourcedQuotes = reply.Memories.All(m => !string.IsNullOrWhiteSpace(m.Quote) && snapshot.Message.Contains(m.Quote, StringComparison.Ordinal));
                bool noGrantClaim = !Regex.IsMatch(reply.Reply, @"\b(?:I(?:'ve| have) (?:added|awarded|granted)|you(?:'ve| have) (?:gained|earned) \d+ (?:hearts?|points?))", RegexOptions.IgnoreCase);
                result = new { snapshot.Name, snapshot.Stage, snapshot.Message, Context = JsonNode.Parse(snapshot.Context), Reply = reply,
                    Checks = new { SpokenLengthValid = reply.Reply.Length is > 0 and <= 1200,
                        ExpressionKnown = AbigailExpression.Names.Contains(reply.Expression), NoOtherCharacterQuest = snapshot.Name == "Abigail" || reply.QuestRequest == "",
                        RecallReferencesSuppliedMemory = groundedRecall, MemoryQuotesComeFromMessage = sourcedQuotes, NoExplicitAwardClaim = noGrantClaim },
                    ManualReview = "Check recognizable voice, gradual couple tone, friendship-only boundaries, hearsay attribution, proportionate conflict, no instant repair or invented completed plans." };
            }
            catch (Exception ex)
            {
                error = true;
                // Never retain provider bodies, headers, keys, or arbitrary exception messages.
                result = new { snapshot.Name, snapshot.Stage, snapshot.Message, Failed = true, ErrorType = ex.GetType().Name };
            }
            finally { concurrency.Release(); }
            await writeGate.WaitAsync().ConfigureAwait(false);
            try { if (error) errors++; results.Add(result); await Save(true).ConfigureAwait(false); }
            finally { writeGate.Release(); }
        })).ConfigureAwait(false);
        await writeGate.WaitAsync().ConfigureAwait(false);
        try { await Save(false).ConfigureAwait(false); }
        finally { writeGate.Release(); }
    }
}
