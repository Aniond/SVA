using SolaceWeather.Core;
using StardewValley;

namespace SolaceWeather.Relationships;

internal sealed partial class EmilyLifeService
{
    internal void OpenTree()
    {
        if (Ready) Game1.activeClickableMenu = new RelationshipTreeMenu(TreeView, "Emily", new[] { "Color and Craft", "Your Own Expression", "Moving Together" });
    }
    internal RelationshipTreeView TreeView()
    {
        RefreshTree(); var tree = state?.Tree ?? new EmilyTreeState();
        var cards = EmilyTreeState.Definitions.Select(d => new TreeCardView(d.Id, d.Title, d.Branch, d.Requirement, d.Perk,
            tree.Unlocked.Contains(d.Id) ? "Unlocked" : "Not yet", d.ParentIds)).ToArray();
        string history = string.Join("\n", tree.Designs.Select(d => $"{AbigailDeliveryQuest.DateLabel(d.Day)}: a {d.Mood} mood and {d.Pattern} pattern."));
        if (tree.FirstMovementDay >= 0) history += $"\nFirst movement break: {AbigailDeliveryQuest.DateLabel(tree.FirstMovementDay)}.";
        return new(cards, "Emily loves fashion as creative expression: color, craft, movement and the freedom to feel like yourself. Her beliefs are personal, and warmth does not imply romance.",
            HasReservation ? "An outfit-design session is planned." : "No design session reserved.", history.Length > 0 ? history : "No completed design sessions yet.",
            state?.Promise.Status == "completed" ? "The cloth promise was actually fulfilled." : $"Cloth promise: {state?.Promise.Status ?? "none"}.", "Explore your own expression");
    }
    internal QuestChoice[] TreeChoices(string name)
    {
        if (!Ready || name != "Emily") return Array.Empty<QuestChoice>();
        RefreshTree(); var choices = new List<QuestChoice> { new("ui:emily-tree", "Emily's relationship tree") };
        if (state!.Tree.Unlocked.Contains("style")) choices.Add(new("emily:tree:style", "Can we talk about this outfit?"));
        if (state.Tree.Unlocked.Contains("tailor")) choices.Add(new("emily:tailoring", "Work on a custom garment with Emily"));
        if (state.Tree.Unlocked.Contains("rhythm") && state.Tree.LastMovementDay < Today)
            choices.Add(new("emily:tree:movement", "Take a movement break together (20 minutes)"));
        return choices.ToArray();
    }
    private QuestActionResult? ApplyTree(string key)
    {
        RefreshTree();
        if (key == "emily:tree:style" && state!.Tree.Unlocked.Contains("style"))
            return new("What could I try with this outfit?", "The farmer explicitly asks Emily for personal fashion advice. Use only Fashion.VisibleOutfit and known tags. Discuss color, craft or self-expression without inventing fabric, handmade origin or garment details. No clothing, stats or friendship changed.",
                "Let's start with what feels like you. A color you love can be a wonderful place to begin, even if it surprises everyone else.");
        return null;
    }
    internal bool CanTailor(bool recovery = false) { RefreshTree(); return Ready && state!.Tree.Unlocked.Contains(recovery ? "care" : "tailor"); }
    internal void RecordCraft(int day, string garment)
    {
        if (!Ready) return;
        state!.Tree.RecordCraft(day); RefreshTree();
        getMemory()!.Experiences.Record("emily:tailoring:" + day, "craft", day, "Emily and the farmer actually made " + garment + ". The farmer approved the generated fabric preview, supplied the materials, and received the wearable garment. No romantic commitment was implied.", "sewing fashion custom fabric garment craft");
    }
}
