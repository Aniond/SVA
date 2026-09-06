using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

internal sealed partial class NpcConversation
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25), MaxResponseContentBufferSize = 65536 };
    private readonly AbigailRelationship relationship;
    private readonly RomanceService romance;
    private static string Speaker = "Abigail";
    private QuestChoice[] QuestChoices() => Speaker == "Abigail" ? relationship.Quests?.Choices() ?? Array.Empty<QuestChoice>() : Array.Empty<QuestChoice>();
    private string[] ItemTerms => Speaker == "Abigail" ? relationship.Quests?.ItemTerms ?? Array.Empty<string>() : Array.Empty<string>();
    private QuestChoice[] ServiceChoices() => romance.Choices(Speaker).Concat(Speaker == "Abigail" ? relationship.TreeService?.Choices() ?? Array.Empty<QuestChoice>() : Array.Empty<QuestChoice>()).ToArray();
    internal void Start(NPC npc)
    {
        if (!romance.Ready || !RomanceService.Supported(npc) || Game1.eventUp) return;
        Reset(); Speaker = npc.Name; romance.Contact(npc);
        farmerId = Game1.player.UniqueMultiplayerID; day = Game1.Date.TotalDays;
        OpenInput();
    }
    private readonly ModConfig config;
    private readonly IMonitor monitor;
    private IClickableMenu? owned;
    private DialogueBox? original;
    private Task<ConversationReply>? pending;
    private CancellationTokenSource? cancellation;
    private string question = "";
    private string? currentAction;
    private string? actionFallback;
    private string? context;
    private long farmerId;
    private int day;
    private DialogueBox? lastNative;

    internal void AfterInteraction(NPC npc)
    {
        if (!config.EnableAbigailAi || !romance.Ready || Game1.eventUp || Game1.activeClickableMenu != null
            || !Game1.player.hasPlayerTalkedToNPC(npc.Name)) return;
        Start(npc);
    }

    internal static bool CanStart(DialogueBox box)
    {
        var dialogue = box.characterDialogue;
        return dialogue != null && RomanceService.Supported(dialogue.speaker) && !box.isQuestion
            && dialogue.onFinish == null && dialogue.answerQuestionBehavior == null
            && !(dialogue.getNPCResponseOptions()?.Count > 0)
            && !dialogue.dialogues.Any(line => line.SideEffects != null);
    }

    public NpcConversation(IModHelper helper, IMonitor monitor, ModConfig config, AbigailRelationship relationship, RomanceService romance)
    {
        this.relationship = relationship;
        this.romance = romance;
        this.config = config;
        this.monitor = monitor;
        helper.Events.Input.ButtonPressed += (_, e) =>
        {
            if (e.Button == SButton.Escape && owned != null && ReferenceEquals(Game1.activeClickableMenu, owned))
            {
                helper.Input.Suppress(e.Button);
                Game1.exitActiveMenu();
                Reset();
                return;
            }
            if (e.Button != config.AbigailTalkKey || !config.EnableAbigailAi || !romance.Ready || Game1.eventUp) return;
            if (pending != null || Game1.activeClickableMenu is ConversationInput) return;
            if (Game1.activeClickableMenu is DialogueBox box && CanStart(box))
            {
                original = box;
                Speaker = box.characterDialogue.speaker.Name; romance.Contact(box.characterDialogue.speaker);
                farmerId = Game1.player.UniqueMultiplayerID;
                day = Game1.Date.TotalDays;
            }
            else if (owned == null || !ReferenceEquals(Game1.activeClickableMenu, owned)) return;
            helper.Input.Suppress(e.Button);
            OpenInput();
        };
        helper.Events.GameLoop.UpdateTicked += (_, _) => Tick();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { Reset(); lastNative = null; };
        helper.Events.GameLoop.SaveLoaded += (_, _) => { Reset(); lastNative = null; };
    }

    private static string? ApiKey() => Environment.GetEnvironmentVariable("GEMINI_API_KEY", EnvironmentVariableTarget.User)
        ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
        ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

    private void OpenInput()
    {
        RefreshContext();
        if (original != null && ReferenceEquals(Game1.activeClickableMenu, original)) original.closeDialogue();
        owned = new ConversationInput(Submit, () =>
        {
            Game1.exitActiveMenu();
            Reset();
        }, QuestChoices, () => ItemTerms, ActOnQuest);
        // Native NPC dialogue freezes the farmer; our menu pauses game time itself.
        // Release that native flag now so closing a non-native menu cannot strand them.
        Game1.player.CanMove = true;
        Game1.activeClickableMenu = owned;
    }

    private void Submit(string text)
    {
        if (pending != null || !romance.Ready || string.IsNullOrWhiteSpace(text)) return;
        question = text.Trim();
        if (question.Length > 500) return;
        RefreshContext(currentAction);
        currentAction = null;
        Game1.keyboardDispatcher.Subscriber = null;
        owned = new DeliveryReplyBox(Speaker + " is thinking... (Esc to cancel)", () => Array.Empty<QuestChoice>(), Array.Empty<string>(), _ => { }, "thoughtful");
        Game1.activeClickableMenu = owned;
        if (!config.EnableAbigailAi || string.IsNullOrWhiteSpace(ApiKey())) { ShowFallback(); return; }
        cancellation = new CancellationTokenSource();
        // Only the network operation runs off-thread. No game objects cross this boundary.
        pending = new GeminiConversation(Http).ReplyForCharacter(ApiKey() ?? "", config.GeminiModel, context ?? "{}", question, Speaker, cancellation.Token);
    }

    private void RefreshContext(string? action = null) => context = JsonSerializer.Serialize(new {
        Relationship = romance.GetContext(Speaker, action == null ? question : question + " " + action), NativeDialogueCue = original?.getCurrentString(),
        CurrentAction = action,
        CueMeaning = "The native line is a game-selected tone/topic cue, not evidence that it was already spoken." });

    private void ActOnQuest(string choiceKey)
    {
        if (pending != null || owned == null || !ReferenceEquals(Game1.activeClickableMenu, owned)
            || !romance.Ready || Game1.player.UniqueMultiplayerID != farmerId || Game1.Date.TotalDays != day) return;
        if (choiceKey == "ui:services")
        {
            Game1.keyboardDispatcher.Subscriber = null;
            owned = new ServiceChoicesMenu(ServiceChoices,
                () => "Choose an explicit relationship action or shared activity.", ActOnQuest, OpenInput);
            Game1.activeClickableMenu = owned;
            return;
        }
        if (choiceKey is "romance:activity" or "romance:romantic-date" or "romance:repair-date")
        {
            if (romance.OpenActivity(Speaker, choiceKey)) Reset();
            return;
        }
        var applied = choiceKey.StartsWith("romance:") ? romance.Apply(Speaker, choiceKey) : Speaker != "Abigail" ? null : choiceKey.StartsWith("tree:") ? relationship.TreeService?.ApplyChoice(choiceKey) : relationship.Quests?.ApplyChoice(choiceKey);
        if (applied is not { } result)
        {
            Game1.addHUDMessage(new HUDMessage("That choice changed. Stand beside " + Speaker + " and check the current choices.", HUDMessage.error_type));
            return;
        }
        // Explicit actions remain recorded even if the optional network acknowledgement fails.
        original = new DeliveryReplyBox(Speaker + ": " + result.Fallback,
            () => Array.Empty<QuestChoice>(), ItemTerms, _ => { });
        actionFallback = result.Fallback;
        currentAction = result.Fact;
        Submit(result.FarmerLine);
    }

    private void Tick()
    {
        if (owned == null && config.EnableAbigailAi && romance.Ready && !Game1.eventUp
            && Game1.activeClickableMenu is DialogueBox native && !ReferenceEquals(native, lastNative) && CanStart(native))
        {
            lastNative = native;
            original = native;
            Speaker = native.characterDialogue.speaker.Name; romance.Contact(native.characterDialogue.speaker);
            farmerId = Game1.player.UniqueMultiplayerID;
            day = Game1.Date.TotalDays;
            OpenInput();
        }
        if (owned == null) return;
        if (!romance.Ready || Game1.player.UniqueMultiplayerID != farmerId || Game1.Date.TotalDays != day
            || Game1.eventUp || !ReferenceEquals(Game1.activeClickableMenu, owned))
        { Reset(); return; }
        if (pending == null || !pending.IsCompleted) return;
        try
        {
            var reply = pending.GetAwaiter().GetResult();
            romance.RememberReply(Speaker, question, reply);
            owned = new DeliveryReplyBox(Speaker + ": " + reply.Reply + $"#{config.AbigailTalkKey}: reply   |   Esc: finish talking",
                QuestChoices, ItemTerms, ActOnQuest, reply.Expression);
            Game1.activeClickableMenu = owned;
        }
        catch (Exception)
        {
            // Never log request bodies, credentials or raw provider error responses.
            monitor.Log(Speaker + "'s AI reply was unavailable; using authored dialogue.", LogLevel.Warn);
            ShowFallback();
        }
        actionFallback = null;
        pending = null;
        cancellation?.Dispose();
        cancellation = null;
    }

    private void ShowFallback()
    {
        string text = actionFallback ?? romance.Fallback(Speaker);
        romance.RememberReply(Speaker, question, new ConversationReply { Reply = text });
        owned = new DeliveryReplyBox(Speaker + ": " + text + $"#{config.AbigailTalkKey}: reply   |   Esc: finish talking",
            QuestChoices, ItemTerms, ActOnQuest,
            Speaker == "Abigail" && relationship.TreeService?.State.HelpStatus(relationship.Memory!.Promises, Game1.Date.TotalDays).Paused == true ? "thoughtful" : "neutral");
        Game1.activeClickableMenu = owned;
        actionFallback = null;
    }

    private void Reset()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
        // Observe eventual failures of a cancelled request without applying stale results.
        if (pending != null) _ = pending.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        pending = null;
        if (owned is ConversationInput input && ReferenceEquals(Game1.keyboardDispatcher.Subscriber, input.textBox))
            Game1.keyboardDispatcher.Subscriber = null;
        owned = null;
        original = null;
        context = null;
        currentAction = null;
        actionFallback = null;
    }

}


