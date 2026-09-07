using System.Security.Cryptography;
using System.Text;

namespace SolaceWeather.Core;

public sealed class FashionCatalog
{
    public int SchemaVersion { get; set; } = 1;
    public Dictionary<string, FashionDefinition> Items { get; set; } = new(StringComparer.Ordinal);
    public bool IsValid() => SchemaVersion == 1 && Items != null && Items.Count <= 10000
        && Items.All(p => p.Key.Length is > 0 and <= 160 && p.Value?.IsValid() == true);
}
public sealed class FashionDefinition
{
    public string Slot { get; set; } = "shirt";
    public int FashionValue { get; set; } = 50;
    public string[] StyleTags { get; set; } = Array.Empty<string>();
    public string[] PaletteTags { get; set; } = Array.Empty<string>();
    public int Formality { get; set; }
    public int Practicality { get; set; }
    public int Statement { get; set; }
    public bool IsValid() => Slot is "shirt" or "pants" or "hat" or "boots" && FashionValue is >= 0 and <= 100
        && Formality is >= 0 and <= 3 && Practicality is >= 0 and <= 3 && Statement is >= 0 and <= 3
        && ValidTags(StyleTags) && ValidTags(PaletteTags);
    private static bool ValidTags(string[]? tags) => tags != null && tags.Length <= 12
        && tags.All(t => !string.IsNullOrWhiteSpace(t) && t.Length <= 40 && t.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'));
}
public sealed record FashionPiece(string Slot, string ItemId, string Name, string Dye, bool Prismatic, FashionDefinition? Definition);
public sealed class NpcFashionTaste
{
    public int Standard { get; set; } = 50;
    public int Importance { get; set; } = 1;
    public string[] PreferredTags { get; set; } = Array.Empty<string>();
    public int Formality { get; set; } = 1;
    public int Practicality { get; set; } = 1;
    public int Statement { get; set; } = 1;
}
public sealed record FashionAssessment(int Score, string Reaction, string[] KnownStyleTags, bool HasKnownStyle);
public static class FashionRules
{
    public static string Fingerprint(IEnumerable<FashionPiece> pieces) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("|", pieces.OrderBy(p => p.Slot, StringComparer.Ordinal).ThenBy(p => p.ItemId, StringComparer.Ordinal)
            .Select(p => $"{p.Slot}:{p.ItemId}:{(p.Prismatic ? "prismatic" : p.Dye)}")))));
    public static FashionAssessment Evaluate(IEnumerable<FashionPiece> source, NpcFashionTaste taste)
    {
        var pieces = source.Take(4).ToArray();
        var known = pieces.Select(p => p.Definition).Where(d => d?.IsValid() == true).Cast<FashionDefinition>().ToArray();
        double average = pieces.Length == 0 ? 50 : pieces.Average(p => p.Definition?.IsValid() == true ? p.Definition.FashionValue : 50);
        var tags = known.SelectMany(d => d.StyleTags).Distinct(StringComparer.Ordinal).OrderBy(t => t, StringComparer.Ordinal).ToArray();
        double fit = known.Length == 0 ? 0 :
            (tags.Intersect(taste.PreferredTags, StringComparer.Ordinal).Any() ? 8 : 0)
            + 3 - Math.Abs(known.Average(d => d.Formality) - taste.Formality) * 2
            + 3 - Math.Abs(known.Average(d => d.Practicality) - taste.Practicality) * 2
            + 3 - Math.Abs(known.Average(d => d.Statement) - taste.Statement) * 2;
        int score = (int)Math.Round(Math.Clamp(average + Math.Clamp(fit, -15, 15), 0, 100));
        string reaction = known.Length == 0 || taste.Importance == 0 ? "neutral"
            : score >= taste.Standard + 8 ? "admiring" : score < taste.Standard - 12 ? "not-my-style" : "neutral";
        return new(score, reaction, tags, known.Length > 0);
    }
}

public sealed class OutfitObservation
{
    public string Fingerprint { get; set; } = "";
    public string Description { get; set; } = "";
    public int SeenDay { get; set; }
    public string LastCommentFingerprint { get; set; } = "";
    public int LastCommentDay { get; set; } = -100;
}
public sealed class FashionMemoryState
{
    public int Version { get; set; } = 1;
    public long FarmerId { get; set; }
    public Dictionary<string, OutfitObservation> Observations { get; set; } = new(StringComparer.Ordinal);
    public int LastAnyCommentDay { get; set; } = -100;
    public bool IsValid(long farmer) => Version == 1 && FarmerId == farmer && Observations != null && Observations.Count <= 256
        && LastAnyCommentDay >= -100 && Observations.All(p => p.Key.Length is > 0 and <= 100 && p.Value != null
            && p.Value.Fingerprint.Length is > 0 and <= 160 && p.Value.Description.Length <= 1000 && p.Value.SeenDay >= 0
            && p.Value.LastCommentFingerprint.Length <= 160 && p.Value.LastCommentDay >= -100);
    public OutfitObservation? LastSeen(string npc) => Observations.GetValueOrDefault(npc);
    public void Observe(string npc, string fingerprint, string description, int day)
    {
        if (npc.Length is < 1 or > 100 || fingerprint.Length is < 1 or > 160 || description.Length > 1000 || day < 0
            || !Observations.ContainsKey(npc) && Observations.Count >= 256) return;
        if (!Observations.TryGetValue(npc, out var entry)) Observations[npc] = entry = new();
        entry.Fingerprint = fingerprint; entry.Description = description; entry.SeenDay = day;
    }
    public bool CanComment(string npc, string fingerprint, int day, int cooldown) => LastSeen(npc) is { } seen
        && seen.Fingerprint == fingerprint && seen.LastCommentFingerprint != fingerprint && day - seen.LastCommentDay >= Math.Max(1, cooldown)
        && day > LastAnyCommentDay;
    public void MarkComment(string npc, string fingerprint, int day)
    {
        if (LastSeen(npc) is not { } seen || seen.Fingerprint != fingerprint || day < seen.SeenDay) return;
        seen.LastCommentFingerprint = fingerprint; seen.LastCommentDay = LastAnyCommentDay = day;
    }
}
