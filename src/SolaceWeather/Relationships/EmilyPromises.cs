using SolaceWeather.Controls;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Quests;

namespace SolaceWeather.Relationships;

internal sealed partial class EmilyLifeService
{
    private const string PromiseQuestId = "David.SolaceWeather/EmilyCloth";
    private void ObservePromise()
    {
        if (!Ready) return;
        foreach (var item in Game1.player.Items.Where(i => i != null)) state!.Promise.Observe(item.QualifiedItemId);
        state!.Promise.AdvanceDay(Today);
    }
    private static StardewValley.Object? Cloth() => Game1.player.Items.OfType<StardewValley.Object>()
        .Where(i => i.QualifiedItemId == EmilyClothPromise.ItemId && i.Stack > 0 && !i.questItem.Value && !i.modData.ContainsKey(QuickStack.ProtectedKey))
        .OrderBy(i => i.Quality).FirstOrDefault();
    private string PromiseStamp => $"emily:promise:{state!.Promise.Status}:{state.Promise.OfferedDay}:{state.Promise.DueDay}:";
    private QuestChoice[] PromiseChoices()
    {
        var promise = state!.Promise;
        if (promise.Status == "offered") return new[] {
            new QuestChoice(PromiseStamp + "accept1", "I'll bring a cloth tomorrow."),
            new QuestChoice(PromiseStamp + "accept3", "Give me three days for the cloth."),
            new QuestChoice(PromiseStamp + "decline", "Not this time.") };
        if (promise.Status is not ("active" or "overdue")) return Array.Empty<QuestChoice>();
        var choices = new List<QuestChoice>();
        if (Cloth() is { } item) choices.Add(new(PromiseStamp + "give:" + item.Quality, "Give Emily one " + item.DisplayName));
        if (promise.Status == "active" && !promise.Extended && Today <= promise.DueDay) choices.Add(new(PromiseStamp + "extend", "Could I have two more days?"));
        choices.Add(new(PromiseStamp + "abandon", "I can't finish this promise."));
        return choices.ToArray();
    }
    private QuestActionResult? ApplyPromise(string key)
    {
        if (!key.StartsWith(PromiseStamp, StringComparison.Ordinal)) return null;
        string action = key[PromiseStamp.Length..]; var promise = state!.Promise;
        if (action is "accept1" or "accept3" && promise.Accept(Today, action == "accept1" ? 1 : 3))
            return new("I'll bring you the cloth.", $"The farmer explicitly promised one cloth for Emily's sewing work by {AbigailDeliveryQuest.DateLabel(promise.DueDay!.Value)}. No delivery has happened.",
                "Thank you! Fabric is full of possibilities. Just let me know if you need more time.");
        if (action == "decline" && promise.Decline(Today)) return new("Not this time.", "The farmer declined the cloth request. No promise or penalty was created.", "Of course. There are plenty of ways to be creative together.");
        if (action == "extend" && promise.Extend(Today)) return new("I need a little more time.", $"Emily's cloth deadline was extended to {AbigailDeliveryQuest.DateLabel(promise.DueDay!.Value)}. Nothing was delivered.", "That's all right. I appreciate you telling me where things stand.");
        if (action == "abandon" && promise.Abandon(Today)) return new("I can't follow through on the cloth.", "The farmer explicitly cancelled the accepted cloth promise. Emily can be disappointed about this specific promise; there is no romantic betrayal or relationship penalty.", "I was looking forward to working with it, but I would rather hear the truth. We can leave it there.");
        if (action.StartsWith("give:", StringComparison.Ordinal) && Cloth() is { } item && action == "give:" + item.Quality
            && promise.Complete(item.QualifiedItemId, item.DisplayName, Today))
        {
            Game1.player.Items.Reduce(item, 1);
            return new("Here's the cloth I promised.", "The farmer actually handed Emily one Cloth for her sewing work. It was removed from inventory. No garment has been made and no romantic reward was given.",
                promise.WasLate ? "You followed through. Thank you. Next time, a little warning would help me plan." : "You remembered! Thank you. I can already imagine a few things we could explore with this.");
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
        quest.questTitle = "Cloth for Emily";
        quest.questDescription = $"You promised Emily one cloth for a sewing work by {AbigailDeliveryQuest.DateLabel(promise.DueDay!.Value)}. Talk to her to hand it over, ask for more time, or cancel. This is a personal promise, not a gift or paid quest.";
        quest.currentObjective = promise.Status == "overdue" ? "The promise is overdue. Talk to Emily and follow through or explain." : "Bring one cloth and explicitly give it to Emily.";
    }
}
