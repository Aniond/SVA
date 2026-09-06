using System.Reflection;
using SolaceWeather.Core;
using StardewValley;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void CheckAbigail()
    {
        RequireWorld();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object info = Helper.ModRegistry.Get("David.SolaceWeather")!;
        object mod = info.GetType().GetProperty("Mod", flags)!.GetValue(info)!;
        object observer = mod.GetType().GetField("abigail", flags)!.GetValue(mod)!;
        var field = observer.GetType().GetField("memory", flags)!;
        var previous = field.GetValue(observer);
        var originalMenu = Game1.activeClickableMenu;
        bool existed = Game1.player.friendshipData.TryGetValue("Abigail", out var originalFriendship);
        var results = new List<object>();
        void Check(string name, bool pass) => results.Add(new { Name = name, Passed = pass });
        try
        {
            Check("Memory loaded from single-player save", previous is AbigailMemory);
            var memory = new AbigailMemory();
            field.SetValue(observer, memory);
            var friendship = new Friendship(123) { TalkedToToday = true, GiftsToday = 1 };
            Game1.player.friendshipData["Abigail"] = friendship;
            Call(observer, "Observe");
            Call(observer, "Observe");
            Check("Native observations deduplicated", memory.Days.Count == 1 && memory.Days[0].Gifts == 1 && memory.Days[0].Talked);
            object context = Call(observer, "GetContext")!;
            Check("Personality and relationship available", Get(context, "Personality") != null && (int)Get(context, "FriendshipPoints")! == 123);
            var copies = (AbigailDay[])Get(context, "Memories")!;
            copies[0].Gifts = 999;
            Check("Context cannot alter ledger", memory.Days[0].Gifts == 1);
            Call(observer, "OpenJournal");
            Check("Shared history opens", Game1.activeClickableMenu is StardewValley.Menus.DialogueBox);
            Check("Native friendship unchanged", friendship.Points == 123 && friendship.GiftsToday == 1 && friendship.TalkedToToday);
        }
        finally
        {
            field.SetValue(observer, previous);
            if (existed) Game1.player.friendshipData["Abigail"] = originalFriendship!;
            else Game1.player.friendshipData.Remove("Abigail");
            Game1.activeClickableMenu = originalMenu;
        }
        Helper.Data.WriteJsonFile("abigail-results.json", results);
    }
}
