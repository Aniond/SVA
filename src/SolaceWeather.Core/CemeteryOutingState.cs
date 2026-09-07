namespace SolaceWeather.Core;

/// <summary>One authored noncombat outing. Dialogue can offer it; only explicit game choices advance it.</summary>
public sealed class CemeteryOutingState
{
    public int Version { get; set; } = 1;
    public long FarmerId { get; set; }
    public string Status { get; set; } = "none";
    public int OfferDay { get; set; } = -1;
    public int MeetingDay { get; set; } = -1;
    public int LastDeclineDay { get; set; } = -100;
    public int Step { get; set; }
    public bool IsValid(long farmer) => Version == 1 && FarmerId == farmer && Status is "none" or "offered" or "accepted" or "active" or "completed" or "missed"
        && OfferDay >= -1 && MeetingDay >= -1 && LastDeclineDay >= -100 && Step is >= 0 and <= 3
        && (Status is "none" || OfferDay >= 0 && MeetingDay >= OfferDay)
        && (Status != "completed" || Step == 3) && (Status is "active" or "completed" || Step == 0);
    public bool CanOffer(int day) => IsValid(FarmerId) && day >= 0 && Status is "none" or "missed" && day - LastDeclineDay >= 3;
    public bool Offer(int day, int meetingDay)
    {
        if (!CanOffer(day) || meetingDay < day || meetingDay > day + 7) return false;
        Status = "offered"; OfferDay = day; MeetingDay = meetingDay; Step = 0; return true;
    }
    public bool Answer(int day, bool accept)
    {
        if (Status != "offered" || day != OfferDay) return false;
        Status = accept ? "accepted" : "none";
        if (!accept) LastDeclineDay = day;
        return true;
    }
    public bool Begin(int day, int minute)
    {
        if (Status != "accepted" || day != MeetingDay || minute is < 1200 or > 1230) return false;
        Status = "active"; Step = 0; return true;
    }
    public bool Advance(int day, int minute, int expectedStep, bool ghostWasVisible)
    {
        if (Status != "active" || day != MeetingDay || minute is < 1200 or > 1260 || Step != expectedStep || Step == 2 && !ghostWasVisible) return false;
        Step++;
        if (Step == 3) Status = "completed";
        return true;
    }
    public void CheckTime(int day, int minute)
    {
        if (Status == "offered" && day > OfferDay) Status = "none";
        if (Status == "accepted" && (day > MeetingDay || day == MeetingDay && minute > 1230)
            || Status == "active" && (day != MeetingDay || minute > 1260)) Interrupt();
    }
    public void Interrupt()
    {
        if (Status is "accepted" or "active") { Status = "missed"; Step = 0; }
    }
}
