using System.Text.RegularExpressions;

namespace SolaceWeather.Core;

public sealed class MemoryProposal
{
    public string Topic { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Quote { get; set; } = "";
    public string Timing { get; set; } = "unspecified";
}

public sealed class PersonalDetail
{
    public string Topic { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Quote { get; set; } = "";
    public string SourceMessage { get; set; } = "";
    public int Day { get; set; }
    public int? FollowUpDay { get; set; }
    public int? AskedDay { get; set; }
}

public sealed class ActivityDay
{
    public int Day { get; set; }
    public bool RecordedFromMorning { get; set; }
    public int? FirstMineVisitTime { get; set; }
}

/// <summary>Player statements and engine observations have separate storage and write paths.</summary>
public sealed class PersonalMemoryStore
{
    public int Version { get; set; } = 1;
    public List<PersonalDetail> Details { get; set; } = new();
    public List<ActivityDay> Activities { get; set; } = new();
    public int? LastFollowUpDay { get; set; }

    public bool IsValid() => Version == 1 && Details != null && Details.Count <= 64 && Activities != null && Activities.Count <= 112
        && Details.All(d => d != null && ValidTopic(d.Topic) && ValidKind(d.Kind) && !string.IsNullOrWhiteSpace(d.Quote)
            && d.Quote.Length <= 500 && d.SourceMessage != null && d.SourceMessage.Length <= 500 && d.Day >= 0 && (d.FollowUpDay == null || d.FollowUpDay > d.Day)
            && (d.AskedDay == null || d.AskedDay >= d.Day))
        && Details.Select(d => d.Kind + ":" + d.Topic).Distinct().Count() == Details.Count
        && Activities.All(d => d != null && d.Day >= 0 && (d.FirstMineVisitTime == null || d.FirstMineVisitTime is >= 600 and <= 2600))
        && Activities.Select(d => d.Day).Distinct().Count() == Activities.Count;

    private static bool ValidTopic(string? topic) => topic != null && Regex.IsMatch(topic, "^[a-z0-9_-]{1,48}$");
    private static bool ValidKind(string kind) => kind is "preference" or "personal" or "plan" or "outcome";

    public void Apply(int day, string farmerMessage, IEnumerable<MemoryProposal> proposals)
    {
        if (day < 0 || farmerMessage.Length > 500 || !IsValid()) throw new InvalidOperationException("Invalid personal memory state.");
        foreach (var proposal in proposals.Take(3))
        {
            if (proposal == null) continue;
            string topic = (proposal.Topic ?? "").Trim().ToLowerInvariant();
            string quote = (proposal.Quote ?? "").Trim();
            if (!ValidTopic(topic) || !ValidKind(proposal.Kind) || quote.Length < 4 || quote.Length > 500
                || !farmerMessage.Contains(quote, StringComparison.OrdinalIgnoreCase)) continue;
            var old = Details.SingleOrDefault(d => d.Topic == topic && d.Kind == proposal.Kind);
            if (old?.Quote.Equals(quote, StringComparison.OrdinalIgnoreCase) == true) continue;
            if (old != null) Details.Remove(old);
            int? due = null;
            if (proposal.Kind == "plan")
            {
                // Only explicit relative dates get an elapsed-plan interpretation.
                due = proposal.Timing == "tomorrow" && Regex.IsMatch(quote, "\\btomorrow\\b", RegexOptions.IgnoreCase) ? day + 2
                    : proposal.Timing == "today" && Regex.IsMatch(quote, "\\btoday\\b", RegexOptions.IgnoreCase) ? day + 1 : day + 3;
            }
            Details.Add(new PersonalDetail { Topic = topic, Kind = proposal.Kind, Quote = quote, SourceMessage = farmerMessage, Day = day, FollowUpDay = due });
            if (proposal.Kind == "outcome")
                foreach (var plan in Details.Where(d => d.Topic == topic && d.Kind == "plan")) plan.AskedDay = day;
        }
        Details = Details.OrderBy(d => d.Day).TakeLast(64).ToList();
    }

    public PersonalDetail? FollowUp(int day)
    {
        if (LastFollowUpDay >= day) return null;
        var detail = Details.Where(d => d.Kind == "plan" && d.FollowUpDay <= day && d.AskedDay == null && day - d.Day <= 14)
            .OrderBy(d => d.FollowUpDay).FirstOrDefault();
        return detail == null ? null : new PersonalDetail { Topic = detail.Topic, Kind = detail.Kind, Quote = detail.Quote, SourceMessage = detail.SourceMessage,
            Day = detail.Day, FollowUpDay = detail.FollowUpDay, AskedDay = detail.AskedDay };
    }

    public void MarkAsked(string topic, int day)
    {
        var due = FollowUp(day);
        if (due?.Topic != topic) return;
        Details.Single(d => d.Topic == topic && d.Kind == "plan").AskedDay = day;
        LastFollowUpDay = day;
    }

    public void StartDay(int day, bool fromMorning)
    {
        if (day < 0) throw new ArgumentOutOfRangeException(nameof(day));
        if (!Activities.Any(d => d.Day == day)) Activities.Add(new ActivityDay { Day = day, RecordedFromMorning = fromMorning });
        Activities = Activities.OrderBy(d => d.Day).TakeLast(112).ToList();
    }

    public void VisitMine(int day, int time)
    {
        if (time is < 600 or > 2600) throw new ArgumentOutOfRangeException(nameof(time));
        StartDay(day, false);
        Activities.Single(d => d.Day == day).FirstMineVisitTime ??= time;
    }

    public string MineVisitStatus(int day)
    {
        var activity = Activities.SingleOrDefault(d => d.Day == day);
        return activity?.FirstMineVisitTime != null ? "visited" : activity?.RecordedFromMorning == true ? "no recorded visit" : "unknown";
    }
}

public sealed class ConversationReply
{
    public string Expression { get; set; } = "neutral";
    public string QuestRequest { get; set; } = "";
    public string Reply { get; set; } = "";
    public List<MemoryProposal> Memories { get; set; } = new();
    public string AskedTopic { get; set; } = "";
    public string RecalledExperienceId { get; set; } = "";
    public bool SpontaneousRecall { get; set; }
}


