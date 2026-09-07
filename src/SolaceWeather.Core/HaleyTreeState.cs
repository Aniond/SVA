namespace SolaceWeather.Core;

public sealed record HaleyPhotoMoment(int Day, string Framing, string Style);
public sealed record HaleyOutfitMoment(int Day, string Fingerprint);
public sealed class HaleyTreeState
{
    public HashSet<string> Unlocked { get; set; } = new(StringComparer.Ordinal);
    public List<HaleyPhotoMoment> Photos { get; set; } = new();
    public List<HaleyOutfitMoment> Outfits { get; set; } = new();
    public string Approach { get; set; } = "none";
    public int LastRestDay { get; set; } = -100;
    public bool KeptSunflowerPromise { get; set; }
    public int VerifiedTalkDays { get; set; }
    public static IReadOnlyList<TreeNodeDefinition> Definitions { get; } = new[] {
        new TreeNodeDefinition("root", "You Showed Up", "Foundation", "Keep the sunflower promise or finish a photo walk", "A shared history begins", Array.Empty<string>()),
        new TreeNodeDefinition("photo", "An Eye for It", "Through Her Lens", "Finish one photo walk", "Arrange another photo session after three days", new[] { "root" }),
        new TreeNodeDefinition("style", "Your Own Look", "Making It Yours", "Let Haley actually see two different outfits on different days", "Ask for personal style advice", new[] { "root" }),
        new TreeNodeDefinition("personal", "Beyond First Impressions", "Time Well Spent", "Keep the sunflower promise, finish a photo walk and talk on three days", "More personal reflections on your shared history", new[] { "root" }),
        new TreeNodeDefinition("angle", "Finding Your Angle", "Through Her Lens", "Finish photo sessions on three different days", "Choose a relaxed or polished creative approach", new[] { "photo" }),
        new TreeNodeDefinition("rest", "A Little Breathing Room", "Time Well Spent", "Unlock Beyond First Impressions and finish three photo sessions", "Spend 20 quiet minutes together to recover 30 energy, once every seven days", new[] { "personal" })
    };
    public bool IsValid() => Unlocked != null && Unlocked.Count <= Definitions.Count && Photos != null && Photos.Count <= 3 && Outfits != null && Outfits.Count <= 2
        && Photos.All(p => p != null && p.Day >= 0 && p.Framing is "wide" or "detail" && p.Style is "candid" or "posed")
        && Photos.Select(p => p.Day).Distinct().Count() == Photos.Count
        && Outfits.All(o => o != null && o.Day >= 0 && !string.IsNullOrWhiteSpace(o.Fingerprint) && o.Fingerprint.Length <= 160)
        && Outfits.Select(o => o.Day).Distinct().Count() == Outfits.Count && Outfits.Select(o => o.Fingerprint).Distinct().Count() == Outfits.Count
        && Unlocked.All(id => Definitions.FirstOrDefault(d => d.Id == id) is { } definition && definition.ParentIds.All(Unlocked.Contains))
        && VerifiedTalkDays >= 0 && VerifiedTalkDays <= 3
        && (!Unlocked.Contains("root") || KeptSunflowerPromise || Photos.Count > 0)
        && (!Unlocked.Contains("photo") || Photos.Count > 0)
        && (!Unlocked.Contains("style") || Outfits.Count >= 2)
        && (!Unlocked.Contains("personal") || KeptSunflowerPromise && Photos.Count > 0 && VerifiedTalkDays >= 3)
        && (!Unlocked.Contains("angle") || Photos.Count >= 3)
        && (!Unlocked.Contains("rest") || Photos.Count >= 3)
        && Approach is "none" or "relaxed" or "polished" && (Approach == "none" || Unlocked.Contains("angle"))
        && LastRestDay >= -100 && (LastRestDay < 0 || Unlocked.Contains("rest"));
    public void RecordPhoto(int day, string framing, string style)
    {
        if (!IsValid() || day < 0 || framing is not ("wide" or "detail") || style is not ("candid" or "posed") || Photos.Any(p => p.Day == day)) return;
        if (Photos.Count < 3) Photos.Add(new(day, framing, style));
    }
    public void ObserveOutfit(int day, string fingerprint)
    {
        if (!IsValid() || day < 0 || string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length > 160 || Outfits.Count >= 2
            || Outfits.Any(o => o.Day == day || o.Fingerprint == fingerprint)) return;
        Outfits.Add(new(day, fingerprint));
    }
    public void Refresh(bool sunflowerCompleted, int talkDays)
    {
        if (!IsValid()) return;
        KeptSunflowerPromise |= sunflowerCompleted;
        VerifiedTalkDays = Math.Max(VerifiedTalkDays, Math.Clamp(talkDays, 0, 3));
        if (KeptSunflowerPromise || Photos.Count > 0) Unlocked.Add("root");
        if (!Unlocked.Contains("root")) return;
        if (Photos.Count > 0) Unlocked.Add("photo");
        if (Outfits.Count >= 2) Unlocked.Add("style");
        if (KeptSunflowerPromise && Photos.Count > 0 && VerifiedTalkDays >= 3) Unlocked.Add("personal");
        if (Photos.Count >= 3) Unlocked.Add("angle");
        if (Photos.Count >= 3 && Unlocked.Contains("personal")) Unlocked.Add("rest");
    }
    public bool ChooseApproach(string choice)
    {
        if (!IsValid() || !Unlocked.Contains("angle") || Approach != "none" || choice is not ("relaxed" or "polished")) return false;
        Approach = choice; return true;
    }
    public bool ClaimRest(int day)
    {
        if (!IsValid() || !Unlocked.Contains("rest") || day < 0 || day - LastRestDay < 7) return false;
        LastRestDay = day; return true;
    }
}
