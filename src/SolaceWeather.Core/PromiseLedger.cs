namespace SolaceWeather.Core;

/// <summary>Authored requests and their importance to Abigail. Trust never changes native friendship.</summary>
public sealed record PromiseDefinition(string Id, string Name, string ItemId, int Importance, string Meaning);

public sealed class PromiseMoment
{
    public int Day { get; set; }
    public string Kind { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class PromiseRecord
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "offered";
    public int OfferedDay { get; set; }
    public int? AcceptedDay { get; set; }
    public int? DueDay { get; set; }
    public int? CompletedDay { get; set; }
    public int? AbandonedDay { get; set; }
    public bool Extended { get; set; }
    public bool WasLate { get; set; }
    public bool TestItemPending { get; set; }
    public bool TestItemGranted { get; set; }
    public int Contribution { get; set; }
    public string DeliveredItemId { get; set; } = "";
    public string DeliveredItemName { get; set; } = "";
    public List<PromiseMoment> History { get; set; } = new();
}

/// <summary>Save-local, bounded promise history. Only explicit game actions accept or fulfill promises.</summary>
public sealed class PromiseLedger
{
    public int Version { get; set; } = 1;
    public List<PromiseRecord> Records { get; set; } = new();
    public HashSet<string> SeenMaterials { get; set; } = new();
    public bool LegacyMigrated { get; set; }
    public int? LastReminderDay { get; set; }

    public static IReadOnlyList<PromiseDefinition> Definitions { get; } = Array.AsReadOnly(new[] {
        new PromiseDefinition("fish", "fish", "", 1,
            "She is curious about the fish around the valley and wants to see one up close."),
        new PromiseDefinition("quartz", "Quartz", "(O)80", 2,
            "A piece of quartz feeds her curiosity about what lies beneath the valley."),
        new PromiseDefinition("iron", "Iron Bar", "(O)335", 3,
            "Iron helps her prepare for an adventure of her own. Taking that wish seriously respects her independence.")
    });

    public static PromiseDefinition? Definition(string id) => Definitions.FirstOrDefault(d => d.Id == id);

    public PromiseRecord? Outstanding => IsValid() ? Records.FirstOrDefault(IsOutstanding) : null;
    public int Score => IsValid() ? Records.Sum(r => r.Contribution) : 0;
    public string TrustDescription => Score switch {
        < 0 => "Cautious",
        <= 2 => "Still learning about you",
        <= 5 => "Starting to rely on you",
        _ => "Consistently dependable"
    };

    public bool IsValid()
    {
        if (Version != 1 || Records == null || Records.Count > 3 || SeenMaterials == null
            || SeenMaterials.Any(id => id is not ("quartz" or "iron")) || LastReminderDay < 0)
            return false;
        return Records.All(IsValidRecord)
            && Records.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() == Records.Count
            && Records.Count(IsOutstanding) <= 1;
    }

    public void MigrateLegacy(DeliveryRequest legacy)
    {
        if (!IsValid() || LegacyMigrated || legacy == null || !legacy.IsValid()) return;
        if (legacy.Status == "none" || Records.Any(r => r.Id == "fish"))
        {
            LegacyMigrated = true;
            return;
        }
        if (legacy.Status == "active" && Outstanding != null) return;
        var fish = new PromiseRecord {
            Id = "fish", Status = legacy.Status, OfferedDay = legacy.CreatedDay!.Value,
            AcceptedDay = legacy.CreatedDay, CompletedDay = legacy.CompletedDay,
            TestItemPending = legacy.TestFishPending, TestItemGranted = legacy.TestFishGranted,
            DeliveredItemId = legacy.DeliveredItemId, DeliveredItemName = legacy.DeliveredItemName,
            Contribution = legacy.Status == "completed" ? 1 : 0
        };
        AddMoment(fish, fish.OfferedDay, "accepted", "You agreed to bring her a fish, with no deadline.");
        if (fish.CompletedDay is int completed)
            AddMoment(fish, completed, "completed", $"You brought her {fish.DeliveredItemName}. She remembers that you followed through.");
        if (!IsValidRecord(fish)) return;
        Records.Add(fish);
        LegacyMigrated = true;
    }

    public void ObserveMaterials(IEnumerable<string> requestIds)
    {
        if (!IsValid() || requestIds == null) return;
        foreach (var id in requestIds)
            if (id is "quartz" or "iron") SeenMaterials.Add(id);
    }

    public bool CanOffer(string id, int day)
    {
        if (!IsValid() || day < 0 || Definition(id) == null || Outstanding != null) return false;
        if (id != "fish" && !SeenMaterials.Contains(id)) return false;
        if (id == "iron" && !Records.Any(r => r.Id != "iron" && r.Status == "completed")) return false;
        var record = Records.FirstOrDefault(r => r.Id == id);
        if (record == null) return true;
        return record.Status switch {
            "declined" => day > LastEventDay(record),
            "abandoned" => (long)day >= (long)record.AbandonedDay!.Value + 2,
            _ => false
        };
    }

