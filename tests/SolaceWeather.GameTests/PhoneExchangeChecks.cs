using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void PhoneAnswer(bool accept)
    {
        Phone();
        if (Game1.activeClickableMenu is not DialogueBox question || !question.isQuestion || question.transitioning)
            throw new InvalidOperationException("Wait for a visible number-exchange question.");
        question.selectedResponse = accept ? 0 : 1;
        question.receiveLeftClick(0, 0);
        Helper.Data.WriteJsonFile("phone-native-answer.json", new { Accept = accept, ContinuedConversation = Game1.activeClickableMenu is NamingMenu,
            Method = "Native DialogueBox receiveLeftClick including its after-answer outro sequence." });
    }
    private void PhoneNumber(string name = "Abigail")
    {
        var phone = Phone();
        var state = PhoneGet<PhoneState>(phone, "State");
        Game1.player.friendshipData[name] = new Friendship(500);
        Call(phone, "Close");
        if (!state.HasNumber(name))
        {
            var conversation = PhoneGet<object>(AiMod(Helper), "abigailConversation");
            Call(conversation, "Start", Game1.getCharacterFromName(name));
            if (Game1.activeClickableMenu is not DialogueBox { isQuestion: true }) throw new InvalidOperationException("Expected native number exchange question.");
        }
        captureRequested = true; captureDelay = 3;
    }

    private void PhoneExchangeChecks()
    {
        var phone = Phone();
        if (PhoneGet<bool>(phone, "Busy")) throw new InvalidOperationException("Wait for pending reply.");
        var original = PhoneGet<PhoneState>(phone, "State");
        var config = PhoneGet<ModConfig>(phone, "config");
        bool ai = config.EnableAbigailAi;
        bool blocking = config.EnableAutomaticPhoneBlocking;
        var romance = PhoneGet<object>(phone, "romance");
        var farm = PhoneGet<object>(romance, "farm");
        var originalRomance = PhoneGet<RomanceSaveState>(farm, "State");
        var testRomance = new RomanceSaveState();
        var friendships = Game1.player.friendshipData.Pairs.ToDictionary(p => p.Key, p => p.Value);
        var conversation = PhoneGet<object>(AiMod(Helper), "abigailConversation");
        var checks = new Dictionary<string, bool>();
        void Check(string name, bool passed) => checks[name] = passed;
        var state = new PhoneState { FarmerId = Game1.player.UniqueMultiplayerID };
        try
        {
            Call(phone, "Close"); config.EnableAbigailAi = false; PhoneSet(phone, "state", state);
            farm.GetType().GetProperty("State")!.SetValue(farm, testRomance);
            Game1.player.friendshipData["Abigail"] = new Friendship(500);
            Game1.player.friendshipData["Sam"] = new Friendship(500);
            Check("meeting alone adds no phone contact", PhoneGet<string[]>(phone, "Contacts").Length == 0);
            Check("locked contact cannot send or start provider", Call(phone, "Send", "Abigail", "Hello") is false && !PhoneGet<bool>(phone, "Busy"));
            Call(conversation, "Start", Game1.getCharacterFromName("Abigail"));
            Check("in-person meeting offers native question", Game1.activeClickableMenu is DialogueBox { isQuestion: true });
            Game1.currentLocation.answerDialogue(new Response("phone:no", "Maybe another time."));
            Check("decline leaves contact locked", !state.HasNumber("Abigail"));
            Check("decline continues ordinary conversation", Game1.activeClickableMenu is NamingMenu);
            Call(conversation, "Start", Game1.getCharacterFromName("Abigail"));
            Check("same-day talk does not repeat offer", Game1.activeClickableMenu is NamingMenu);
            state.Contacts["Abigail"].LastOfferDay = Game1.Date.TotalDays - 3;
            Call(conversation, "Start", Game1.getCharacterFromName("Abigail"));
            Check("existing acquaintance gets later offer", Game1.activeClickableMenu is DialogueBox { isQuestion: true });
            Game1.currentLocation.answerDialogue(new Response("phone:yes", "Sure, let's exchange numbers."));
            Check("accept unlocks only that contact", state.HasNumber("Abigail") && PhoneGet<string[]>(phone, "Contacts").SequenceEqual(new[] { "Abigail" }));
            Check("accept continues ordinary conversation", Game1.activeClickableMenu is NamingMenu);
            Call(conversation, "Reset"); Game1.exitActiveMenu();
            Call(phone, "Open"); var menu = Game1.activeClickableMenu;
            Call(menu, "Select", "Abigail");
            var input = PhoneGet<TextBox>(menu, "input"); input.Text = "Keep my draft";
            Call(menu, "ToggleDropdown");
            Check("dropdown releases typing focus", !ReferenceEquals(Game1.keyboardDispatcher.Subscriber, input));
            menu.receiveKeyPress(Keys.Enter);
            Check("dropdown selection preserves draft", input.Text == "Keep my draft" && PhoneGet<string>(menu, "Contact") == "Abigail");
            config.EnableAutomaticPhoneBlocking = false;
            Game1.player.friendshipData["Abigail"].Status = FriendshipStatus.Divorced;
            Call(phone, "Tick");
            Check("automatic blocking defaults on but explicit opt-out works", new ModConfig().EnableAutomaticPhoneBlocking && state.CanText("Abigail"));
            Game1.player.friendshipData["Abigail"].Status = FriendshipStatus.Friendly;
            config.EnableAutomaticPhoneBlocking = true;
            var message = state.Send("Abigail", "In-flight text", Game1.Date.TotalDays, 900)!;
            var delayed = new TaskCompletionSource<ConversationReply>();
            PhoneSet(phone, "pending", delayed.Task); PhoneSet(phone, "pendingName", "Abigail"); PhoneSet(phone, "pendingId", message.Id);
            Game1.player.friendshipData["Abigail"].Status = FriendshipStatus.Divorced;
            Call(phone, "Tick"); delayed.SetResult(new ConversationReply { Reply = "Must not arrive after blocking" }); Call(phone, "Tick");
            Check("native falling-out blocks and cancels in-flight reply", !state.CanText("Abigail") && !PhoneGet<bool>(phone, "Busy") && message.Status == "failed");
            Check("blocked quick send cannot call provider", Call(phone, "Send", "Abigail", "In-flight text") is false && !PhoneGet<bool>(phone, "Busy"));
            Call(phone, "Retry", "Abigail");
            Check("blocked retry cannot call provider", !PhoneGet<bool>(phone, "Busy") && message.Status == "failed");
            Check("blocked history retained", state.Thread("Abigail").Messages.Count == 1);
            Game1.player.friendshipData["Abigail"].Status = FriendshipStatus.Friendly;
            Call(phone, "Tick");
            Check("native relationship reset restores texting", state.CanText("Abigail"));
            Check("recovery clears stale block notice", PhoneGet<string>(phone, "Notice") == "");
            var relationship = testRomance.Characters["Abigail"] = new() { IsDating = true, InConflict = true, PendingTransition = "breakup" };
            Check("authored breakup completes", RomanceRules.CompleteTransition(testRomance, "Abigail", Game1.Date.TotalDays));
            Call(phone, "Tick");
            Check("completed breakup remains blocked with native friendship", !state.CanText("Abigail") && relationship.PhoneBlockReason != "");
            Check("premature dating cannot restore phone", !RomanceRules.StartDating(testRomance, "Abigail", Game1.Date.TotalDays + 1) && !state.CanText("Abigail"));
            relationship.InterestExpressed = true; relationship.TalkDays = 12; relationship.CompletedActivities = 4;
            relationship.ActivityTypes = new() { "walk", "coffee" };
            Check("validated reconciliation succeeds", RomanceRules.StartDating(testRomance, "Abigail", Game1.Date.TotalDays + 28));
            Call(phone, "Tick");
            Check("validated reconciliation restores phone and keeps history", state.CanText("Abigail") && state.Thread("Abigail").Messages.Count == 1);
            Check("contact and block state validates", state.IsValid(Game1.player.UniqueMultiplayerID));
        }
        catch (Exception ex) { checks["exception: " + ex.GetBaseException().GetType().Name] = false; }
        finally
        {
            Call(conversation, "Reset"); Call(phone, "Close"); Call(phone, "StopRequest"); PhoneSet(phone, "state", original);
            Game1.exitActiveMenu(); config.EnableAbigailAi = ai;
            config.EnableAutomaticPhoneBlocking = blocking;
            farm.GetType().GetProperty("State")!.SetValue(farm, originalRomance);
            Game1.player.friendshipData.Clear(); foreach (var pair in friendships) Game1.player.friendshipData[pair.Key] = pair.Value;
        }
        Helper.Data.WriteJsonFile("phone-exchange-checks.json", new { Passed = checks.Values.All(v => v), Checks = checks });
    }
}
