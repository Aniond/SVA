using System.Reflection;
using System.Text.Json;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;
namespace SolaceWeather.GameTests;
public sealed partial class ModEntry
{
    private void ExperienceChecks()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var mod = AiMod(Helper); var observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        var field = observer.GetType().GetField("memory", flags)!;
        var original = (AbigailMemory)field.GetValue(observer)!;
        var memory = JsonSerializer.Deserialize<AbigailMemory>(JsonSerializer.Serialize(original))!;
        int points = Game1.player.friendshipData["Abigail"].Points;
        var results = new List<object>();
        void Check(string name, bool pass) => results.Add(new { Name = name, Passed = pass });
        try
        {
            field.SetValue(observer, memory);
            memory.Experiences.Record("test:music", "music", Game1.Date.TotalDays, "A verified test flute break occurred.", "flute music");
            var context = JsonSerializer.Serialize(Call(observer, "GetConversationContext", "That flute music was comforting."));
            Check("Production context includes verified experiences and attribution boundary", context.Contains("test:music") && context.Contains("VerifiedFact") && context.Contains("RelatedConversations"));
            int count = memory.Experiences.Entries.Count;
            Call(observer, "RememberReply", "I fought a dragon", new ConversationReply { Reply = "That's quite a claim.", RecalledExperienceId = "invented:event" });
            Check("AI cannot create an experience by naming an invented event", memory.Experiences.Entries.Count == count);
            var reply = new ConversationReply { Reply = "I'm glad the music helped you.", RecalledExperienceId = "test:music" };
            Call(observer, "RememberReply", "The music helped me", reply);
            var moment = memory.Experiences.Entries.Single(e => e.Id == "test:music");
            Check("Relevant conversation is retained alongside the verified event", moment.Conversations.Count == 1 && moment.Fact == "A verified test flute break occurred.");
            Call(observer, "RememberReply", "The music helped me", reply);
            Check("Repeated acknowledgement does not duplicate the reflection", moment.Conversations.Count == 1);
            Check("Native friendship remains unchanged", Game1.player.friendshipData["Abigail"].Points == points);
            Check("Complete memory remains valid", memory.IsValid());
        }
        finally { field.SetValue(observer, original); Helper.Data.WriteJsonFile("experience-results.json", results); }
    }
    private void ExperienceAi()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var mod = AiMod(Helper); var conversation = mod.GetType().GetField("abigailConversation", flags)!.GetValue(mod)!;
        Call(conversation, "Reset");
        Game1.activeClickableMenu = new DialogueBox(new Dialogue(Game1.getCharacterFromName("Abigail"), null, "Hello."));
        Call(conversation, "Tick");
        if (Game1.activeClickableMenu is not NamingMenu input) throw new InvalidOperationException("Conversation input missing.");
        aiCount = LiveMemory().Exchanges.Count; aiPoints = Game1.player.friendshipData["Abigail"].Points; aiWatching = true;
        input.textBox.Text = "Do you remember the minerals we've looked at together? I like how curious you get about them.";
        input.textBoxEnter(input.textBox);
    }
}
