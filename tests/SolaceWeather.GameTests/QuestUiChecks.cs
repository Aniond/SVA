using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void QuestUiChecks(string screen)
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var assembly = typeof(SolaceWeather.ModEntry).Assembly;
        var type = assembly.GetType("SolaceWeather.Relationships.NpcConversation")!;
        var inputType = type.GetNestedType("ConversationInput", BindingFlags.NonPublic)!;
        var replyType = type.GetNestedType("DeliveryReplyBox", BindingFlags.NonPublic)!;
        var rowType = assembly.GetType("SolaceWeather.Relationships.QuestChoice")!;
        Array rows = Array.CreateInstance(rowType, 3), empty = Array.CreateInstance(rowType, 0);
        string[] labels = { "I'll bring it tomorrow (Spring 5, Year 1)", "Give me three days (Spring 7, Year 1)", "Not right now" };
        for (int i = 0; i < 3; i++) rows.SetValue(Activator.CreateInstance(rowType, "choice" + i, labels[i]), i);
        bool available = true;
        Func<Array> getRows = () => available ? rows : empty;
        Delegate choices = Expression.Lambda(Expression.GetFuncType(rows.GetType()),
            Expression.Convert(Expression.Invoke(Expression.Constant(getRows)), rows.GetType())).Compile();
        int clicks = 0;
        Action<string> choose = _ => clicks++;
        Func<string[]> terms = () => new[] { "Quartz", "Iron Bar", "Sardine", "fish" };
        NamingMenu Input() => (NamingMenu)Activator.CreateInstance(inputType, flags, null,
            new object[] { (Action<string>)(_ => { }), (Action)(() => Game1.exitActiveMenu()), choices, terms, choose }, null)!;
        var input = Input();
        var first = (Rectangle)Call(input, "QuestBounds", 0)!;
        var last = (Rectangle)Call(input, "QuestBounds", 2)!;
        var results = new List<object>();
        void Check(string name, bool passed) => results.Add(new { Name = name, Passed = passed });
        Check("Three choices fit below text entry", first.Y >= input.textBox.Y + 64 && last.Bottom <= Game1.uiViewport.Height);
        Check("Starter question controls are removed", inputType.GetField("suggestions", flags) == null);
        input.receiveLeftClick(first.Center.X, first.Center.Y);
        Check("Explicit row invokes one action", clicks == 1);
        available = false;
        Input().receiveLeftClick(first.Center.X, first.Center.Y);
        Check("Absent choices cannot invoke an action", clicks == 1);
        available = screen != "questthanks";
        string text = screen == "questthanks"
            ? "Abigail: You brought the Sardine! Thanks for remembering the fish I asked for. I appreciate it, Dave."
            : "Abigail: Could you bring me a Quartz? There's something about those stones that makes me want to find out what else is hiding underground. I want to examine one properly. You don't have to agree just because I asked, though. Tell me whether tomorrow or three days works for you. I'd rather you chose a day you can actually manage.";
        var reply = (DialogueBox)Activator.CreateInstance(replyType, flags, null, new object[] { text, choices, terms(), choose, screen == "questthanks" ? "warm" : "thoughtful" }, null)!;
        reply.transitioning = false; reply.transitionInitialized = true;
        reply.characterIndexInDialogue = text.Length;
        var replyFirst = (Rectangle)Call(reply, "QuestBounds", 0)!;
        var replyLast = (Rectangle)Call(reply, "QuestBounds", 2)!;
        Check("Three reply choices fit below dialogue", screen == "questthanks" || (replyFirst.Y >= reply.y + reply.height && replyLast.Bottom <= Game1.uiViewport.Height));
        if (available) reply.receiveLeftClick(replyFirst.Center.X, replyFirst.Center.Y);
        Check("Reply row invokes one action", clicks == (available ? 2 : 1));
        Game1.activeClickableMenu = screen == "questinput" ? input : reply;
        captureRequested = true; captureDelay = 2;
        Helper.Data.WriteJsonFile("quest-ui-results.json", results);
    }
}

