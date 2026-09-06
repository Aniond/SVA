using System.Text.RegularExpressions;
namespace SolaceWeather.Core;

public sealed class SharedExperience
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Fact { get; set; } = "";
    public string Topics { get; set; } = "";
    public int FirstDay { get; set; }
    public int LastDay { get; set; }
    public int Occurrences { get; set; }
    public int? LastRecalledDay { get; set; }
    // These are attributed speech, never evidence of additional game events.
    public List<AbigailExchange> Conversations { get; set; } = new();
}

/// <summary>Permanent authored experience groups, with bounded attributed conversation and repeat history.</summary>
public sealed class SharedExperienceStore
{
    public int Version { get; set; } = 1;
    public List<SharedExperience> Entries { get; set; } = new();
    public bool IsValid() => Version == 1 && Entries != null && Entries.Count <= 128
        && Entries.All(e => e != null && !string.IsNullOrWhiteSpace(e.Id) && e.Id.Length <= 100
            && e.Kind != null && e.Kind.Length <= 50 && !string.IsNullOrWhiteSpace(e.Fact) && e.Fact.Length <= 1600
            && e.Topics != null && e.Topics.Length <= 300 && e.FirstDay >= 0 && e.LastDay >= e.FirstDay && e.Occurrences > 0
            && e.LastRecalledDay is not < 0 && e.Conversations != null && e.Conversations.Count <= 4
            && e.Conversations.All(c => c != null && c.Day >= e.FirstDay && c.Farmer != null && c.Farmer.Length <= 500 && c.Reply != null && c.Reply.Length <= 1200))
        && Entries.Select(e => e.Id).Distinct().Count() == Entries.Count;

    public bool Record(string id, string kind, int day, string fact, string topics)
    {
        if (!IsValid() || day < 0 || string.IsNullOrWhiteSpace(id) || id.Length > 100 || kind == null || kind.Length > 50
            || string.IsNullOrWhiteSpace(fact) || fact.Length > 1600 || topics == null || topics.Length > 300) return false;
        var entry = Entries.FirstOrDefault(e => e.Id == id);
        if (entry != null && entry.LastDay >= day) return false;
        if (entry == null)
        {
            if (Entries.Count == 128) return false;
            Entries.Add(entry = new SharedExperience { Id = id, FirstDay = day });
        }
        entry.Kind = kind; entry.Fact = fact; entry.Topics = topics; entry.LastDay = day;
        entry.Occurrences = (int)Math.Min(int.MaxValue, (long)entry.Occurrences + 1);
        return true;
    }
    public bool Reflect(string id, int day, string farmer, string reply)
    {
        if (!IsValid() || farmer == null || farmer.Length > 500 || reply == null || reply.Length > 1200) return false;
        var entry = Entries.FirstOrDefault(e => e.Id == id);
        if (entry == null || day < entry.LastDay || entry.Conversations.Any(c => c.Day == day && c.Farmer == farmer && c.Reply == reply)) return false;
        entry.Conversations.Add(new AbigailExchange { Day = day, Farmer = farmer, Reply = reply });
        entry.Conversations = entry.Conversations.TakeLast(4).ToList();
        entry.LastRecalledDay = day;
        return true;
    }
    public SharedExperience[] Select(string message, int day)
    {
        if (!IsValid() || day < 0) return Array.Empty<SharedExperience>();
        var words = Regex.Matches((message ?? "").ToLowerInvariant(), @"[\p{L}]{3,}").Select(m => m.Value).ToHashSet();
        int Relevance(SharedExperience e) => Regex.Matches((e.Topics + " " + e.Fact + " " + string.Join(" ", e.Conversations.Select(c => c.Farmer))).ToLowerInvariant(), @"[\p{L}]{3,}")
            .Select(m => m.Value).Distinct().Count(words.Contains);
        return Entries.Where(e => e.LastDay <= day).OrderByDescending(Relevance)
            .ThenBy(e => e.LastRecalledDay != null && day - e.LastRecalledDay < 3)
            .ThenByDescending(e => e.LastDay).ThenBy(e => e.Id).Take(6).ToArray();
    }
    public void Observe(PromiseLedger ledger, RelationshipTreeState tree)
    {
        if (!IsValid() || !ledger.IsValid() || !tree.IsValid()) return;
        foreach (var promise in ledger.Records)
            foreach (var moment in promise.History.Where(h => h.Kind is "accepted" or "extended" or "late" or "abandoned" or "completed"))
                Record("promise:" + promise.Id + ":" + moment.Kind, "promise", moment.Day,
                    PromiseLedger.Definition(promise.Id)!.Name + ": " + moment.Text,
                    promise.Id == "iron" ? "adventure mines promise trust repair independence" : promise.Id == "quartz" ? "quartz mineral curiosity promise trust repair" : "fish favor promise trust");
        foreach (var study in tree.Studies)
            Record("study:" + study.ItemId, "curiosity", study.Day, "Abigail examined " + MineralName(study.ItemId) + " with the farmer. The farmer kept it; this was a study, not a gift.", "mineral discovery shiny curiosity " + MineralName(study.ItemId));
        if (tree.LastFluteDay is int flute) Record("flute", "music", flute, "Abigail played a short flute break for the farmer; twenty minutes passed and the game restored up to thirty energy.", "flute music rest tired comfort quiet");
        // Old cooldowns prove a transaction, but do not establish which mineral or approach was used.
        if (tree.LastExchangeDay is int exchange && !Entries.Any(e => e.Id == "exchange" && e.LastDay >= exchange))
            Record("exchange", "curiosity", exchange, "Two spare studied minerals were exchanged for one Geode. The old record does not identify the mineral.", "mineral exchange geode curiosity");
        if (tree.LastKitDay is int kit && !Entries.Any(e => e.Id == "kit" && e.LastDay >= kit))
            Record("kit", "adventure", kit, "Abigail supplied an adventure kit. The old cooldown record does not identify its contents; the current approach does not prove the past kit type.", "mines adventure supplies preparation");
    }
    public static string MineralName(string id) => id switch { "(O)80" => "Quartz", "(O)66" => "Amethyst", "(O)86" => "Earth Crystal", "(O)84" => "Frozen Tear", "(O)82" => "Fire Quartz", _ => id };
}
