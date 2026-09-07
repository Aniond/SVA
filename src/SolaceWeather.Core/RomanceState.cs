namespace SolaceWeather.Core;

/// <summary>Versioned save-local evidence. Historical import never invents dates or completed activities.</summary>
public sealed class RomanceSaveState
{
    public int Version { get; set; } = 1;
    public int LatestDay { get; set; }
    public int NextIncidentSequence { get; set; } = 1;
    public Dictionary<string, int> ReporterLastDay { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, RomanceCharacterState> Characters { get; set; } = new(StringComparer.Ordinal);
    public ActivityBooking? Booking { get; set; }
    public List<RomanceIncident> Incidents { get; set; } = new();
    public List<RomanceKnowledge> Knowledge { get; set; } = new();
    public List<RomanceMemory> History { get; set; } = new();
    public bool IsValid()
    {
        if (Version != 1 || LatestDay < 0 || LatestDay > 10000000 || NextIncidentSequence < 1 || NextIncidentSequence == int.MaxValue || Characters == null || Characters.Count > 12 || Incidents == null || Incidents.Count > 512 || Knowledge == null || Knowledge.Count > 6144 || History == null || History.Count > 256 || ReporterLastDay == null || ReporterLastDay.Count > 128) return false;
        if (Characters.Any(p => !RomanceRules.IsCandidate(p.Key) || p.Value == null || !p.Value.IsValid())) return false;
        if (Incidents.Any(i => i == null || !RomanceRules.Short(i.Id) || !RomanceRules.IsCandidate(i.ActorNpc) || i.Day < 0 || i.Day > LatestDay || !RomanceRules.IsIncidentKind(i.Kind) || i.Sequence < 1 || i.Sequence >= NextIncidentSequence)) return false;
        if (Incidents.Select(i => i.Id).Distinct().Count() != Incidents.Count || Incidents.Select(i => i.Sequence).Distinct().Count() != Incidents.Count) return false;
        if (Knowledge.Any(k => k == null || !RomanceRules.Short(k.KnowerNpc) || !RomanceRules.Short(k.Source) || !RomanceRules.Short(k.OriginalEyewitness) || k.Day < 0 || k.Day > LatestDay || k.Hops is < 0 or > 2 || !Incidents.Any(i => i.Id == k.IncidentId && k.Day >= i.Day && k.Day - i.Day <= 7))) return false;
        if (Knowledge.Select(k => (k.KnowerNpc, k.IncidentId)).Distinct().Count() != Knowledge.Count) return false;
        return History.All(h => h != null && h.Day >= 0 && h.Day <= LatestDay && RomanceRules.Short(h.Npc) && RomanceRules.Short(h.Kind)) && ReporterLastDay.All(p => RomanceRules.Short(p.Key) && p.Value >= 0 && p.Value <= LatestDay) && (Booking == null || Booking.IsValid());
    }
    public RomanceJourney GetJourney(string name, int day)
    {
        if (day is < 0 or > 9999980 || !IsValid() || name == null || !Characters.TryGetValue(name, out var c)) return new("friendship", false, false, false);
        bool clear = !c.InConflict && c.SeparationUntilDay == null && c.PendingTransition == null && !c.NeedsReaffirmation;
        bool dating = c.InterestExpressed && c.FirstContactDay.HasValue && day - c.FirstContactDay.Value >= 28 && c.TalkDays >= 12 && c.CompletedActivities >= 4 && c.ActivityTypes.Count >= 2 && !c.IsDating && !c.IsMarried && !c.FriendshipOnly && clear;
        bool established = c.IsDating && c.DatingSinceDay.HasValue && day - c.DatingSinceDay.Value >= 28 && c.RomanticDates >= 4 && clear;
        bool propose = established && !c.IsEngaged && !c.IsMarried && day - c.DatingSinceDay!.Value >= 56 && c.RomanticDates >= 8 && day - (c.ConflictFreeSinceDay ?? c.DatingSinceDay.Value) >= 14;
        string stage = c.PendingTransition != null ? c.PendingTransition : c.SeparationUntilDay != null ? "separated" : c.NeedsReaffirmation ? "reaffirm" : c.InConflict ? "conflict" : c.IsMarried ? "married" : c.IsEngaged ? "engaged" : established ? "established" : c.IsDating ? "dating" : c.InterestExpressed ? "interest" : "friendship";
        string bondStage = c.IsMarried ? "married" : c.IsEngaged ? "engaged" : c.IsDating && c.DatingSinceDay.HasValue && day - c.DatingSinceDay.Value >= 28 && c.RomanticDates >= 4 ? "established couple" : c.IsDating ? "exclusive dating" : dating ? "mutual interest" : "friendship";
        return new(stage, dating, established, propose, bondStage);
    }
}
public sealed record RomanceJourney(string Stage, bool CanDate, bool Established, bool CanPropose, string BondStage = "friendship");
public sealed class RomanceCharacterState
{
    public int? FirstContactDay { get; set; }
    public int LastTalkDay { get; set; } = -1;
    public int TalkDays { get; set; }
    public int LastActivityDay { get; set; } = -1;
    public int CompletedActivities { get; set; }
    public HashSet<string> ActivityTypes { get; set; } = new(StringComparer.Ordinal);
    public int RomanticDates { get; set; }
    public bool InterestExpressed { get; set; }
    public bool FriendshipOnly { get; set; }
    public bool IsEngaged { get; set; }
    public bool IsDating { get; set; }
    public bool IsMarried { get; set; }
    public bool Exclusive { get; set; } = true;
    public bool NeedsReaffirmation { get; set; }
    public int? DatingSinceDay { get; set; }
    public int? ReaffirmedDay { get; set; }
    public bool InConflict { get; set; }
    public int? WarningDeliveredDay { get; set; }
    public int WarningIncidentSequence { get; set; }
    public int LastViolationSequence { get; set; }
    public int? LastViolationDay { get; set; }
    public int? ConflictFreeSinceDay { get; set; }
    public int? SeparationUntilDay { get; set; }
    public bool RepairAcknowledged { get; set; }
    public int RepairActivities { get; set; }
    public int LastRepairDay { get; set; } = -1;
    public int LastReportDay { get; set; } = -1;
    public int NoShows { get; set; }
    public string? PendingTransition { get; set; }
    // Remains after an authored ending; only successful repair or renewed dating clears it.
    public string PhoneBlockReason { get; set; } = "";
    public bool IsValid() => PhoneBlockReason is "" or "separated" or "ending-relationship" or "divorced" && TalkDays is >= 0 and < 10000000 && CompletedActivities is >= 0 and < 10000000 && RomanticDates >= 0 && RomanticDates <= CompletedActivities && NoShows is >= 0 and < 10000000 && RepairActivities is >= 0 and <= 2 && ActivityTypes != null && ActivityTypes.Count <= 32 && ActivityTypes.All(RomanceRules.Short) && new[] { FirstContactDay, DatingSinceDay, ReaffirmedDay, WarningDeliveredDay, LastViolationDay, ConflictFreeSinceDay, SeparationUntilDay }.All(d => d == null || d is >= 0 and <= 10000000) && new[] { LastTalkDay, LastActivityDay, LastRepairDay, LastReportDay }.All(d => d is >= -1 and <= 10000000) && WarningIncidentSequence >= 0 && LastViolationSequence >= 0 && PendingTransition is null or "breakup" or "divorce" && (!IsMarried || IsDating) && (!IsEngaged || IsDating && !IsMarried) && (!NeedsReaffirmation || IsDating) && (!SeparationUntilDay.HasValue || InConflict) && (PendingTransition == null || InConflict);
}
public sealed class ActivityBooking
{
    public string Npc { get; set; } = "";
    public string Type { get; set; } = "";
    public int Day { get; set; }
    public int StartMinute { get; set; }
    public bool Romantic { get; set; }
    public bool Repair { get; set; }
    public bool Arrived { get; set; }
    public int? ArrivedMinute { get; set; }
    public int SegmentsCompleted { get; set; }
    public bool IsValid() => RomanceRules.IsCandidate(Npc) && RomanceRules.Short(Type) && Day >= 0 && StartMinute is >= 360 and <= 1500 && SegmentsCompleted is >= 0 and <= 3 && (!Arrived || ArrivedMinute.HasValue && ArrivedMinute >= StartMinute && ArrivedMinute <= StartMinute + 30);
}
public sealed class RomanceIncident
{
    public string Id { get; set; } = "";
    public string ActorNpc { get; set; } = "";
    public int Day { get; set; }
    public string Kind { get; set; } = "";
    public int Sequence { get; set; }
}
public sealed class RomanceKnowledge
{
    public string IncidentId { get; set; } = "";
    public string KnowerNpc { get; set; } = "";
    public int Day { get; set; }
    public string Source { get; set; } = "";
    public int Hops { get; set; }
    public string OriginalEyewitness { get; set; } = "";
}
public sealed class RomanceMemory
{
    public string Npc { get; set; } = "";
    public int Day { get; set; }
    public string Kind { get; set; } = "";
}
public static class RomanceRules
{
    public static readonly string[] Candidates = { "Abigail", "Alex", "Elliott", "Emily", "Haley", "Harvey", "Leah", "Maru", "Penny", "Sam", "Sebastian", "Shane" };
    public static bool IsCandidate(string? name) => name != null && Candidates.Contains(name, StringComparer.Ordinal);
    internal static bool Short(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 96;
    public static bool IsIncidentKind(string? kind) => kind is "kiss" or "date" or "bouquet" or "proposal" or "marriage" or "pursuit" or "flirt" or "dating";
    private static bool Ready(RomanceSaveState s, int day) => s != null && s.IsValid() && day >= s.LatestDay && day is >= 0 and <= 9999980;
    private static RomanceCharacterState Character(RomanceSaveState s, string name, int day) { s.LatestDay = day; if (!s.Characters.TryGetValue(name, out var c)) s.Characters[name] = c = new() { FirstContactDay = day }; return c; }
    private static void Remember(RomanceSaveState s, string name, int day, string kind) { s.History.Add(new() { Npc = name, Day = day, Kind = kind }); if (s.History.Count > 256) s.History.RemoveAt(0); }
    public static bool Contact(RomanceSaveState s, string name, int day) { if (!Ready(s, day) || !IsCandidate(name)) return false; Character(s, name, day).FirstContactDay ??= day; return true; }
    public static bool Talk(RomanceSaveState s, string name, int day) { if (!Contact(s, name, day)) return false; var c = s.Characters[name]; if (c.LastTalkDay >= day) return false; c.LastTalkDay = day; c.TalkDays++; return true; }
    public static bool ExpressInterest(RomanceSaveState s, string name, int day) { if (!Contact(s, name, day)) return false; s.Characters[name].InterestExpressed = true; s.Characters[name].FriendshipOnly = false; Remember(s, name, day, "interest"); return true; }
    public static bool ChooseFriendship(RomanceSaveState s, string name, int day) { if (!Contact(s, name, day)) return false; var c = s.Characters[name]; if (c.IsDating || c.IsMarried) return false; c.InterestExpressed = false; c.FriendshipOnly = true; Remember(s, name, day, "friendship"); return true; }
    public static bool StartDating(RomanceSaveState s, string name, int day) { if (!Ready(s, day) || !s.GetJourney(name, day).CanDate) return false; var c = Character(s, name, day); c.IsDating = true; c.PhoneBlockReason = ""; c.DatingSinceDay = day; c.ConflictFreeSinceDay = day; Remember(s, name, day, "dating"); return true; }
    public static bool ImportPartner(RomanceSaveState s, string name, int day, bool married) { if (!Ready(s, day) || !IsCandidate(name)) return false; var c = Character(s, name, day); if (c.IsDating || c.IsMarried) return false; c.IsDating = true; c.IsMarried = married; c.NeedsReaffirmation = true; c.InterestExpressed = true; Remember(s, name, day, "imported"); return true; }
    public static bool Reaffirm(RomanceSaveState s, string name, int day) { if (!Ready(s, day) || !s.Characters.TryGetValue(name, out var c) || !c.NeedsReaffirmation) return false; s.LatestDay = day; c.NeedsReaffirmation = false; c.ReaffirmedDay = day; c.DatingSinceDay ??= day; c.ConflictFreeSinceDay = day; Remember(s, name, day, "reaffirmed"); return true; }
    public static bool RecordCompletedActivity(RomanceSaveState s, string name, int day, string type, bool romantic = false, bool repair = false)
    {
        if (!Ready(s, day) || !IsCandidate(name) || !Short(type)) return false;
        var c = Character(s, name, day); if (c.LastActivityDay >= day || c.PendingTransition != null || (romantic && (!c.IsDating || c.NeedsReaffirmation || c.InConflict || c.SeparationUntilDay != null)) || (repair && (!c.InConflict || !c.RepairAcknowledged))) return false;
        if (!c.ActivityTypes.Contains(type) && c.ActivityTypes.Count >= 32) return false;
        c.LastActivityDay = day; c.CompletedActivities++; c.ActivityTypes.Add(type); if (romantic) c.RomanticDates++;
        if (repair && c.LastRepairDay < day) { c.LastRepairDay = day; c.RepairActivities = Math.Min(2, c.RepairActivities + 1); }
        Remember(s, name, day, repair ? "repair-activity" : romantic ? "romantic-date" : "activity"); return true;
    }
    public static bool Book(RomanceSaveState s, string name, int day, int startMinute, int currentDay, int currentMinute, string type, bool romantic = false, bool repair = false)
    {
        if (!Ready(s, currentDay) || s.Booking != null || !IsCandidate(name) || !Short(type) || day < currentDay || day > currentDay + 7 || startMinute < 360 || startMinute > 1500 || (day == currentDay && startMinute <= currentMinute)) return false;
        var c = Character(s, name, currentDay); if (c.PendingTransition != null || (romantic && (!c.IsDating || c.InConflict || c.NeedsReaffirmation || c.SeparationUntilDay != null)) || (repair && (!c.InConflict || !c.RepairAcknowledged))) return false;
        s.Booking = new() { Npc = name, Type = type, Day = day, StartMinute = startMinute, Romantic = romantic, Repair = repair }; Remember(s, name, currentDay, "booked"); return true;
    }
    public static bool CancelBooking(RomanceSaveState s, int day, int minute) { if (!Ready(s, day) || s.Booking is not { } b || day > b.Day || (day == b.Day && minute >= b.StartMinute)) return false; s.LatestDay = day; s.Booking = null; Remember(s, b.Npc, day, "cancelled"); return true; }
    public static bool Arrive(RomanceSaveState s, int day, int minute) { if (!Ready(s, day) || s.Booking is not { } b || b.Arrived || b.Day != day || minute < b.StartMinute || minute > b.StartMinute + 30) return false; s.LatestDay = day; b.Arrived = true; b.ArrivedMinute = minute; Remember(s, b.Npc, day, "arrived"); return true; }
    public static bool AdvanceBooking(RomanceSaveState s, int day, int minute)
    {
        if (!Ready(s, day) || s.Booking is not { } b) return false; s.LatestDay = day;
        if (day > b.Day || (!b.Arrived && day == b.Day && minute > b.StartMinute + 30)) { s.Booking = null; if (!b.Arrived) { Character(s, b.Npc, day).NoShows++; Remember(s, b.Npc, day, "no-show"); } else Remember(s, b.Npc, day, "interrupted"); return true; }
        return false;
    }
    public static bool RecordActivitySegment(RomanceSaveState s, int day, int minute)
    {
        if (!Ready(s, day) || s.Booking is not { } b || !b.Arrived || b.Day != day || minute - b.ArrivedMinute!.Value < (b.SegmentsCompleted + 1) * 20) return false;
        s.LatestDay = day; b.SegmentsCompleted++; if (b.SegmentsCompleted < 3) return true;
        s.Booking = null; return RecordCompletedActivity(s, b.Npc, day, b.Type, b.Romantic, b.Repair);
    }
    public static bool GameCancelBooking(RomanceSaveState s, int day)
    {
        if (!Ready(s, day) || s.Booking is not { } b) return false; s.LatestDay = day; s.Booking = null; Remember(s, b.Npc, day, "interrupted"); return true;
    }
    public static bool RecordIncident(RomanceSaveState s, string id, string actorNpc, int day, string kind) { if (!Ready(s, day) || !Short(id) || !IsCandidate(actorNpc) || !IsIncidentKind(kind) || s.Incidents.Count >= 512 || s.Incidents.Any(i => i.Id == id)) return false; s.LatestDay = day; s.Incidents.Add(new() { Id = id, ActorNpc = actorNpc, Day = day, Kind = kind, Sequence = s.NextIncidentSequence++ }); Remember(s, actorNpc, day, kind); return true; }
    public static bool LearnIncident(RomanceSaveState s, string incidentId, string knowerNpc, int day, string source, int hops = 0, string? originalEyewitness = null)
    {
        if (!Ready(s, day) || !Short(knowerNpc) || !Short(source) || hops < 0 || hops > 2 || s.Knowledge.Count >= 6144 || s.Knowledge.Any(k => k.IncidentId == incidentId && k.KnowerNpc == knowerNpc)) return false;
        var i = s.Incidents.FirstOrDefault(i => i.Id == incidentId); if (i == null || day < i.Day || day - i.Day > 7) return false;
        string witness = originalEyewitness ?? (hops == 0 ? knowerNpc : source); if (!Short(witness)) return false;
        s.LatestDay = day; s.Knowledge.Add(new() { IncidentId = incidentId, KnowerNpc = knowerNpc, Day = day, Source = source, Hops = hops, OriginalEyewitness = witness }); if (!IsCandidate(knowerNpc)) return true; var c = Character(s, knowerNpc, day);
        if (i.ActorNpc == knowerNpc || !c.Exclusive || !c.IsDating || c.NeedsReaffirmation || c.PendingTransition != null || i.Day < (c.ReaffirmedDay ?? c.DatingSinceDay ?? day)) return true;
        c.InConflict = true; c.LastViolationDay = day; c.LastViolationSequence = i.Sequence; c.ConflictFreeSinceDay = null; c.RepairAcknowledged = false; c.RepairActivities = 0;
        if (c.WarningDeliveredDay.HasValue && i.Day >= c.WarningDeliveredDay.Value && i.Sequence > c.WarningIncidentSequence && c.SeparationUntilDay == null) { c.SeparationUntilDay = day + 14; c.PhoneBlockReason = "separated"; }
        Remember(s, knowerNpc, day, c.SeparationUntilDay.HasValue ? "separation" : "conflict"); return true;
    }
    public static bool DeliverWarning(RomanceSaveState s, string name, int day) { if (!Ready(s, day) || !s.Characters.TryGetValue(name, out var c) || !c.InConflict || c.WarningDeliveredDay.HasValue) return false; s.LatestDay = day; c.WarningDeliveredDay = day; c.WarningIncidentSequence = s.NextIncidentSequence - 1; Remember(s, name, day, "warning-delivered"); return true; }
    public static bool AcknowledgeConflict(RomanceSaveState s, string name, int day) { if (!Ready(s, day) || !s.Characters.TryGetValue(name, out var c) || !c.InConflict) return false; s.LatestDay = day; c.RepairAcknowledged = true; Remember(s, name, day, "acknowledged"); return true; }
    public static bool SendReport(RomanceSaveState s, string sender, string receiver, string incidentId, int day)
    {
        if (!Ready(s, day) || !Short(sender) || s.ReporterLastDay.TryGetValue(sender, out var reportDay) && reportDay >= day || sender == receiver || s.ReporterLastDay.Count >= 128 && !s.ReporterLastDay.ContainsKey(sender)) return false;
        var k = s.Knowledge.FirstOrDefault(k => k.KnowerNpc == sender && k.IncidentId == incidentId); if (k == null || k.Hops >= 2) return false;
        if (!LearnIncident(s, incidentId, receiver, day, sender, k.Hops + 1, k.OriginalEyewitness)) return false; s.ReporterLastDay[sender] = day; return true;
    }
    public static bool AdvanceDay(RomanceSaveState s, int day)
    {
        if (!Ready(s, day)) return false; s.LatestDay = day;
        s.Incidents.RemoveAll(i => day - i.Day > 7); s.Knowledge.RemoveAll(k => !s.Incidents.Any(i => i.Id == k.IncidentId));
        foreach (var p in s.Characters)
        {
            var c = p.Value; if (!c.InConflict || c.PendingTransition != null) continue; bool repaired = c.RepairAcknowledged && c.RepairActivities >= 2 && c.LastViolationDay.HasValue && day - c.LastViolationDay.Value >= 7;
            if (c.SeparationUntilDay.HasValue && day < c.SeparationUntilDay.Value) continue;
            if (repaired) { c.PhoneBlockReason = ""; c.InConflict = false; c.SeparationUntilDay = null; c.WarningDeliveredDay = null; c.WarningIncidentSequence = 0; c.ConflictFreeSinceDay = day; c.RepairAcknowledged = false; c.RepairActivities = 0; Remember(s, p.Key, day, "repaired"); }
            else if (c.SeparationUntilDay.HasValue) { c.PendingTransition = c.IsMarried ? "divorce" : "breakup"; c.PhoneBlockReason = "ending-relationship"; c.SeparationUntilDay = null; Remember(s, p.Key, day, c.PendingTransition); }
        }
        return true;
    }
    public static bool CompleteTransition(RomanceSaveState s, string name, int day) { if (!Ready(s, day) || !s.Characters.TryGetValue(name, out var c) || c.PendingTransition == null) return false; s.LatestDay = day; c.PhoneBlockReason = c.PendingTransition == "divorce" ? "divorced" : "ending-relationship"; c.IsDating = false; c.IsMarried = false; c.IsEngaged = false; c.FirstContactDay = day; c.TalkDays = 0; c.LastTalkDay = day; c.CompletedActivities = 0; c.RomanticDates = 0; c.ActivityTypes.Clear(); c.LastActivityDay = day; c.InterestExpressed = false; c.PendingTransition = null; c.InConflict = false; c.SeparationUntilDay = null; c.NeedsReaffirmation = false; c.ReaffirmedDay = null; c.WarningDeliveredDay = null; c.WarningIncidentSequence = 0; c.LastViolationDay = null; c.LastViolationSequence = 0; c.ConflictFreeSinceDay = null; c.LastRepairDay = -1; c.Exclusive = true; c.FriendshipOnly = false; c.DatingSinceDay = null; c.RepairAcknowledged = false; c.RepairActivities = 0; Remember(s, name, day, "friendship-transition"); return true; }
}



