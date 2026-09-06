using System.Reflection;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private Action? restoreRomanceWarning;
    private RomanceCharacterState? romanceWarningCharacter;
    private List<object>? romanceWarningResults;
    private int romanceWarningDay;
    private string? romanceWarningKey;
    private Dictionary<string, int>? romanceWarningHearts;

    private void StartRomanceWarningCheck()
    {
        RequireWorld();
        if (restoreRomanceWarning != null) throw new InvalidOperationException("Finish the existing warning-letter check first.");
        if (Game1.eventUp || Game1.Date.TotalDays < 2) throw new InvalidOperationException("Use a loaded farm after its first day, outside an event.");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var live = LiveRomanceFixture();
        var farmField = live.Service.GetType().GetField("farm", flags)!;
        object? originalFarm = farmField.GetValue(live.Service);
        var originalMailbox = Game1.player.mailbox.ToArray();
        var originalReceived = Game1.player.mailReceived.ToArray();
        var originalTomorrow = Game1.player.mailForTomorrow.ToArray();
        var originalMenu = Game1.activeClickableMenu;
        var subscriber = Game1.keyboardDispatcher.Subscriber;
        bool canMove = Game1.player.CanMove, dialogueUp = Game1.dialogueUp;
        romanceWarningHearts = Game1.player.friendshipData.Pairs.ToDictionary(p => p.Key, p => p.Value.Points);
        romanceWarningResults = new();
        void Check(string name, bool passed) => romanceWarningResults.Add(new { Name = name, Passed = passed });
        restoreRomanceWarning = () =>
        {
            farmField.SetValue(live.Service, originalFarm);
            Game1.player.mailbox.Clear(); foreach (string value in originalMailbox) Game1.player.mailbox.Add(value);
            Game1.player.mailReceived.Clear(); foreach (string value in originalReceived) Game1.player.mailReceived.Add(value);
            Game1.player.mailForTomorrow.Clear(); foreach (string value in originalTomorrow) Game1.player.mailForTomorrow.Add(value);
            Game1.activeClickableMenu = originalMenu;
            Game1.keyboardDispatcher.Subscriber = subscriber;
            Game1.player.CanMove = canMove;
            Game1.dialogueUp = dialogueUp;
            Helper.GameContent.InvalidateCache("Data/mail");
        };
        try
        {
            int day = Game1.Date.TotalDays;
            romanceWarningDay = day;
            var state = new RomanceSaveState();
            RomanceRules.ImportPartner(state, "Abigail", day - 1, false);
            RomanceRules.Reaffirm(state, "Abigail", day - 1);
            RomanceRules.RecordIncident(state, "notice:first", "Alex", day - 1, "flirt");
            RomanceRules.LearnIncident(state, "notice:first", "Abigail", day - 1, "Robin", 1, "Robin");
            RomanceRules.AdvanceDay(state, day);
            romanceWarningCharacter = state.Characters["Abigail"];
            object fixture = Activator.CreateInstance(farmField.FieldType, true)!;
            farmField.FieldType.GetProperty("FarmerId")!.SetValue(fixture, Game1.player.UniqueMultiplayerID);
            farmField.FieldType.GetProperty("State")!.SetValue(fixture, state);
            farmField.SetValue(live.Service, fixture);
            romanceWarningKey = "SolaceRomance_Abigail_" + romanceWarningCharacter.LastViolationSequence;
            // A prior run can have used the same notice ID. The original mail collections are restored below.
            Game1.player.mailbox.Remove(romanceWarningKey);
            Game1.player.mailReceived.Remove(romanceWarningKey);
            Game1.player.mailForTomorrow.Remove(romanceWarningKey);
            Call(live.Service, "QueueWarnings");
            var notices = (Dictionary<string, string>)farmField.FieldType.GetProperty("Notices")!.GetValue(fixture)!;
            Check("Authored warning is queued in mailbox and retained in Notices", Game1.player.mailbox.Contains(romanceWarningKey) && notices.ContainsKey(romanceWarningKey));
            Check("Queuing a warning does not count as delivering it", romanceWarningCharacter.WarningDeliveredDay == null);
            string originalKey = romanceWarningKey;
            int originalSequence = romanceWarningCharacter.LastViolationSequence;
            RomanceRules.RecordIncident(state, "notice:later", "Sam", day, "flirt");
            RomanceRules.LearnIncident(state, "notice:later", "Abigail", day, "Robin", 1, "Robin");
            Check("A later known incident changes the live sequence without deleting the queued notice", romanceWarningCharacter.LastViolationSequence > originalSequence && notices.ContainsKey(originalKey));
            Check("The unopened original letter remains undelivered after the later incident", romanceWarningCharacter.WarningDeliveredDay == null);
            var mail = Game1.content.Load<Dictionary<string, string>>("Data/mail");
            Check("The queued original letter remains available in the native mail asset", mail.TryGetValue(originalKey, out var content) && content == notices[originalKey]);
            Game1.activeClickableMenu = new LetterViewerMenu(mail[originalKey], originalKey, false);
            Check("The native letter opens with its original identity", ((LetterViewerMenu)Game1.activeClickableMenu).mailTitle == originalKey);
            // Finish is a separate command: the real UpdateTicked handler must observe the open letter first.
            Helper.Data.WriteJsonFile("romance-warning-results.json", new { Running = true, Letter = originalKey, Results = romanceWarningResults });
        }
        catch
        {
            restoreRomanceWarning(); restoreRomanceWarning = null;
            romanceWarningCharacter = null;
            throw;
        }
    }

    private void FinishRomanceWarningCheck()
    {
        RequireWorld();
        if (restoreRomanceWarning == null || romanceWarningCharacter == null || romanceWarningResults == null)
            throw new InvalidOperationException("Start the warning-letter check, allow a game tick, then finish it.");
        try
        {
            romanceWarningResults.Add(new { Name = "Opening the original queued letter delivers the warning despite a newer incident", Passed = romanceWarningCharacter.WarningDeliveredDay == romanceWarningDay });
            romanceWarningResults.Add(new { Name = "Reading a warning preserves native friendship points", Passed = romanceWarningHearts!.All(p => Game1.player.friendshipData.TryGetValue(p.Key, out var current) && current.Points == p.Value) });
            romanceWarningResults.Add(new { Name = "Reading a warning does not itself finish repair", Passed = romanceWarningCharacter.InConflict && !romanceWarningCharacter.RepairAcknowledged });
            Helper.Data.WriteJsonFile("romance-warning-results.json", new { Running = false, Letter = romanceWarningKey, Results = romanceWarningResults });
        }
        finally
        {
            restoreRomanceWarning(); restoreRomanceWarning = null;
            romanceWarningCharacter = null; romanceWarningResults = null; romanceWarningHearts = null;
        }
    }
}
