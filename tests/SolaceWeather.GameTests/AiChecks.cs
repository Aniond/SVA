using System.Reflection;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private int aiPoints;
    private int aiCount;
    private bool aiWatching;
    private void CheckAiClick()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object mod = AiMod(Helper);
        object movement = mod.GetType().GetField("movement", flags)!.GetValue(mod)!;
        object conversation = mod.GetType().GetField("abigailConversation", flags)!.GetValue(mod)!;
        NPC abigail = Game1.getCharacterFromName("Abigail");
        Game1.exitActiveMenu();
        bool accepted = (bool)Call(movement, "TryInteract", abigail.TilePoint)!;
        string? nativeType = Game1.activeClickableMenu?.GetType().Name;
        Call(conversation, "Tick");
        Helper.Data.WriteJsonFile("ai-click-results.json", new { ClickAccepted = accepted, NativeMenu = nativeType,
            TypingOpened = Game1.activeClickableMenu is NamingMenu, Game1.player.CanMove, Game1.player.UsingTool,
            FarmerTile = Game1.player.TilePoint.ToString(), AbigailTile = abigail.TilePoint.ToString(), Game1.fadeToBlack,
            DialogueLines = abigail.CurrentDialogue.Count });
        RestoreFocusPause();
        captureRequested = true;
        captureDelay = 30;
    }
    private void WatchAiCheck()
    {
        if (!aiWatching || Game1.activeClickableMenu is not DialogueBox box || !box.getCurrentString().StartsWith("Abigail:")) return;
        aiWatching = false;
        InspectAiCheck();
        captureRequested = true;
        captureDelay = 2;
    }
    private static object AiMod(StardewModdingAPI.IModHelper helper)
    {
        object info = helper.ModRegistry.Get("David.SolaceWeather")!;
        return info.GetType().GetProperty("Mod", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(info)!;
    }
    private void StartAiCheck()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object mod = AiMod(Helper);
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        var memory = (AbigailMemory)observer.GetType().GetField("memory", flags)!.GetValue(observer)!;
        aiCount = memory.Exchanges.Count;
        aiPoints = Game1.player.friendshipData.TryGetValue("Abigail", out var friendship) ? friendship.Points : 0;
        object conversation = mod.GetType().GetField("abigailConversation", flags)!.GetValue(mod)!;
        var abigail = Game1.getCharacterFromName("Abigail");
        Call(conversation, "Reset");
        Game1.activeClickableMenu = new DialogueBox(new Dialogue(abigail, null, "Hello."));
        Call(conversation, "Tick");
        if (Game1.activeClickableMenu is not NamingMenu input) throw new Exception("AI input did not open.");
        input.textBox.Text = "Hey Abigail, what do you like doing on a free afternoon?";
        if (input.textBox.Text.Length < 50) throw new Exception("Conversation input truncated the message.");
        aiWatching = true;
        input.textBoxEnter(input.textBox);
    }
    private void InspectAiCheck()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object mod = AiMod(Helper);
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        var memory = (AbigailMemory)observer.GetType().GetField("memory", flags)!.GetValue(observer)!;
        int points = Game1.player.friendshipData.TryGetValue("Abigail", out var friendship) ? friendship.Points : 0;
        Helper.Data.WriteJsonFile("ai-results.json", new
        {
            ReplyRemembered = memory.Exchanges.Count == aiCount + 1,
            ReplyDisplayed = Game1.activeClickableMenu is DialogueBox box && box.getCurrentString().StartsWith("Abigail:"),
            FriendshipUnchanged = points == aiPoints,
            TypedMessageSent = memory.Exchanges.LastOrDefault()?.Farmer == "Hey Abigail, what do you like doing on a free afternoon?",
            Reply = memory.Exchanges.LastOrDefault()?.Reply
        });
    }
}

