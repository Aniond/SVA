using SolaceWeather.Core;
using StardewValley;

namespace SolaceWeather.Relationships;

internal sealed partial class HaleyLifeService
{
    internal void OpenTree()
    {
        if (Ready) Game1.activeClickableMenu = new RelationshipTreeMenu(TreeView, "Haley", new[] { "Through Her Lens", "Making It Yours", "Time Well Spent" });
    }
    internal RelationshipTreeView TreeView()
    {
        RefreshTree();
        var tree = state?.Tree ?? new HaleyTreeState();
        var cards = HaleyTreeState.Definitions.Select(d => new TreeCardView(d.Id, d.Title, d.Branch, d.Requirement, d.Perk,
            tree.Unlocked.Contains(d.Id) ? "Unlocked" : "Not yet", d.ParentIds)).ToArray();
        string photos = string.Join("\n", tree.Photos.Select(p => $"{AbigailDeliveryQuest.DateLabel(p.Day)}: {p.Framing} framing, {p.Style} photo session."));
        string promise = state?.Promise.Status switch { "completed" => "The sunflower was actually delivered.", "active" => "You have an accepted sunflower promise.", "overdue" => "The sunflower promise is overdue.", "abandoned" => "You said you could not finish the sunflower promise.", _ => "No accepted sunflower promise." };
        return new(cards, "Haley notices personal effort, reliable follow-through and a point of view of your own. Shared milestones do not automatically mean romance.",
            HasReservation ? "A photo walk is planned." : "No photo walk reserved.", photos.Length == 0 ? "No completed photo sessions yet." : photos,
            promise, tree.Approach == "none" ? "Not chosen" : tree.Approach);
    }
    internal QuestChoice[] TreeChoices(string name)
    {
        if (!Ready || name != "Haley") return Array.Empty<QuestChoice>();
        RefreshTree(); var tree = state!.Tree;
        var choices = new List<QuestChoice> { new("ui:haley-tree", "Haley's relationship tree") };
        if (tree.Unlocked.Contains("style")) choices.Add(new("haley:tree:style", "What do you think of this outfit?"));
        if (tree.Unlocked.Contains("angle") && tree.Approach == "none")
        {
            choices.Add(new("haley:tree:relaxed", "I like a relaxed, candid approach."));
            choices.Add(new("haley:tree:polished", "I like a polished, carefully composed approach."));
        }
        if (tree.Unlocked.Contains("rest") && Today - tree.LastRestDay >= 7) choices.Add(new("haley:tree:rest", "Take a quiet break together (20 minutes)"));
        return choices.ToArray();
    }
    private QuestActionResult? ApplyTree(string key)
    {
        RefreshTree(); var tree = state!.Tree;
        if (key == "haley:tree:style" && tree.Unlocked.Contains("style"))
            return new("What do you think of this outfit?", "The farmer explicitly asked Haley for style advice about the currently observed outfit. Offer a specific, subjective suggestion grounded only in Fashion.VisibleOutfit and known style tags. Never reveal hidden values or invent garment details. Advice changes no clothing or friendship.",
                "I think the best place to start is what you want the outfit to feel like. Something relaxed, or more put together?");
        string approach = key == "haley:tree:relaxed" ? "relaxed" : key == "haley:tree:polished" ? "polished" : "";
        if (approach.Length > 0 && tree.ChooseApproach(approach))
        {
            getMemory()!.Experiences.Record("haley:approach", "creative-choice", Today, "The farmer explicitly chose a " + approach + " creative approach for future photo conversations. This is a preference, not a new photo session or relationship commitment.", "photography style creative approach");
            return new("I prefer a " + approach + " approach.", "The farmer chose a " + approach + " creative direction; future dialogue can respect it. No outfit or photograph changed.", "Okay, I can work with that. It's good to know what feels like you.");
        }
        if (key == "haley:tree:rest" && tree.Unlocked.Contains("rest") && Today - tree.LastRestDay >= 7 && RestTogether())
        {
            getMemory()!.Experiences.Record("haley:rest:" + Today, "shared-time", Today, "The farmer and Haley explicitly spent twenty quiet minutes together, and the farmer recovered a little energy. There was no romantic commitment.", "quiet break rest shared time");
            return new("Let's take a little break.", "A twenty-minute quiet break actually completed. The farmer recovered up to thirty energy. Do not quote mechanics in dialogue or invent a meal, photograph or physical affection.", "That was nice. Seriously, you don't have to fill every minute with work.");
        }
        return null;
    }
    private bool RestTogether()
    {
        var npc = Game1.getCharacterFromName("Haley");
        string venue = Game1.currentLocation.Name;
        string activity = venue == "Saloon" ? "saloon-conversation" : venue == "Mountain" ? "mountain-lake" : "town-walk";
        if (!PhotoBoundaries() || HasReservation || romance.State.Characters.GetValueOrDefault("Haley")?.InConflict == true
            || venue is not ("Town" or "Mountain" or "Saloon") || npc.isSleeping.Value
            || npc.isMoving() || npc.controller != null || npc.temporaryController != null || npc.doingEndOfRouteAnimation.Value
            || Game1.player.Stamina >= Game1.player.MaxStamina || !romance.Dates.Available(npc, Today, Minute, activity)) return false;
        bool follow = npc.followSchedule, ignore = npc.ignoreScheduleToday;
        var location = npc.currentLocation; var position = npc.Position;
        try
        {
            npc.Halt(); npc.followSchedule = false; npc.ignoreScheduleToday = true;
            Game1.performTenMinuteClockUpdate();
            if (Game1.eventUp || !BesideHaley() || npc.currentLocation != location) return false;
            Game1.performTenMinuteClockUpdate();
            if (Game1.eventUp || !BesideHaley() || npc.currentLocation != location || !state!.Tree.ClaimRest(Today)) return false;
            Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + 30); return true;
        }
        finally
        {
            npc.followSchedule = follow; npc.ignoreScheduleToday = ignore;
            if (!Game1.eventUp && npc.currentLocation == location) { npc.Position = position; if (follow) npc.checkSchedule(Game1.timeOfDay); }
        }
    }
}