    public bool Offer(string id, int day)
    {
        if (!CanOffer(id, day)) return false;
        var record = Records.FirstOrDefault(r => r.Id == id);
        if (record == null)
        {
            record = new PromiseRecord { Id = id };
            Records.Add(record);
        }
        record.Status = "offered";
        record.OfferedDay = day;
        record.AcceptedDay = null;
        record.DueDay = null;
        record.CompletedDay = null;
        record.Extended = false;
        record.TestItemPending = false;
        AddMoment(record, day, "offered", $"She asked whether you could bring her {Definition(id)!.Name}.");
        return true;
    }

    public bool Accept(string id, int day, int days, bool testItem)
    {
        var record = FindForAction(id, day);
        if (record?.Status != "offered" || (id == "fish" ? days != 0 : days is not (1 or 3))
            || (long)day + days > int.MaxValue) return false;
        record.Status = "active";
        record.AcceptedDay = day;
        record.DueDay = days == 0 ? null : day + days;
        record.TestItemPending = id == "fish" && testItem && !record.TestItemGranted;
        AddMoment(record, day, "accepted", days == 0
            ? "You agreed to bring her a fish, with no deadline."
            : $"You promised to bring her {Definition(id)!.Name} within {days} day{(days == 1 ? "" : "s")}.");
        return true;
    }

    public bool Decline(string id, int day)
    {
        var record = FindForAction(id, day);
        if (record?.Status != "offered") return false;
        record.Status = "declined";
        AddMoment(record, day, "declined", "You said you could not take this on. No new promise was made.");
        return true;
    }

    public bool Extend(string id, int day)
    {
        var record = FindForAction(id, day);
        if (record?.Status != "active" || record.Extended || record.DueDay is not int due
            || day > due || (long)due + 2 > int.MaxValue) return false;
        record.DueDay = due + 2;
        record.Extended = true;
        AddMoment(record, day, "extended", "You asked for more time before the deadline. She agreed to two more days.");
        return true;
    }

    public void AdvanceDay(int day)
    {
        if (!IsValid() || day < 0) return;
        foreach (var record in Records)
        {
            if (record.Status != "active" || record.DueDay is not int due || day <= due) continue;
            record.Status = "overdue";
            record.Contribution = -Definition(record.Id)!.Importance;
            record.WasLate = true;
            AddMoment(record, day, "late", "The promised day passed without a delivery. She is less sure she can count on this promise.");
        }
    }

    public bool Abandon(string id, int day)
    {
        var record = FindForAction(id, day);
        if (record?.Status is not ("active" or "overdue")) return false;
        AdvanceDay(day);
        record.Status = "abandoned";
        record.AbandonedDay = day;
        record.Contribution = id == "fish" ? 0 : -Definition(id)!.Importance;
        record.TestItemPending = false;
        AddMoment(record, day, "abandoned", id == "fish"
            ? "You let her know you would not bring the fish. It was a casual request, so there is no loss of trust."
            : "You told her you could not keep this promise. She remembers, but there will be a chance to make it right.");
        return true;
    }

    public bool Complete(string id, string itemId, string itemName, int day)
    {
        var record = FindForAction(id, day);
        if (record?.Status is not ("active" or "overdue") || string.IsNullOrWhiteSpace(itemId)
            || itemId.Length > 100 || string.IsNullOrWhiteSpace(itemName) || itemName.Length > 200) return false;
        if (id != "fish" && itemId != Definition(id)!.ItemId) return false;
        AdvanceDay(day);
        var repair = record.Contribution < 0 || record.WasLate || record.AbandonedDay != null;
        var weight = Definition(id)!.Importance;
        record.Status = "completed";
        record.CompletedDay = day;
        record.DeliveredItemId = itemId;
        record.DeliveredItemName = itemName;
        record.Contribution = repair ? Math.Max(1, weight - 1) : weight;
        record.TestItemPending = false;
        AddMoment(record, day, "completed", repair
            ? $"You brought her {itemName}. Following through now helps rebuild her trust."
            : $"You brought her {itemName}. She remembers that you kept your word.");
        return true;
    }

    public bool MarkTestItemGranted(string id)
    {
        if (!IsValid()) return false;
        var record = Records.FirstOrDefault(r => r.Id == id);
        if (record?.Status is not ("active" or "overdue") || !record.TestItemPending || record.TestItemGranted) return false;
        record.TestItemPending = false;
        record.TestItemGranted = true;
        return true;
    }

