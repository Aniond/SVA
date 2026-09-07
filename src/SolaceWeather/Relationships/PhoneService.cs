using System.Text.Json;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;

namespace SolaceWeather.Relationships;

internal sealed class PhoneService
{
    private const string SaveKey = "phone-texts";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25), MaxResponseContentBufferSize = 65536 };
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly RomanceService romance;
    private PhoneState? state;
    private Task<ConversationReply>? pending;
    private CancellationTokenSource? cancel;
    private string pendingName = "", pendingId = "", pendingText = "";
    private string pendingContext = "{}";
    private int requestDay, requestTime;
    private PhoneMenu? menu;
    internal PhoneState? State => state;
    internal bool Ready => config.EnablePhone && romance.Ready && state != null;
    internal bool Busy => pending != null;
    internal string Notice { get; private set; } = "";
    internal string[] Contacts => RomanceRules.Candidates.Where(name => state?.HasNumber(name) == true)
        .OrderByDescending(name => state?.Threads.GetValueOrDefault(name)?.Unread == true).ThenBy(name => name).ToArray();
    internal bool CanText(string name) => Ready && state!.CanText(name);
    internal string BlockReason(string name) => state?.Contacts.GetValueOrDefault(name)?.BlockReason ?? "";
    internal object ContactContext(string name) => new
    {
        NumberExchanged = state?.HasNumber(name) == true,
        TextingBlocked = BlockReason(name) != "",
        BlockReason = BlockReason(name),
        Rule = "Only the explicit in-person number exchange unlocks phone contact. Dialogue cannot exchange numbers or change a block."
    };

    internal PhoneService(IModHelper helper, IMonitor monitor, ModConfig config, RomanceService romance)
    {
        this.helper = helper; this.monitor = monitor; this.config = config; this.romance = romance;
        helper.Events.GameLoop.SaveLoaded += (_, _) => Load();
        helper.Events.GameLoop.Saving += (_, _) =>
        {
            SyncBlocks();
            StopRequest();
            if (Ready && state!.IsValid(Game1.player.UniqueMultiplayerID)) helper.Data.WriteSaveData(SaveKey, state);
        };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { Close(); StopRequest(); state = null; };
        helper.Events.GameLoop.DayEnding += (_, _) => { Close(); StopRequest(); };
        helper.Events.GameLoop.UpdateTicked += (_, _) => Tick();
        helper.Events.GameLoop.TimeChanged += (_, _) => Initiative();
        helper.Events.Input.ButtonPressed += (_, e) =>
        {
            if (e.Button != config.PhoneKey || !Ready || Game1.activeClickableMenu != null || Game1.eventUp || !Context.IsPlayerFree) return;
            helper.Input.Suppress(e.Button); Open();
        };
    }

    private void Load()
    {
        Close(); StopRequest(); state = null; Notice = "";
        if (!romance.Ready) return;
        try
        {
            var loaded = helper.Data.ReadSaveData<PhoneState>(SaveKey);
            if (loaded != null && !loaded.IsValid(Game1.player.UniqueMultiplayerID)) throw new InvalidDataException();
            state = loaded ?? new() { FarmerId = Game1.player.UniqueMultiplayerID };
            state.Interrupt();
            SyncBlocks();
        }
        catch { monitor.Log("Phone save could not be read; existing data preserved and phone disabled for this session.", LogLevel.Warn); }
    }

    internal void Open()
    {
        if (!Ready || Game1.eventUp) return;
        menu = new PhoneMenu(this);
        Game1.activeClickableMenu = menu;
    }

    internal void Close()
    {
        menu?.ReleaseInput();
        if (menu != null && ReferenceEquals(Game1.activeClickableMenu, menu)) Game1.exitActiveMenu();
        menu = null;
    }

    internal bool Send(string name, string text)
    {
        SyncBlocks();
        if (!Ready || Busy || !state!.CanText(name)) return false;
        var message = state!.Send(name, text, Game1.Date.TotalDays, Game1.timeOfDay);
        if (message == null) { Notice = "Use 1-500 characters. Retry or dismiss any failed text first."; return false; }
        Request(name, message);
        return true;
    }

    internal void Retry(string name)
    {
        SyncBlocks();
        if (!Ready || Busy) return;
        var message = state!.Retry(name);
        if (message != null) Request(name, message);
    }

    internal void Dismiss(string name)
    {
        if (!Ready || Busy) return;
        // Keep the text reusable, but remove the failed delivery rather than implying it was received.
        state!.Thread(name).Messages.RemoveAll(m => m.Outgoing && m.Status == "failed");
        Notice = "Unsent text dismissed; its quick message is still available.";
    }

    private static string? Key() => Environment.GetEnvironmentVariable("GEMINI_API_KEY", EnvironmentVariableTarget.User)
        ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

    private void Request(string name, PhoneMessage? message)
    {
        pendingName = name; pendingId = message?.Id ?? ""; pendingText = message?.Text ?? "";
        requestDay = Game1.Date.TotalDays; requestTime = Game1.timeOfDay;
        Notice = "";
        if (!config.EnableAbigailAi || string.IsNullOrWhiteSpace(Key()))
        {
            state!.Fail(pendingId); Notice = "AI texting is unavailable. Check AI settings, then Retry."; return;
        }
        try
        {
            pendingContext = JsonSerializer.Serialize(new {
                Relationship = romance.GetPhoneContext(name, pendingText),
                PhoneHistory = state!.Thread(name).Messages.Where(m => m.Status == "sent").TakeLast(12)
                    .Select(m => new { m.Day, m.Time, Speaker = m.Outgoing ? "Farmer" : name, m.Text }),
                Channel = "remote phone text" });
            cancel = new CancellationTokenSource();
            pending = new GeminiConversation(Http).TextForCharacter(Key()!, config.GeminiModel, pendingContext, pendingText, name, message == null, cancel.Token);
        }
        catch { state!.Fail(pendingId); Notice = "Could not send. Retry when ready."; cancel?.Dispose(); cancel = null; }
    }

    private void Tick()
    {
        if (menu != null && !ReferenceEquals(Game1.activeClickableMenu, menu)) { menu.ReleaseInput(); menu = null; }
        if (!Ready) { if (pending != null) StopRequest(); return; }
        SyncBlocks();
        if (pending == null) return;
        if (state!.FarmerId != Game1.player.UniqueMultiplayerID || Game1.Date.TotalDays != requestDay) { StopRequest(); return; }
        if (!pending.IsCompleted) return;
        try
        {
            var reply = pending.GetAwaiter().GetResult();
            bool applied = pendingId == "" ? state.Incoming(pendingName, reply.Reply, requestDay, requestTime) : state.Complete(pendingId, reply.Reply);
            if (applied)
            {
                if (pendingId == "")
                {
                    // An unsolicited text is NPC speech, never a fabricated farmer memory.
                    reply.Memories = new();
                }
                romance.RememberPhoneReply(pendingName, pendingText, reply, pendingContext);
                if (menu?.Contact == pendingName) state.Thread(pendingName).Unread = false;
                else Game1.addHUDMessage(new HUDMessage($"New text from {pendingName} ({config.PhoneKey})", HUDMessage.newQuest_type));
            }
            else throw new InvalidDataException();
        }
        catch
        {
            state.Fail(pendingId);
            Notice = "No reply received. Retry sends the same text without adding a duplicate.";
            monitor.Log("Phone reply unavailable. No provider details were logged.", LogLevel.Warn);
        }
        pending = null; cancel?.Dispose(); cancel = null;
    }

    private void Initiative()
    {
        SyncBlocks();
        if (!Ready || Busy || !config.EnablePhoneInitiative || !config.EnableAbigailAi || string.IsNullOrWhiteSpace(Key())
            || Game1.eventUp || Game1.activeClickableMenu != null || !Context.IsPlayerFree || Game1.timeOfDay < 1000 || Game1.timeOfDay > 1800) return;
        int day = Game1.Date.TotalDays;
        var eligible = Contacts.Where(n => Game1.player.getFriendshipHeartLevelForNPC(n) >= 2)
            .OrderBy(n => state!.Threads.GetValueOrDefault(n)?.LastInitiativeDay ?? -100).ThenBy(n => n);
        foreach (string name in eligible)
            if (state!.TryInitiative(name, day)) { Request(name, null); break; }
    }

    private void StopRequest()
    {
        cancel?.Cancel(); cancel?.Dispose(); cancel = null;
        if (pending != null) _ = pending.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        pending = null; state?.Interrupt();
    }

    private void SyncBlocks()
    {
        if (!Ready) return;
        foreach (string name in RomanceRules.Candidates)
        {
            bool divorced = Game1.player.friendshipData.TryGetValue(name, out var native) && native.IsDivorced();
            state!.SetBlock(name, config.EnableAutomaticPhoneBlocking
                ? PhoneState.RelationshipBlock(romance.State.Characters.GetValueOrDefault(name), divorced) : "");
        }
        if (state!.CanText(pendingName) && Notice == pendingName + " has blocked texting. Your conversation history is kept.") Notice = "";
        if (pending != null && !state!.CanText(pendingName))
        {
            StopRequest(); Notice = pendingName + " has blocked texting. Your conversation history is kept.";
        }
    }

    internal bool OfferNumber(NPC npc, Action continueConversation)
    {
        SyncBlocks();
        if (!Ready || !RomanceService.Supported(npc) || Game1.eventUp
            || !state!.TryOfferNumber(npc.Name, Game1.Date.TotalDays)) return false;
        var offeredState = state;
        int offeredDay = Game1.Date.TotalDays;
        long farmer = Game1.player.UniqueMultiplayerID;
        string offer = npc.Name switch
        {
            "Abigail" => "Hey, want to exchange numbers? You can text me when you're taking a break.",
            "Alex" => "We should swap numbers! It'll be easier to keep in touch.",
            "Elliott" => "Would you like to exchange numbers? A message is welcome when our paths don't cross.",
            "Emily" => "Oh! Shall we exchange numbers? I'd love to hear how your day is going.",
            "Haley" => "Want to exchange numbers? It's easier than trying to find each other around town.",
            "Harvey" => "Would you like to exchange numbers? No pressure to reply while you're busy on the farm.",
            "Leah" => "Want to swap numbers? We can catch up when there's a quiet moment.",
            "Maru" => "We could exchange numbers, if you'd like. Texting is handy when we're both working.",
            "Penny" => "If you'd like, we could exchange numbers. It would be nice to keep in touch.",
            "Sam" => "Hey, let's swap numbers! You can text me whenever you get a break.",
            "Sebastian" => "Want to exchange numbers? Sometimes texting is easier for me.",
            _ => "I don't always answer right away, but... want to exchange numbers?"
        };
        if (Game1.activeClickableMenu is StardewValley.Menus.DialogueBox previous && NpcConversation.CanStart(previous)) previous.closeDialogue();
        Game1.currentLocation.createQuestionDialogue(npc.displayName + ": " + offer,
            new[] { new Response("phone:yes", "Sure, let's exchange numbers."), new Response("phone:no", "Maybe another time.") },
            (who, answer) =>
            {
                if (!Ready || !ReferenceEquals(state, offeredState) || Game1.player.UniqueMultiplayerID != farmer || Game1.Date.TotalDays != offeredDay) return;
                SyncBlocks();
                if (state!.AnswerNumberOffer(npc.Name, answer == "phone:yes", offeredDay) && answer == "phone:yes")
                    Game1.addHUDMessage(new HUDMessage(npc.displayName + $" added to your phone ({config.PhoneKey}).", HUDMessage.newQuest_type));
                continueConversation();
            }, npc);
        return true;
    }
}
