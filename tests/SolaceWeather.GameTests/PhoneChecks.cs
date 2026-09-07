using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private const BindingFlags PhoneFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private object Phone()
    {
        RequireWorld();
        if (Game1.player.Name != "SvaAudit" || !Program.GetSavesFolder().Contains("phone", StringComparison.OrdinalIgnoreCase)
            || !Program.GetSavesFolder().Contains("artifacts", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Phone checks require a disposable phone audit profile and farmer.");
        return AiMod(Helper).GetType().GetField("phone", PhoneFlags)!.GetValue(AiMod(Helper))!;
    }
    private static T PhoneGet<T>(object target, string name) => target.GetType().GetField(name, PhoneFlags) is { } field
        ? (T)field.GetValue(target)! : (T)target.GetType().GetProperty(name, PhoneFlags)!.GetValue(target)!;
    private static void PhoneSet(object target, string name, object? value) => target.GetType().GetField(name, PhoneFlags)!.SetValue(target, value);

    private void PhoneChecks()
    {
        var phone = Phone();
        if (PhoneGet<bool>(phone, "Busy")) throw new InvalidOperationException("Wait for existing request.");
        var original = PhoneGet<PhoneState>(phone, "State");
        var config = PhoneGet<ModConfig>(phone, "config");
        bool ai = config.EnableAbigailAi;
        var results = new Dictionary<string, bool>();
        void Check(string name, bool pass) => results[name] = pass;
        var originalFriendships = Game1.player.friendshipData.Pairs.ToDictionary(p => p.Key, p => p.Value);
        var romance = PhoneGet<object>(phone, "romance");
        var farm = PhoneGet<object>(romance, "farm");
        var memories = PhoneGet<Dictionary<string, AbigailMemory>>(farm, "Memories");
        var memorySnapshot = memories.ToDictionary(p => p.Key, p => JsonSerializer.Serialize(p.Value));
        var abigailObserver = PhoneGet<object>(AiMod(Helper), "abigail");
        object? oldReminder = abigailObserver.GetType().GetField("offeredReminder", PhoneFlags)!.GetValue(abigailObserver);
        try
        {
            PhoneSet(phone, "state", new PhoneState { FarmerId = Game1.player.UniqueMultiplayerID });
            config.EnableAbigailAi = false;
            foreach (string name in RomanceRules.Candidates)
            {
                Game1.player.friendshipData[name] = new Friendship(500);
                var fixtureState = PhoneGet<PhoneState>(phone, "State");
                fixtureState.TryOfferNumber(name, 0); fixtureState.AnswerNumberOffer(name, true, 0);
            }
            Check("new phone is empty", PhoneGet<PhoneState>(phone, "State").Threads.Count == 0);
            Call(phone, "Open");
            var menu = Game1.activeClickableMenu;
            Check("inbox opens", menu?.GetType().Name == "PhoneMenu");
            string wrapped = (string)menu!.GetType().GetMethod("Wrap", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { new string('W', 500), 333, .85f })!;
            Check("unbroken text fits bubble width", wrapped.Split('\n').All(line => Game1.smallFont.MeasureString(line).X * .85f <= 333));
            Check("all twelve accepted contacts", PhoneGet<string[]>(phone, "Contacts").Length == 12);
            Call(menu!, "Select", "Abigail");
            var input = PhoneGet<TextBox>(menu!, "input");
            Check("chat owns keyboard input", ReferenceEquals(Game1.keyboardDispatcher.Subscriber, input));
            Check("empty text rejected", Call(phone, "Send", "Abigail", " ") is false);
            input.Text = "I enjoy quiet rain and purple flowers.";
            input.RecieveCommandInput('\r');
            var state = PhoneGet<PhoneState>(phone, "State");
            var sent = state.Thread("Abigail").Messages.Single();
            Check("missing AI produces visible retry state", sent.Status == "failed");
            Check("outgoing text saved as shortcut", state.Thread("Abigail").Shortcuts.Single() == sent.Text);
            Check("accepted draft cleared", input.Text == "");
            Check("failed request blocks duplicate send", Call(phone, "Send", "Abigail", "Again") is false);
            state.Retry("Abigail");
            PhoneSet(phone, "pendingContext", JsonSerializer.Serialize(new { Relationship = new { OfferedFollowUp = (object?)null, SharedExperiences = Array.Empty<object>() } }));
            PhoneSet(phone, "pending", Task.FromResult(new ConversationReply { Reply = "Rain makes a good soundtrack for practicing my flute." }));
            Call(phone, "Tick");
            Check("retry completes one outgoing and one incoming", state.Thread("Abigail").Messages.Count == 2 && sent.Status == "sent");
            Check("phone conversation enters shared Living Memory", memories["Abigail"].Exchanges.Last().Farmer == sent.Text);
            var observer = PhoneGet<object>(AiMod(Helper), "abigail");
            PhoneSet(observer, "offeredReminder", "preserve-in-person");
            _ = Call(romance, "GetPhoneContext", "Abigail", "Hello");
            Check("phone context preserves in-person selection", PhoneGet<string>(observer, "offeredReminder") == "preserve-in-person");
            var reminder = new ConversationReply { Reply = "Still planning to bring that fish?", AskedTopic = "promise:fish" };
            Call(romance, "RememberPhoneReply", "Abigail", "Hello", reminder,
                JsonSerializer.Serialize(new { Relationship = new { OfferedFollowUp = new { Topic = "promise:fish" } } }));
            Check("phone promise reminder consumes daily allowance", memories["Abigail"].Promises.LastReminderDay == Game1.Date.TotalDays
                && memories["Abigail"].Personal.LastFollowUpDay == Game1.Date.TotalDays);
            using (var nextContext = JsonDocument.Parse(JsonSerializer.Serialize(Call(romance, "GetPhoneContext", "Abigail", "Hello"))))
                Check("next phone context does not repeat reminder", nextContext.RootElement.GetProperty("OfferedFollowUp").ValueKind == JsonValueKind.Null);
            Check("read chat has no unread badge", !state.Thread("Abigail").Unread);
            input.Text = "Draft stays between contacts";
            Call(menu!, "Select", "Sam"); Call(menu!, "Select", "Abigail");
            Check("contact switch preserves draft", input.Text == "Draft stays between contacts");
            Call(menu!, "Send", state.Thread("Abigail").Shortcuts.Single());
            Check("shortcut creates a distinct outgoing request", state.Thread("Abigail").Messages.Count == 3 && state.Thread("Abigail").Messages.Last().Id != sent.Id);
            Call(phone, "Dismiss", "Abigail");
            Check("dismiss keeps reusable outgoing text", state.Thread("Abigail").Messages.Count == 2 && state.Thread("Abigail").Shortcuts.Count == 1);
            menu!.receiveKeyPress(Keys.Escape);
            Check("escape closes phone", Game1.activeClickableMenu == null);
            Check("escape releases keyboard", !ReferenceEquals(Game1.keyboardDispatcher.Subscriber, input));
            Check("phone state remains valid", state.IsValid(Game1.player.UniqueMultiplayerID));
            var delayed = new TaskCompletionSource<ConversationReply>();
            state.Send("Abigail", "Delayed reply", Game1.Date.TotalDays, 900);
            PhoneSet(phone, "pending", delayed.Task);
            Call(phone, "Load");
            delayed.SetResult(new ConversationReply { Reply = "This must not reach the reloaded phone." });
            Call(phone, "Tick");
            Check("reload discards earlier session request", !PhoneGet<bool>(phone, "Busy")
                && !PhoneGet<PhoneState>(phone, "State").Threads.Values.SelectMany(t => t.Messages).Any(m => m.Text.Contains("must not reach")));
        }
        catch (Exception ex) { results["exception: " + ex.GetBaseException().GetType().Name] = false; }
        finally
        {
            Call(phone, "Close"); Call(phone, "StopRequest"); PhoneSet(phone, "state", original); config.EnableAbigailAi = ai;
            PhoneSet(abigailObserver, "offeredReminder", oldReminder);
            Game1.player.friendshipData.Clear(); foreach (var p in originalFriendships) Game1.player.friendshipData[p.Key] = p.Value;
            // Restore the same memory objects so Abigail's adopted reference remains valid.
            foreach (var name in memories.Keys.Except(memorySnapshot.Keys).ToArray()) memories.Remove(name);
            foreach (var p in memorySnapshot)
            {
                var restored = JsonSerializer.Deserialize<AbigailMemory>(p.Value)!;
                foreach (var property in typeof(AbigailMemory).GetProperties().Where(p => p.CanWrite)) property.SetValue(memories[p.Key], property.GetValue(restored));
            }
        }
        Helper.Data.WriteJsonFile("phone-checks.json", new { Passed = results.Values.All(v => v), Checks = results });
    }
    private void PhoneCapture()
    {
        var phone = Phone();
        StageBackgroundProgress();
        Game1.player.friendshipData["Abigail"] = new Friendship(500);
        var state = PhoneGet<PhoneState>(phone, "State");
        if (!state.HasNumber("Abigail")) throw new InvalidOperationException("Exchange Abigail's number first.");
        if (state.Thread("Abigail").Messages.Count == 0)
        {
            state.Incoming("Abigail", "Hey! Heading to the mines later?", Game1.Date.TotalDays, 900);
            var sent = state.Send("Abigail", "Hi, how are you doing?", Game1.Date.TotalDays, 910)!;
            state.Complete(sent.Id, "Pretty good! Practicing my sword swings. What are you up to?");
        }
        Call(phone, "Open"); Call(Game1.activeClickableMenu, "Select", "Abigail");
        captureRequested = true; captureDelay = 3;
    }
    private void PhoneCompact()
    {
        originalUiScale ??= Game1.options.baseUIScale; originalDesiredUiScale = Game1.options.desiredUIScale;
        Game1.options.baseUIScale = 1.5f; Game1.options.desiredUIScale = 1.5f; Game1.game1.refreshWindowSettings();
        PhoneCapture();
    }
    private void PhoneAi(bool shortcut)
    {
        var phone = Phone(); StageBackgroundProgress();
        Game1.player.friendshipData["Abigail"] = new Friendship(500);
        var state = PhoneGet<PhoneState>(phone, "State");
        string text = shortcut ? state.Thread("Abigail").Shortcuts.First() : "I enjoy quiet rain and purple flowers. What do you like doing on rainy days?";
        Call(phone, "Open"); Call(Game1.activeClickableMenu, "Select", "Abigail");
        Call(Game1.activeClickableMenu, "Send", text);
        PhoneSnapshot();
    }
    private void PhoneInitiative()
    {
        var phone = Phone(); Call(phone, "Close");
        Game1.player.friendshipData["Sam"] = new Friendship(500);
        Game1.timeOfDay = 1100;
        var config = PhoneGet<ModConfig>(phone, "config");
        bool enabled = config.EnablePhoneInitiative;
        config.EnablePhoneInitiative = true;
        try { Call(phone, "Initiative"); } finally { config.EnablePhoneInitiative = enabled; }
        PhoneSnapshot();
    }
    private void PhoneSnapshot()
    {
        var phone = Phone();
        var romance = PhoneGet<object>(phone, "romance");
        var state = PhoneGet<PhoneState>(phone, "State");
        Helper.Data.WriteJsonFile("phone-state.json", new { Ready = PhoneGet<bool>(phone, "Ready"), Busy = PhoneGet<bool>(phone, "Busy"),
            Notice = PhoneGet<string>(phone, "Notice"), State = state, Valid = state.IsValid(Game1.player.UniqueMultiplayerID),
            Context = JsonSerializer.Serialize(Call(romance, "GetContext", "Abigail", "What do you remember about me?")),
            Menu = Game1.activeClickableMenu?.GetType().Name, Keyboard = Game1.keyboardDispatcher.Subscriber?.GetType().Name });
    }
    private void PhoneMic()
    {
        var phone = Phone();
        if (Game1.activeClickableMenu?.GetType().Name != "PhoneMenu") throw new InvalidOperationException("Open the phone chat first.");
        var dictation = PhoneGet<object>(Game1.activeClickableMenu, "dictation");
        Call(dictation, "Start");
        bool listening = PhoneGet<bool>(dictation, "Listening");
        string status = PhoneGet<string>(dictation, "Status");
        Call(dictation, "Stop");
        Helper.Data.WriteJsonFile("phone-mic.json", new { StartedListening = listening, Status = status,
            StoppedListening = !PhoneGet<bool>(dictation, "Listening"), TranscriptTested = false,
            Note = "Actual default audio device opened and stopped; no spoken transcript was supplied." });
    }
    private void PhoneClick()
    {
        var phone = Phone();
        var menu = Game1.activeClickableMenu;
        if (menu?.GetType().Name != "PhoneMenu") throw new InvalidOperationException("Open and draw a phone chat first.");
        var config = PhoneGet<ModConfig>(phone, "config");
        bool enabled = config.EnableAbigailAi;
        var state = PhoneGet<PhoneState>(phone, "State");
        int before = state.Thread("Abigail").Messages.Count;
        var input = PhoneGet<TextBox>(menu, "input");
        config.EnableAbigailAi = false;
        try
        {
            input.Text = "Checking the real Send button.";
            var button = (Rectangle)Call(menu, "R", new Rectangle(434, 690, 56, 56))!;
            menu.receiveLeftClick(button.Center.X, button.Center.Y);
            Helper.Data.WriteJsonFile("phone-click.json", new { Passed = state.Thread("Abigail").Messages.Count == before + 1
                && state.Thread("Abigail").Messages.Last().Text == "Checking the real Send button." && input.Text == "",
                Method = "Real rendered menu Send button hit test; AI disabled to avoid an extra paid call." });
            Call(phone, "Dismiss", "Abigail");
        }
        finally { config.EnableAbigailAi = enabled; }
    }
}
