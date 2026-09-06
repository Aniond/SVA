using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void RomanceConversationChecks()
    {
        RequireWorld();
        if (Game1.eventUp) throw new InvalidOperationException("Finish the active event before checking conversation entry.");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object mod = AiMod(Helper);
        object conversation = mod.GetType().GetField("abigailConversation", flags)!.GetValue(mod)!;
        var type = conversation.GetType();
        if (type.GetField("pending", flags)!.GetValue(conversation) != null)
            throw new InvalidOperationException("Finish or cancel the active conversational reply first.");
        var conversationFields = type.GetFields(flags).Where(f => !f.IsInitOnly).ToDictionary(f => f, f => f.GetValue(conversation));
        var speaker = type.GetField("Speaker", BindingFlags.Static | BindingFlags.NonPublic)!;
        object? originalSpeaker = speaker.GetValue(null);
        var live = LiveRomanceFixture();
        var farmField = live.Service.GetType().GetField("farm", flags)!;
        object? originalFarm = farmField.GetValue(live.Service);
        var selectedField = live.Service.GetType().GetField("selected", flags)!;
        object? originalSelected = selectedField.GetValue(live.Service);
        var selections = (Dictionary<string, HashSet<string>>)live.Service.GetType().GetField("selectedMemories", flags)!.GetValue(live.Service)!;
        var originalSelections = selections.ToDictionary(p => p.Key, p => new HashSet<string>(p.Value));
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        var memoryField = observer.GetType().GetField("memory", flags)!;
        object? originalMemory = memoryField.GetValue(observer);
        var transient = new[] { "offeredReminder", "selectedExperiences" }.Select(n => observer.GetType().GetField(n, flags))
            .Where(f => f != null).ToDictionary(f => f!, f => f!.GetValue(observer));
        var originalMenu = Game1.activeClickableMenu;
        var originalSubscriber = Game1.keyboardDispatcher.Subscriber;
        bool dialogueUp = Game1.dialogueUp, dialogueTyping = Game1.dialogueTyping, canMove = Game1.player.CanMove, usingTool = Game1.player.UsingTool;
        int freeze = Game1.player.freezePause;
        string friendshipBefore = JsonSerializer.Serialize(Game1.player.friendshipData.Pairs.Select(p => new { p.Key, p.Value.Points, p.Value.Status, p.Value.TalkedToToday, p.Value.GiftsToday }).ToArray());
        var results = new List<object>();
        void Check(string name, bool passed) => results.Add(new { Name = name, Passed = passed });
        bool CanStart(DialogueBox box) => (bool)type.GetMethod("CanStart", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { box })!;
        var memories = RomanceRules.Candidates.ToDictionary(n => n, _ => new AbigailMemory());
        object fixture = Activator.CreateInstance(farmField.FieldType, true)!;
        farmField.FieldType.GetProperty("FarmerId")!.SetValue(fixture, Game1.player.UniqueMultiplayerID);
        farmField.FieldType.GetProperty("State")!.SetValue(fixture, new RomanceSaveState { LatestDay = Game1.Date.TotalDays });
        farmField.FieldType.GetProperty("Memories")!.SetValue(fixture, memories);
        try
        {
            farmField.SetValue(live.Service, fixture);
            memoryField.SetValue(observer, memories["Abigail"]);
            foreach (string name in RomanceRules.Candidates)
            {
                NPC npc = Game1.getCharacterFromName(name);
                Call(conversation, "Reset");
                var native = new DialogueBox(new Dialogue(npc, null, "Hello."));
                Check(name + " ordinary native dialogue can enter conversation", CanStart(native));
                Game1.activeClickableMenu = native;
                Call(conversation, "Start", npc);
                Check(name + " opens the generic input", Game1.activeClickableMenu is NamingMenu && Game1.activeClickableMenu.GetType().Name == "ConversationInput");
                Check(name + " is the active speaker", (string?)speaker.GetValue(null) == name);
                string? context = (string?)type.GetField("context", flags)!.GetValue(conversation);
                Check(name + " input carries its own profile", context != null && JsonNode.Parse(context)?["Relationship"]?["Personality"]?["Name"]?.GetValue<string>() == name);
                var portrait = Game1.content.Load<Texture2D>("Portraits/" + name);
                int cells = portrait.Width / 64 * (portrait.Height / 64);
                Check(name + " portrait asset contains every mapped expression", cells > 0 && AbigailExpression.Names.All(e => RomanceProfiles.Get(name)!.PortraitIndex(e) < cells));
                Check(name + " opening does not submit an AI request", type.GetField("pending", flags)!.GetValue(conversation) == null);
                if (name != "Abigail") Check(name + " input has no Abigail quest choices", ((Array)Call(conversation, "QuestChoices")!).Length == 0);
            }
            Call(conversation, "Reset");
            var nonCandidate = Game1.getCharacterFromName("Lewis");
            var lewisBox = new DialogueBox(new Dialogue(nonCandidate, null, "Good morning."));
            Check("Noncandidate native dialogue stays native", !CanStart(lewisBox));
            Game1.activeClickableMenu = lewisBox;
            Call(conversation, "Start", nonCandidate);
            Check("Explicit start rejects a noncandidate", ReferenceEquals(Game1.activeClickableMenu, lewisBox));

            int sideEffects = 0;
            var guardedDialogue = new Dialogue(Game1.getCharacterFromName("Abigail"), null, "A quest handover is waiting.");
            var guarded = new DialogueBox(guardedDialogue);
            guardedDialogue.dialogues[0].SideEffects = () => sideEffects++;
            Game1.activeClickableMenu = guarded;
            Check("Quest side-effect dialogue is excluded", !CanStart(guarded));
            Call(conversation, "Tick");
            Check("Automatic entry preserves the native quest dialogue and its deferred action", ReferenceEquals(Game1.activeClickableMenu, guarded) && sideEffects == 0);
            guardedDialogue.dialogues[0].SideEffects = null;
            guardedDialogue.onFinish = () => sideEffects++;
            Check("Completion callbacks remain native", !CanStart(guarded) && sideEffects == 0);
            guardedDialogue.onFinish = null;
            guardedDialogue.answerQuestionBehavior = _ => true;
            Check("Question callbacks remain native", !CanStart(guarded));
            guardedDialogue.answerQuestionBehavior = null;
            guarded.isQuestion = true;
            Check("Native question menus remain native", !CanStart(guarded));
            string friendshipAfter = JsonSerializer.Serialize(Game1.player.friendshipData.Pairs.Select(p => new { p.Key, p.Value.Points, p.Value.Status, p.Value.TalkedToToday, p.Value.GiftsToday }).ToArray());
            Check("Opening inputs leaves native friendship and gifts unchanged", friendshipBefore == friendshipAfter);
        }
        catch (Exception ex) { results.Add(new { Name = "Conversation entry checks completed", Passed = false, Error = ex.GetBaseException().Message }); }
        finally
        {
            Call(conversation, "Reset");
            foreach (var pair in conversationFields) pair.Key.SetValue(conversation, pair.Value);
            speaker.SetValue(null, originalSpeaker);
            farmField.SetValue(live.Service, originalFarm);
            selectedField.SetValue(live.Service, originalSelected);
            memoryField.SetValue(observer, originalMemory);
            foreach (var pair in transient) pair.Key.SetValue(observer, pair.Value);
            selections.Clear();
            foreach (var pair in originalSelections) selections[pair.Key] = pair.Value;
            Game1.activeClickableMenu = originalMenu;
            Game1.keyboardDispatcher.Subscriber = originalSubscriber;
            Game1.dialogueUp = dialogueUp; Game1.dialogueTyping = dialogueTyping;
            Game1.player.CanMove = canMove; Game1.player.UsingTool = usingTool; Game1.player.freezePause = freeze;
        }
        Helper.Data.WriteJsonFile("romance-conversation-results.json", results);
    }
}
