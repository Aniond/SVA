namespace SolaceWeather.Core;

/// <summary>Only Haley's supported game actions; her personal conversation memory stays in the shared NPC memory store.</summary>
public sealed class HaleyLifeState
{
    public int Version { get; set; } = 1;
    public long FarmerId { get; set; }
    public HaleyPromiseState Promise { get; set; } = new();
    public HaleyPhotoOuting Outing { get; set; } = new();
    public HaleyTreeState Tree { get; set; } = new();
    public int LastReminderDay { get; set; } = -1;
    public bool IsValid(long farmer) => Version == 1 && FarmerId == farmer && Promise?.IsValid() == true
        && Outing?.IsValid() == true && Tree?.IsValid() == true && LastReminderDay >= -1;
}

public sealed class HaleyPhotoOuting
{
    public string Status { get; set; } = "none";
    public int OfferDay { get; set; } = -1;
    public int MeetingDay { get; set; } = -1;
    public int LastDeclineDay { get; set; } = -100;
    public int Step { get; set; }
    public string Framing { get; set; } = "";
    public string Style { get; set; } = "";
    public bool IsValid() => Status is "none" or "offered" or "accepted" or "active" or "completed" or "missed"
        && OfferDay is >= -1 and <= 10000000 && MeetingDay is >= -1 and <= 10000000 && LastDeclineDay >= -100
        && Step is >= 0 and <= 3 && Framing is "" or "wide" or "detail" && Style is "" or "candid" or "posed"
        && (Status == "none" || OfferDay >= 0 && MeetingDay >= OfferDay)
        && (Status != "completed" || Step == 3) && (Status is "active" or "completed" || Step == 0)
        && (Step < 1 ? Framing == "" : Framing != "") && (Step < 2 ? Style == "" : Style != "");
    public bool CanOffer(int day) => IsValid() && day is >= 0 and <= 9999993 && Status is "none" or "missed" && day - LastDeclineDay >= 3;
    public bool Offer(int day, int meetingDay)
    {
        if (!CanOffer(day) || meetingDay < day || meetingDay > day + 7) return false;
        Status = "offered"; OfferDay = day; MeetingDay = meetingDay; Step = 0; Framing = Style = ""; return true;
    }
    public bool Answer(int day, bool accept)
    {
        if (!IsValid() || Status != "offered" || day != OfferDay) return false;
        Status = accept ? "accepted" : "none"; if (!accept) LastDeclineDay = day; return true;
    }
    public bool Begin(int day, int minute)
    {
        if (!IsValid() || Status != "accepted" || day != MeetingDay || minute is < 1020 or > 1050) return false;
        Status = "active"; return true;
    }
    public bool Advance(int day, int minute, string choice, bool shutterObserved)
    {
        if (!IsValid() || Status != "active" || day != MeetingDay || minute is < 1020 or > 1080) return false;
        if (Step == 0 && choice is "wide" or "detail") Framing = choice;
        else if (Step == 1 && choice is "candid" or "posed") Style = choice;
        else if (Step != 2 || choice != "finish" || !shutterObserved) return false;
        Step++; if (Step == 3) Status = "completed"; return true;
    }
    public void CheckTime(int day, int minute)
    {
        if (Status == "offered" && day > OfferDay) Status = "none";
        if (Status == "accepted" && (day > MeetingDay || day == MeetingDay && minute > 1050)
            || Status == "active" && (day != MeetingDay || minute > 1080)) Interrupt();
    }
    public void Interrupt()
    {
        if (Status is not ("accepted" or "active")) return;
        Status = "missed"; Step = 0; Framing = Style = "";
    }
}
