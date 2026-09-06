namespace SolaceWeather.Core;

public sealed record TreeNodeDefinition(string Id, string Title, string Branch, string Requirement, string Perk, string[] ParentIds);
public sealed record RelationshipHelpStatus(bool Paused, int? UntilDay, string Description);
public sealed class MineralStudy
{
    public string ItemId { get; set; } = "";
    public int Day { get; set; }
}
public sealed class ApproachSwitch
{
    public string Target { get; set; } = "";
    public int StartedDay { get; set; }
    public int? PreparationDay { get; set; }
    public int? PreparationTime { get; set; }
    public HashSet<int> VerifiedDays { get; set; } = new();
}
public sealed class CoolingAttempt
{
    public int AcceptedDay { get; set; }
    public int FailureDay { get; set; }
}

/// <summary>Save-local milestones. Only explicit, verified game actions award progress or claim help.</summary>
public sealed class RelationshipTreeState
{
    public int Version { get; set; } = 1;
    public HashSet<string> Unlocked { get; set; } = new();
    // Five distinct verified days suffice for every lifetime milestone; switch proof is separate.
    public HashSet<int> MineDays { get; set; } = new();
    public List<MineralStudy> Studies { get; set; } = new();
    public string Approach { get; set; } = "none";
    public ApproachSwitch? PendingSwitch { get; set; }
    public int? CoolingUntilDay { get; set; }
    public Dictionary<string, CoolingAttempt> FailureAttempts { get; set; } = new();
    public int? LastExchangeDay { get; set; }
    public int? LastKitDay { get; set; }
    public int? LastFluteDay { get; set; }
    public bool OutstandingSwitch => IsValid() && PendingSwitch != null;

    public static IReadOnlyList<string> SupportedMinerals { get; } = Array.AsReadOnly(new[] { "(O)80", "(O)66", "(O)86", "(O)84", "(O)82" });
    public static IReadOnlyList<TreeNodeDefinition> Definitions { get; } = Array.AsReadOnly(new[] {
        new TreeNodeDefinition("root", "You Actually Came Through", "Foundation", "Complete any promise", "A shared history begins", Array.Empty<string>()),
        new TreeNodeDefinition("study", "Show Me What You Found", "Strange Little Treasures", "Complete the Quartz promise", "Study one new mineral together per day", new[] { "root" }),
        new TreeNodeDefinition("exchange", "Something Strange for Something New", "Strange Little Treasures", "Study 3 different minerals across 3 days", "Trade 2 spare pieces of the same studied mineral for 1 Geode every 7 days", new[] { "study" }),
        new TreeNodeDefinition("planning", "No More Daydreaming", "Beyond the Fence", "Complete Iron and visit the mines on 2 days", "Discuss adventure preparations", new[] { "root" }),
        new TreeNodeDefinition("personal", "No Need to Pretend", "On My Own Terms", "Reach Starting to rely on you", "More personal conversations", new[] { "root" }),
        new TreeNodeDefinition("flute", "Quiet Notes", "On My Own Terms", "Complete all 3 promises and reach Starting to rely on you", "Spend 20 minutes listening to her flute and recover 30 energy, every 7 days", new[] { "personal" }),
        new TreeNodeDefinition("fork", "Your Kind of Adventure", "Beyond the Fence", "Unlock No More Daydreaming and No Need to Pretend; visit mines on 5 days", "Choose safe or bold preparation", new[] { "planning", "personal" }),
        new TreeNodeDefinition("safe", "Better Safe Than Sorry", "Beyond the Fence", "Explicitly choose safe at the fork", "2 Field Snacks and 5 Torches; shared 7-day kit cooldown", new[] { "fork" }),
        new TreeNodeDefinition("bold", "Let's Make Some Noise", "Beyond the Fence", "Explicitly choose bold at the fork", "2 Cherry Bombs and 5 Torches; shared 7-day kit cooldown", new[] { "fork" })
    });

