namespace SolaceWeather.Core;

public sealed record EmilyDesignMoment(int Day, string Mood, string Pattern);
public sealed record EmilyOutfitMoment(int Day, string Fingerprint);

public sealed class EmilyTreeState
{
    public HashSet<string> Unlocked { get; set; } = new(StringComparer.Ordinal);
    public List<EmilyDesignMoment> Designs { get; set; } = new();
    public List<EmilyOutfitMoment> Outfits { get; set; } = new();
    public bool KeptClothPromise { get; set; }
    public int VerifiedTalkDays { get; set; }
    public int FirstMovementDay { get; set; } = -1;
    public int LastMovementDay { get; set; } = -1;
    public int LastRecoveryDay { get; set; } = -100;
    public List<int> CraftDays { get; set; } = new();
    public static IReadOnlyList<TreeNodeDefinition> Definitions { get; } = new[] {
        new TreeNodeDefinition("root", "Made With Care", "Foundation", "Keep the cloth promise or finish a design session", "A creative friendship begins", Array.Empty<string>()),
        new TreeNodeDefinition("design", "A Pattern Between Us", "Color and Craft", "Finish one outfit-design session", "Arrange another session after three days", new[] { "root" }),
        new TreeNodeDefinition("style", "Wear What Feels Like You", "Your Own Expression", "Show Emily two different outfits on different days", "Ask for personal fashion advice", new[] { "root" }),
        new TreeNodeDefinition("personal", "Room To Be Yourself", "Moving Together", "Keep the cloth promise, finish a session and talk on three days", "More personal conversations grounded in shared history", new[] { "root" }),
        new TreeNodeDefinition("rhythm", "Find Your Rhythm", "Moving Together", "Finish two design sessions and unlock Room To Be Yourself", "Choose a twenty-minute movement break together", new[] { "personal" }),
        new TreeNodeDefinition("light", "Carry A Little Light", "Moving Together", "Actually complete a movement break together", "Later breaks can recover 30 energy, once every seven days", new[] { "rhythm" }),
        new TreeNodeDefinition("tailor", "Made For You", "Color and Craft", "Keep the cloth promise, finish two sessions and talk on three days", "Commission a custom fabric design and wearable garment", new[] { "design", "personal" }),
        new TreeNodeDefinition("care", "Care In Every Stitch", "Color and Craft", "Craft two garments on different days and complete a movement break", "Craft garments with bounded health or energy recovery", new[] { "tailor", "light" })
    };
    public bool IsValid() => Unlocked != null && Designs != null && Outfits != null && Unlocked.Count <= Definitions.Count
        && CraftDays != null && CraftDays.Count <= 2 && CraftDays.All(d => d >= 0) && CraftDays.Distinct().Count() == CraftDays.Count
        && (!Unlocked.Contains("tailor") || KeptClothPromise && Designs.Count == 2 && VerifiedTalkDays == 3)
        && (!Unlocked.Contains("care") || CraftDays.Count == 2 && FirstMovementDay >= 0)
        && Designs.Count <= 2 && Designs.All(d => d != null && d.Day >= 0 && d.Mood is "quiet" or "playful" && d.Pattern is "natural" or "geometric")
        && Designs.Select(d => d.Day).Distinct().Count() == Designs.Count
        && Outfits.Count <= 2 && Outfits.All(o => o != null && o.Day >= 0 && !string.IsNullOrWhiteSpace(o.Fingerprint) && o.Fingerprint.Length <= 160)
        && Outfits.Select(o => o.Day).Distinct().Count() == Outfits.Count && Outfits.Select(o => o.Fingerprint).Distinct().Count() == Outfits.Count
        && VerifiedTalkDays is >= 0 and <= 3 && FirstMovementDay >= -1 && LastMovementDay >= FirstMovementDay && LastRecoveryDay >= -100
        && Unlocked.All(id => Definitions.FirstOrDefault(d => d.Id == id) is { } d && d.ParentIds.All(Unlocked.Contains))
        && (!Unlocked.Contains("root") || KeptClothPromise || Designs.Count > 0)
        && (!Unlocked.Contains("design") || Designs.Count > 0)
        && (!Unlocked.Contains("style") || Outfits.Count == 2)
        && (!Unlocked.Contains("personal") || KeptClothPromise && Designs.Count > 0 && VerifiedTalkDays == 3)
        && (!Unlocked.Contains("rhythm") || Designs.Count == 2)
        && (!Unlocked.Contains("light") || FirstMovementDay >= 0)
        && (FirstMovementDay < 0 ? LastMovementDay == -1 : Unlocked.Contains("rhythm"))
        && (LastRecoveryDay < 0 || Unlocked.Contains("light") && LastRecoveryDay <= LastMovementDay);
    public void Refresh(bool clothKept, int talkDays)
    {
        if (!IsValid()) return;
        KeptClothPromise |= clothKept; VerifiedTalkDays = Math.Max(VerifiedTalkDays, Math.Clamp(talkDays, 0, 3));
        if (KeptClothPromise || Designs.Count > 0) Unlocked.Add("root");
        if (!Unlocked.Contains("root")) return;
        if (Designs.Count > 0) Unlocked.Add("design");
        if (Outfits.Count == 2) Unlocked.Add("style");
        if (KeptClothPromise && Designs.Count > 0 && VerifiedTalkDays == 3) Unlocked.Add("personal");
        if (Designs.Count == 2 && Unlocked.Contains("personal")) Unlocked.Add("rhythm");
        if (FirstMovementDay >= 0 && Unlocked.Contains("rhythm")) Unlocked.Add("light");
        if (Designs.Count == 2 && Unlocked.Contains("personal")) Unlocked.Add("tailor");
        if (CraftDays.Count == 2 && Unlocked.Contains("tailor") && Unlocked.Contains("light")) Unlocked.Add("care");
    }
    public void RecordCraft(int day)
    {
        if (IsValid() && Unlocked.Contains("tailor") && day >= 0 && CraftDays.Count < 2 && !CraftDays.Contains(day)) CraftDays.Add(day);
    }
    public void RecordDesign(int day, string mood, string pattern)
    {
        if (!IsValid() || day < 0 || mood is not ("quiet" or "playful") || pattern is not ("natural" or "geometric") || Designs.Count >= 2 || Designs.Any(d => d.Day == day)) return;
        Designs.Add(new(day, mood, pattern));
    }
    public void ObserveOutfit(int day, string fingerprint)
    {
        if (!IsValid() || day < 0 || string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length > 160 || Outfits.Count >= 2 || Outfits.Any(o => o.Day == day || o.Fingerprint == fingerprint)) return;
        Outfits.Add(new(day, fingerprint));
    }
    public bool RecordMovement(int day)
    {
        if (!IsValid() || !Unlocked.Contains("rhythm") || day < 0 || day <= LastMovementDay) return false;
        if (FirstMovementDay < 0) FirstMovementDay = day;
        LastMovementDay = day; return true;
    }
    public bool ClaimRecovery(int day)
    {
        if (!IsValid() || !Unlocked.Contains("light") || day != LastMovementDay || day <= FirstMovementDay || day - LastRecoveryDay < 7) return false;
        LastRecoveryDay = day; return true;
    }
}
