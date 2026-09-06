namespace SolaceWeather.Core;

/// <summary>Observed game facts only; never model-generated claims.</summary>
public sealed class AbigailMemory
{
    public int Version { get; set; } = 1;
    public List<AbigailDay> Days { get; set; } = new();
    public List<AbigailExchange> Exchanges { get; set; } = new();
    public PersonalMemoryStore Personal { get; set; } = new();
    public DeliveryRequest Delivery { get; set; } = new();
    public PromiseLedger Promises { get; set; } = new();
    public RelationshipTreeState Tree { get; set; } = new();

    public SharedExperienceStore Experiences { get; set; } = new();

    public bool IsValid() => Experiences != null && Experiences.IsValid() && Version == 1 && Tree != null && Tree.IsValid() && Promises != null && Promises.IsValid() && Delivery != null && Delivery.IsValid() && Personal != null && Personal.IsValid() && Exchanges != null && Exchanges.Count <= 32
        && Exchanges.All(e => e != null && e.Day >= 0 && e.Farmer != null && e.Farmer.Length <= 500 && e.Reply != null && e.Reply.Length <= 1200)
        && Days != null && Days.Count <= 112
        && Days.All(d => d != null && d.Day >= 0 && d.Gifts >= 0 && d.SpokenLines != null
            && d.SpokenLines.Count <= 8 && d.SpokenLines.All(s => s != null && s.Length <= 2000))
        && Days.Select(d => d.Day).Distinct().Count() == Days.Count;

    public void Observe(int day, bool talked, int gifts)
    {
        if (!IsValid()) throw new InvalidOperationException("Unsupported Abigail memory state.");
        if (day < 0 || gifts < 0) throw new ArgumentOutOfRangeException(nameof(day));
        if (!talked && gifts == 0) return;
        var entry = Days.SingleOrDefault(d => d.Day == day);
        if (entry == null) Days.Add(entry = new AbigailDay { Day = day });
        entry.Talked |= talked;
        entry.Gifts = Math.Max(entry.Gifts, gifts);
        Days = Days.OrderBy(d => d.Day).TakeLast(112).ToList();
    }

    public void RememberLine(int day, string line)
    {
        if (!IsValid()) throw new InvalidOperationException("Unsupported Abigail memory state.");
        if (day < 0) throw new ArgumentOutOfRangeException(nameof(day));
        if (string.IsNullOrWhiteSpace(line)) return;
        line = line.Trim();
        if (line.Length > 2000) line = line[..2000];
        var entry = Days.SingleOrDefault(d => d.Day == day);
        if (entry == null) Days.Add(entry = new AbigailDay { Day = day });
        if (!entry.SpokenLines.Contains(line)) entry.SpokenLines.Add(line);
        entry.SpokenLines = entry.SpokenLines.TakeLast(8).ToList();
        Days = Days.OrderBy(d => d.Day).TakeLast(112).ToList();
    }
}

public sealed class AbigailExchange
{
    public int Day { get; set; }
    public string Farmer { get; set; } = "";
    public string Reply { get; set; } = "";
}

public sealed class AbigailDay
{
    public int Day { get; set; }
    public bool Talked { get; set; }
    public int Gifts { get; set; }
    public List<string> SpokenLines { get; set; } = new();
}
