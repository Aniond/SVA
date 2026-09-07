namespace SolaceWeather.Core;

/// <summary>Display names only. No friendship points, consent, readiness or relationship status is changed here.</summary>
public static class RelationshipStageLabels
{
    public static string For(RomanceCharacterState? relationship, bool female, IEnumerable<string>? treeNodes)
    {
        if (relationship?.PendingTransition != null) return "Ending relationship";
        if (relationship?.SeparationUntilDay != null) return "Taking space";
        if (relationship?.IsMarried == true) return "Spouse";
        if (relationship?.IsEngaged == true) return "Engaged";
        if (relationship?.IsDating == true) return female ? "Girlfriend" : "Boyfriend";
        if (treeNodes != null)
        {
            var nodes = treeNodes.ToHashSet(StringComparer.Ordinal);
            if (nodes.Contains("personal") && nodes.Count >= 4) return "Best friend";
            if (nodes.Contains("root") && nodes.Count >= 3) return "Close friend";
            return nodes.Contains("root") ? "Friend" : "Acquaintance";
        }
        if (relationship is { TalkDays: >= 12, CompletedActivities: >= 4 }) return "Best friend";
        if (relationship is { TalkDays: >= 7, CompletedActivities: >= 1 }) return "Close friend";
        return relationship?.TalkDays >= 3 ? "Friend" : "Acquaintance";
    }
    public static string NativeFriendship(int points) => points >= 2000 ? "Best friend" : points >= 1250 ? "Close friend" : points >= 500 ? "Friend" : "Acquaintance";
}
