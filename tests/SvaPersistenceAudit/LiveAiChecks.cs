using System.Text.Json;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SvaPersistenceAudit;

public sealed partial class ModEntry
{
    private Task? liveTask;
    private string liveCase = "", liveMessage = "", liveContext = "";
    private DateTime liveStarted;
    private int liveCount;
    private bool liveWaiting;

    private void StartLiveAi(string kind)
    {
        RequireOwnedWorld();
        if (!StardewModdingAPI.Context.IsWorldReady || liveWaiting) throw new InvalidOperationException("Live check requires settled owned farm and no pending call.");
        var services = Services();
        if (Member(services.Abigail, "Ready") is not true || Member(services.Romance, "Ready") is not true) throw new InvalidOperationException("Living Memory unavailable.");
        liveCase = kind;
        liveMessage = kind == "remember" ? "My favorite quiet-day snack is blackberry jam on toast. Please remember that about me." : "What is my favorite quiet-day snack? If I have not told you, please say you do not know.";
        var memory = (AbigailMemory)Member(services.Abigail, "memory")!;
        liveCount = memory.Exchanges.Count;
        object conversation = Member(Mod("David.SolaceWeather"), "abigailConversation")!;
        Game1.exitActiveMenu();
        conversation.GetType().GetMethod("Start", Members)!.Invoke(conversation, new object[] { Game1.getCharacterFromName("Abigail") });
        if (Game1.activeClickableMenu is not NamingMenu input) throw new InvalidOperationException("Real NPC conversation input did not open.");
        input.textBox.Text = liveMessage;
        input.textBoxEnter(input.textBox);
        liveTask = Member(conversation, "pending") as Task;
        liveContext = Member(conversation, "context") as string ?? "";
        liveStarted = DateTime.UtcNow; liveWaiting = true;
        Helper.Data.WriteJsonFile("live-ai-" + kind + ".json", new { Running = true, ProviderRequestStarted = liveTask != null, Message = liveMessage, StartedAt = liveStarted });
    }

    private void WatchLiveAi()
    {
        if (!liveWaiting) return;
        if (liveTask != null && !liveTask.IsCompleted && DateTime.UtcNow - liveStarted < TimeSpan.FromSeconds(45)) return;
        var memory = (AbigailMemory?)Member(Services().Abigail, "memory");
        var exchange = memory?.Exchanges.LastOrDefault();
        // Production processes the completed task in its own update handler; wait for the recorded reply.
        if (liveTask?.IsCompletedSuccessfully == true && exchange?.Farmer != liveMessage && DateTime.UtcNow-liveStarted < TimeSpan.FromSeconds(45)) return;
        bool provider = liveTask?.IsCompletedSuccessfully == true;
        bool recorded = exchange?.Farmer == liveMessage && memory!.Exchanges.Count > liveCount;
        string reply = recorded ? exchange!.Reply : "";
        var result = new { Running = false, Case = liveCase, StartedAt = liveStarted, FinishedAt = DateTime.UtcNow,
            ProviderSucceeded = provider, Recorded = recorded, ReplyDisplayed = Game1.activeClickableMenu is DialogueBox,
            Message = liveMessage, Reply = reply, ContextContainedSnack = liveContext.Contains("blackberry jam", StringComparison.OrdinalIgnoreCase),
            ReplyMentionsSnack = reply.Contains("blackberry", StringComparison.OrdinalIgnoreCase) && reply.Contains("toast", StringComparison.OrdinalIgnoreCase),
            RememberedPersonalDetails = memory?.Personal.Details, FarmerId = Game1.player.UniqueMultiplayerID, Seed,
            ErrorCategory = liveTask?.IsFaulted == true ? "Provider task faulted; credential-bearing details withheld" : null };
        liveWaiting = false;
        Helper.Data.WriteJsonFile("live-ai-" + liveCase + ".json", result);
        WriteProfile("live-ai-" + liveCase + ".json", result);
        capturePending = true;
    }
}
