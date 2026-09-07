namespace SolaceWeather.Core;

/// <summary>Haley's bounded sunflower promise. Only explicit game actions can accept or fulfill it.</summary>
public sealed class HaleyPromiseState
{
    public const string ItemId = "(O)421";
    public int Version { get; set; } = 1;
    public bool SeenSunflower { get; set; }
    public string Status { get; set; } = "none";
    public int LastActionDay { get; set; } = -1;
    public int? OfferedDay { get; set; }
    public int? AcceptedDay { get; set; }
    public int? DueDay { get; set; }
    public int? CompletedDay { get; set; }
    public bool Extended { get; set; }
    public bool WasLate { get; set; }
    public int Reliability { get; set; }
    public string DeliveredItemName { get; set; } = "";
    public bool Outstanding => Status is "offered" or "active" or "overdue";

    public bool IsValid() => Version == 1 && Status is "none" or "offered" or "active" or "overdue" or "completed" or "declined" or "abandoned"
        && LastActionDay >= -1 && Reliability is >= -1 and <= 1 && DeliveredItemName != null && DeliveredItemName.Length <= 200
        && new[] { OfferedDay, AcceptedDay, DueDay, CompletedDay }.All(d => d == null || d is >= 0 and <= 10000000)
        && (Status == "none" || SeenSunflower && OfferedDay.HasValue && LastActionDay >= OfferedDay)
        && (Status is not ("active" or "overdue" or "completed" or "abandoned") || AcceptedDay >= OfferedDay && DueDay > AcceptedDay)
        && (Status != "completed" || CompletedDay >= AcceptedDay && DeliveredItemName.Length > 0)
        && (Status == "completed" || CompletedDay == null);
    public void Observe(string itemId) { if (itemId == ItemId) SeenSunflower = true; }
    public bool CanOffer(int day) => IsValid() && SeenSunflower && day >= 0 && day >= LastActionDay
        && (Status == "none" || Status == "declined" && day >= LastActionDay + 3 || Status == "abandoned" && day >= LastActionDay + 2);
    public bool Offer(int day)
    {
        if (!CanOffer(day)) return false;
        Status = "offered"; OfferedDay = LastActionDay = day; AcceptedDay = DueDay = CompletedDay = null;
        Extended = false; WasLate = false; DeliveredItemName = ""; return true;
    }
    public bool Accept(int day, int days)
    {
        if (!ActionAllowed(day) || Status != "offered" || days is not (1 or 3) || day > 9999997) return false;
        Status = "active"; AcceptedDay = LastActionDay = day; DueDay = day + days; return true;
    }
    public bool Decline(int day)
    {
        if (!ActionAllowed(day) || Status != "offered") return false;
        Status = "declined"; LastActionDay = day; return true;
    }
    public bool Extend(int day)
    {
        if (!ActionAllowed(day) || Status != "active" || day > DueDay || Extended || DueDay > 9999998) return false;
        DueDay += 2; Extended = true; LastActionDay = day; return true;
    }
    public void AdvanceDay(int day)
    {
        if (!ActionAllowed(day) || Status != "active" || day <= DueDay) return;
        Status = "overdue"; WasLate = true; Reliability = -1; LastActionDay = day;
    }
    public bool Abandon(int day)
    {
        if (!ActionAllowed(day) || Status is not ("active" or "overdue")) return false;
        Status = "abandoned"; LastActionDay = day; Reliability = -1; return true;
    }
    public bool Complete(string itemId, string name, int day)
    {
        if (!ActionAllowed(day) || Status is not ("active" or "overdue") || itemId != ItemId || string.IsNullOrWhiteSpace(name) || name.Length > 200) return false;
        AdvanceDay(day);
        Reliability = WasLate || Reliability < 0 ? 0 : 1;
        Status = "completed"; CompletedDay = LastActionDay = day; DeliveredItemName = name; return true;
    }
    private bool ActionAllowed(int day) => IsValid() && day is >= 0 and <= 10000000 && day >= LastActionDay;
}