    private PromiseRecord? FindForAction(string id, int day)
    {
        if (!IsValid() || day < 0) return null;
        var record = Records.FirstOrDefault(r => r.Id == id);
        return record != null && day >= LastEventDay(record) ? record : null;
    }

    private static bool IsOutstanding(PromiseRecord record) => record.Status is "offered" or "active" or "overdue";
    private static int LastEventDay(PromiseRecord record) => Math.Max(record.OfferedDay,
        record.History.Count == 0 ? record.OfferedDay : record.History[^1].Day);

    private static void AddMoment(PromiseRecord record, int day, string kind, string text)
    {
        record.History.Add(new PromiseMoment { Day = day, Kind = kind, Text = text });
        if (record.History.Count > 24) record.History.RemoveRange(0, record.History.Count - 24);
    }

    private static bool IsValidRecord(PromiseRecord? record)
    {
        if (record == null || Definition(record.Id) is not { } definition || record.OfferedDay < 0
            || record.AcceptedDay < 0 || record.DueDay < 0 || record.CompletedDay < 0 || record.AbandonedDay < 0
            || record.DeliveredItemId == null || record.DeliveredItemId.Length > 100
            || record.DeliveredItemName == null || record.DeliveredItemName.Length > 200
            || record.History == null || record.History.Count is < 1 or > 24 || record.TestItemPending && record.TestItemGranted)
            return false;
        if (record.History.Any(h => h == null || h.Day < 0 || string.IsNullOrWhiteSpace(h.Text) || h.Text.Length > 1000
            || h.Kind is not ("offered" or "accepted" or "declined" or "extended" or "late" or "abandoned" or "completed"))) return false;
        for (var i = 1; i < record.History.Count; i++)
            if (record.History[i].Day < record.History[i - 1].Day) return false;
        if (record.Id == "fish" && (record.DueDay != null || record.Extended || record.WasLate)) return false;
        if (record.Id != "fish" && (record.TestItemPending || record.TestItemGranted)) return false;
        if (record.TestItemPending && record.Status != "active") return false;
        if (record.AcceptedDay is int accepted && accepted < record.OfferedDay) return false;
        if (record.AbandonedDay is int abandoned && record.Status != "abandoned" && abandoned > record.OfferedDay) return false;
        if (record.DueDay is int due && (record.AcceptedDay == null
            || (long)due - record.AcceptedDay.Value != (record.Extended ? 3 : 1)
                && (long)due - record.AcceptedDay.Value != (record.Extended ? 5 : 3))) return false;
        if (record.Extended && record.DueDay == null) return false;
        if (record.Status != "completed" && (record.CompletedDay != null || record.DeliveredItemId != "" || record.DeliveredItemName != "")) return false;
        if (record.Contribution < 0 && !record.WasLate && record.AbandonedDay == null) return false;
        if (record.Status == "completed" && record.DueDay is int completedDue
            && record.CompletedDay > completedDue && !record.WasLate) return false;
        var last = record.History[^1];
        var matchesHistory = record.Status switch {
            "offered" => last.Kind == "offered" && last.Day == record.OfferedDay,
            "declined" => last.Kind == "declined" && last.Day >= record.OfferedDay,
            "active" => last.Kind is "accepted" or "extended" && last.Day >= record.AcceptedDay,
            "overdue" => last.Kind == "late" && last.Day > record.DueDay,
            "abandoned" => last.Kind == "abandoned" && last.Day == record.AbandonedDay,
            "completed" => last.Kind == "completed" && last.Day == record.CompletedDay,
            _ => false
        };
        if (!matchesHistory) return false;
        var failure = record.Id == "fish" ? 0 : -definition.Importance;
        var acceptedState = record.AcceptedDay != null && (record.Id == "fish" || record.DueDay != null);
        return record.Status switch {
            "offered" or "declined" => record.AcceptedDay == null && record.DueDay == null && !record.Extended
                && (record.Contribution == 0 || record.Contribution == failure)
                && (record.Status != "declined" || record.History.LastOrDefault()?.Kind == "declined"),
            "active" => acceptedState && (record.Contribution == 0 || record.Contribution == failure),
            "overdue" => acceptedState && record.DueDay != null && record.WasLate && record.Contribution == failure,
            "abandoned" => acceptedState && record.AbandonedDay >= record.AcceptedDay && record.Contribution == failure,
            "completed" => acceptedState && record.CompletedDay >= record.AcceptedDay
                && !string.IsNullOrWhiteSpace(record.DeliveredItemId) && !string.IsNullOrWhiteSpace(record.DeliveredItemName)
                && (record.Id == "fish" || record.DeliveredItemId == definition.ItemId)
                && record.Contribution == (record.WasLate || record.AbandonedDay != null
                    ? Math.Max(1, definition.Importance - 1) : definition.Importance),
            _ => false
        };
    }
}