    public bool IsValid()
    {
        if (Version != 1 || Unlocked == null || MineDays == null || Studies == null || FailureAttempts == null
            || Unlocked.Count > Definitions.Count || MineDays.Count > 5 || MineDays.Any(d => d < 0)
            || Studies.Count > 5 || Studies.Any(s => s == null || s.Day < 0 || !SupportedMinerals.Contains(s.ItemId))
            || Studies.Select(s => s.ItemId).Distinct().Count() != Studies.Count
            || Studies.Select(s => s.Day).Distinct().Count() != Studies.Count
            || Approach is not ("none" or "safe" or "bold") || CoolingUntilDay < 0
            || LastExchangeDay < 0 || LastKitDay < 0 || LastFluteDay < 0 || FailureAttempts.Count > 2)
            return false;
        foreach (var id in Unlocked)
        {
            var node = Definitions.FirstOrDefault(d => d.Id == id);
            if (node == null || node.ParentIds.Any(p => !Unlocked.Contains(p))) return false;
        }
        if (Studies.Count > 0 && !Unlocked.Contains("study")
            || Unlocked.Contains("exchange") && (Studies.Count < 3 || Studies.Select(s => s.Day).Distinct().Count() < 3)
            || Unlocked.Contains("planning") && MineDays.Count < 2 || Unlocked.Contains("fork") && MineDays.Count < 5
            || Approach != "none" && !Unlocked.Contains(Approach)
            || Approach == "none" && (Unlocked.Contains("safe") || Unlocked.Contains("bold"))
            || LastExchangeDay != null && !Unlocked.Contains("exchange")
            || LastFluteDay != null && !Unlocked.Contains("flute") || LastKitDay != null && Approach == "none") return false;
        if (FailureAttempts.Any(p => p.Key is not ("quartz" or "iron") || p.Value == null
            || p.Value.AcceptedDay < 0 || p.Value.FailureDay < p.Value.AcceptedDay
            || CoolingUntilDay == null || CoolingUntilDay < SaturatingAdd(p.Value.FailureDay, p.Key == "iron" ? 3 : 1))) return false;
        if (PendingSwitch is { } s)
        {
            if (Approach == "none" || s.Target is not ("safe" or "bold") || s.Target == Approach || s.StartedDay < 0
                || s.VerifiedDays == null || s.VerifiedDays.Count > 2 || s.VerifiedDays.Any(d => d < s.StartedDay)
                || s.PreparationDay.HasValue != s.PreparationTime.HasValue
                || s.PreparationDay < s.StartedDay || s.PreparationTime is < 600 or > 2600
                || s.VerifiedDays.Count > 0 && (s.PreparationDay == null || s.VerifiedDays.Max() > s.PreparationDay)) return false;
        }
        return true;
    }

    public void Observe(PromiseLedger ledger, int day, IEnumerable<int> knownMineDays)
    {
        if (!IsValid() || ledger == null || !ledger.IsValid() || day < 0 || knownMineDays == null) return;
        foreach (int mineDay in knownMineDays.Where(d => d >= 0 && d <= day))
            if (MineDays.Count < 5) MineDays.Add(mineDay);
        bool Done(string id) => ledger.Records.Any(r => r.Id == id && r.Status == "completed" && r.CompletedDay <= day);
        if (ledger.Records.Any(r => r.Status == "completed" && r.CompletedDay <= day)) Unlocked.Add("root");
        if (Done("quartz")) Unlocked.Add("study");
        if (Done("iron") && MineDays.Count >= 2) Unlocked.Add("planning");
        if (ledger.Score >= 3 && Unlocked.Contains("root")) Unlocked.Add("personal");
        if (Done("fish") && Done("quartz") && Done("iron") && ledger.Score >= 3) Unlocked.Add("flute");
        if (Unlocked.Contains("planning") && Unlocked.Contains("personal") && MineDays.Count >= 5) Unlocked.Add("fork");
        foreach (var record in ledger.Records.Where(r => r.Id is "quartz" or "iron"))
        {
            int? accepted = null;
            foreach (var moment in record.History.Where(h => h.Day <= day))
            {
                if (moment.Kind == "accepted") accepted = moment.Day;
                if (moment.Kind is not ("late" or "abandoned")) continue;
                int? attemptDay = accepted ?? (record.AcceptedDay <= moment.Day ? record.AcceptedDay : null);
                if (attemptDay is not int attempt) continue;
                if (FailureAttempts.TryGetValue(record.Id, out var prior) && prior.AcceptedDay >= attempt) continue;
                FailureAttempts[record.Id] = new CoolingAttempt { AcceptedDay = attempt, FailureDay = moment.Day };
                CoolingUntilDay = Math.Max(CoolingUntilDay ?? 0, SaturatingAdd(moment.Day, record.Id == "iron" ? 3 : 1));
            }
        }
    }

