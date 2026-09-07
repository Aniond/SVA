using SolaceWeather.Controls;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Quests;

namespace SolaceWeather.Relationships;

internal sealed partial class HaleyLifeService
{
    private const string PromiseQuestId = "David.SolaceWeather/HaleySunflower";
    private void ObservePromise()
    {
        if (!Ready) return;
        foreach (var item in Game1.player.Items.Where(i => i != null)) state!.Promise.Observe(item.QualifiedItemId);
        state!.Promise.AdvanceDay(Today);
    }
    private static StardewValley.Object? Sunflower() => Game1.player.Items.OfType<StardewValley.Object>()
        .Where(i => i.QualifiedItemId == HaleyPromiseState.ItemId && i.Stack > 0 && !i.questItem.Value && !i.modData.ContainsKey(QuickStack.ProtectedKey))
        .OrderBy(i => i.Quality).FirstOrDefault();
    private string PromiseStamp => $"haley:promise:{state!.Promise.Status}:{state.Promise.OfferedDay}:{state.Promise.DueDay}:";
    private QuestChoice[] PromiseChoices()
    {
        var promise = state!.Promise;
        if (promise.Status == "offered") return new[] {
            new QuestChoice(PromiseStamp + "accept1", "I'll bring a sunflower tomorrow."),
            new QuestChoice(PromiseStamp + "accept3", "Give me three days for the sunflower."),
            new QuestChoice(PromiseStamp + "decline", "Not this time.") };
        if (promise.Status is not ("active" or "overdue")) return Array.Empty<QuestChoice>();
        var choices = new List<QuestChoice>();
        if (Sunflower() is { } item) choices.Add(new(PromiseStamp + "give:" + item.Quality, "Give Haley one " + item.DisplayName));
        if (promise.Status == "active" && !promise.Extended && Today <= promise.DueDay) choices.Add(new(PromiseStamp + "extend", "Could I have two more days?"));
        choices.Add(new(PromiseStamp + "abandon", "I can't finish this promise."));
        return choices.ToArray();
    }
    private QuestActionResult? ApplyPromise(string key)
    {
        if (!key.StartsWith(PromiseStamp, StringComparison.Ordinal)) return null;
        string action = key[PromiseStamp.Length..]; var promise = state!.Promise;
        if (action is "accept1" or "accept3" && promise.Accept(Today, action == "accept1" ? 1 : 3))
            return new("I'll bring you the sunflower.", $"The farmer explicitly promised one sunflower for Haley's photography still life by {AbigailDeliveryQuest.DateLabel(promise.DueDay!.Value)}. No delivery has happened.",
                "Okay, a sunflower could look really good in that shot. Just tell me if you need more time.");
        if (action == "decline" && promise.Decline(Today)) return new("Not this time.", "The farmer declined the sunflower request. No promise or penalty was created.", "That's fine. I can work on something else.");
        if (action == "extend" && promise.Extend(Today)) return new("I need a little more time.", $"Haley's sunflower deadline was extended to {AbigailDeliveryQuest.DateLabel(promise.DueDay!.Value)}. Nothing was delivered.", "Okay. Thanks for actually telling me.");
        if (action == "abandon" && promise.Abandon(Today)) return new("I can't follow through on the sunflower.", "The farmer explicitly cancelled the accepted sunflower promise. Haley can be disappointed about this specific promise; there is no romantic betrayal or relationship penalty.", "I wish you'd told me sooner, but okay. At least I know now.");
        if (action.StartsWith("give:", StringComparison.Ordinal) && Sunflower() is { } item && action == "give:" + item.Quality
            && promise.Complete(item.QualifiedItemId, item.DisplayName, Today))
        {
            Game1.player.Items.Reduce(item, 1);
            return new("Here's the sunflower I promised.", "The farmer actually handed Haley one sunflower for her still life. It was removed from inventory. No photograph has been taken and no romantic reward was given.",
                promise.WasLate ? "You brought it. A little late, but... thanks for following through." : "Oh, you remembered! This will work really well. Thank you.");
        }
        return null;
    }
    private void EnsurePromiseQuest()
    {
        if (!Ready) return;
        var promise = state!.Promise; bool needed = promise.Status is "active" or "overdue";
        var existing = Game1.player.questLog.Where(q => q.id.Value == PromiseQuestId).ToArray();
        foreach (var extra in existing.Skip(needed ? 1 : 0)) Game1.player.questLog.Remove(extra);
        if (!needed) return;
        var quest = existing.FirstOrDefault();
        if (quest == null)
        {
            quest = new Quest(); quest.id.Value = PromiseQuestId; quest.questType.Value = Quest.type_basic;
            quest.accepted.Value = true; quest.showNew.Value = true; quest.canBeCancelled.Value = false; Game1.player.questLog.Add(quest);
        }
        quest.questTitle = "A sunflower for Haley";
        quest.questDescription = $"You promised Haley one sunflower for a photography still life by {AbigailDeliveryQuest.DateLabel(promise.DueDay!.Value)}. Talk to her to hand it over, ask for more time, or cancel. This is a personal promise, not a gift or paid quest.";
        quest.currentObjective = promise.Status == "overdue" ? "The promise is overdue. Talk to Haley and follow through or explain." : "Bring one sunflower and explicitly give it to Haley.";
    }
}
