namespace SolaceWeather.Core;

public sealed record ChatterFact(string Id, string Fact);
public sealed record ChatterContext(string First, string Second, int Day, string Season, int DayOfMonth, string Location, ChatterFact[] Facts);
public sealed record ChatterLine(string Speaker, string Text);
public sealed record ChatterReply(string FactId, ChatterLine[] Lines);
public sealed record HeardChatter(int Day, string Location, string Topic, ChatterLine[] Lines);

/// <summary>Public, attributed speech witnessed by this farmer; never proof of additional events.</summary>
public sealed class TownChatterState
{
    public int Version { get; set; } = 1;
    public long FarmerId { get; set; }
    public int AttemptDay { get; set; } = -1;
    public int LastMinute { get; set; } = -1;
    public List<string> AttemptedPairs { get; set; } = new();
    public List<HeardChatter> Heard { get; set; } = new();
    public static bool ValidLine(ChatterLine? line) => line != null && RomanceRules.IsCandidate(line.Speaker)
        && PhoneState.ValidText(line.Text, 120) && !line.Text.Any(c => "#$^@[]<>".Contains(c));
    private static string Pair(string a, string b) => string.Join("|", new[] { a, b }.OrderBy(n => n, StringComparer.Ordinal));
    public bool IsValid(long farmer) => Version == 1 && FarmerId == farmer && AttemptDay >= -1 && LastMinute is >= -1 and <= 1560
        && AttemptedPairs != null && AttemptedPairs.Count <= 3 && AttemptedPairs.Distinct().Count() == AttemptedPairs.Count
        && AttemptedPairs.All(p => p != null && p.Split('|') is var names && names.Length == 2 && names[0] != names[1] && names.All(RomanceRules.IsCandidate))
        && Heard != null && Heard.Count <= 24 && Heard.All(h => h != null && h.Day >= 0 && h.Location == "Town"
            && h.Topic != null && h.Topic.Length is > 0 and <= 80 && h.Lines != null && h.Lines.Length == 2
            && h.Lines.All(ValidLine) && h.Lines[0].Speaker != h.Lines[1].Speaker);
    public bool TryAttempt(string first, string second, int day, int minute)
    {
        if (!IsValid(FarmerId) || !RomanceRules.IsCandidate(first) || !RomanceRules.IsCandidate(second) || first == second
            || day < 0 || day < AttemptDay || minute is < 0 or > 1560) return false;
        if (AttemptDay != day) { AttemptDay = day; LastMinute = -1; AttemptedPairs.Clear(); }
        string pair = Pair(first, second);
        if (AttemptedPairs.Count >= 3 || AttemptedPairs.Contains(pair) || LastMinute >= 0 && minute - LastMinute < 120) return false;
        AttemptedPairs.Add(pair); LastMinute = minute; return true;
    }
    public bool Record(int day, string location, string topic, ChatterLine[] lines)
    {
        if (!IsValid(FarmerId) || day < 0 || location != "Town" || string.IsNullOrEmpty(topic) || topic.Length > 80
            || lines == null || lines.Length != 2 || !lines.All(ValidLine) || lines[0].Speaker == lines[1].Speaker) return false;
        Heard.Add(new(day, location, topic, lines.ToArray())); Heard = Heard.TakeLast(24).ToList(); return true;
    }
    public HeardChatter[] ForCharacter(string name) => Heard.Where(h => h.Lines.Any(l => l.Speaker == name)).TakeLast(4).ToArray();
}