    public RelationshipHelpStatus HelpStatus(PromiseLedger ledger, int day)
    {
        if (!IsValid() || ledger == null || !ledger.IsValid() || day < 0) return new(true, null, "Relationship data is unavailable.");
        if (CoolingUntilDay is int until && day < until)
        {
            var reason = FailureAttempts.OrderByDescending(p => SaturatingAdd(p.Value.FailureDay, p.Key == "iron" ? 3 : 1)).FirstOrDefault();
            return new(true, until, "Abigail needs a little time after the broken " + (reason.Key == "iron" ? "Iron Bar" : "Quartz") + " promise.");
        }
        if (ledger.Records.Any(r => r.Id == "iron" && (r.Status == "overdue" || r.Status == "active" && r.DueDay < day)))
            return new(true, null, "Resolve the overdue Iron promise before asking for practical help.");
        if (ledger.Score < 0) return new(true, null, "Rebuild trust before asking for practical help.");
        return new(false, null, "Practical help is available when its own requirements are met.");
    }

    public bool Study(string itemId, int day)
    {
        if (!IsValid() || day < 0 || !Unlocked.Contains("study") || !SupportedMinerals.Contains(itemId)
            || Studies.Any(s => s.ItemId == itemId) || Studies.Any(s => s.Day >= day)) return false;
        Studies.Add(new MineralStudy { ItemId = itemId, Day = day });
        if (Studies.Count >= 3 && Studies.Select(s => s.Day).Distinct().Count() >= 3) Unlocked.Add("exchange");
        return true;
    }

    public bool ChooseApproach(string approach, int day)
    {
        if (!IsValid() || day < 0 || Approach != "none" || !Unlocked.Contains("fork") || approach is not ("safe" or "bold")) return false;
        Approach = approach; Unlocked.Add(approach); return true;
    }
    public bool BeginSwitch(string target, int day, bool outstandingPromise)
    {
        if (!IsValid() || day < 0 || outstandingPromise || PendingSwitch != null || Approach == "none"
            || target is not ("safe" or "bold") || target == Approach) return false;
        PendingSwitch = new ApproachSwitch { Target = target, StartedDay = day }; return true;
    }
    public bool RecordPreparation(int day, int time, bool qualified)
    {
        if (!IsValid() || !qualified || PendingSwitch is not { } s || day < s.StartedDay
            || time is < 600 or > 2600 || s.PreparationDay > day || s.PreparationDay == day && s.PreparationTime >= time
            || s.VerifiedDays.Contains(day)) return false;
        s.PreparationDay = day; s.PreparationTime = time; return true;
    }
    public bool RecordMineVisit(int day, int time)
    {
        if (!IsValid() || day < 0 || time is < 600 or > 2600) return false;
        if (MineDays.Count < 5) MineDays.Add(day);
        if (PendingSwitch is { } s && s.PreparationDay == day && s.PreparationTime < time && s.VerifiedDays.Count < 2)
            s.VerifiedDays.Add(day);
        return true;
    }
    public bool FinishSwitch(int day)
    {
        if (!IsValid() || PendingSwitch is not { } s || s.VerifiedDays.Count != 2 || day < s.VerifiedDays.Max()) return false;
        Approach = s.Target; Unlocked.Add(Approach); PendingSwitch = null; return true;
    }
    public bool CancelSwitch()
    {
        if (!IsValid() || PendingSwitch == null) return false;
        PendingSwitch = null; return true;
    }
    public bool CanClaim(string service, int day)
    {
        if (!IsValid() || day < 0) return false;
        bool Ready(int? last) => last == null || (long)day - last.Value >= 7;
        return service switch {
            "exchange" => Unlocked.Contains("exchange") && Ready(LastExchangeDay),
            "kit" => Unlocked.Contains("fork") && Approach != "none" && Ready(LastKitDay),
            "flute" => Unlocked.Contains("flute") && Ready(LastFluteDay),
            _ => false
        };
    }
    public bool Claim(string service, int day)
    {
        if (!CanClaim(service, day)) return false;
        switch (service) { case "exchange": LastExchangeDay = day; break; case "kit": LastKitDay = day; break; case "flute": LastFluteDay = day; break; }
        return true;
    }
    private static int SaturatingAdd(int day, int amount) => (int)Math.Min(int.MaxValue, (long)day + amount);
}
