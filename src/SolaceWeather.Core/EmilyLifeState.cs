namespace SolaceWeather.Core;

/// <summary>Only Emily's supported game actions; her personal conversation memory stays in the shared NPC memory store.</summary>
public sealed class EmilyLifeState
{
    public int Version { get; set; } = 1;
    public long FarmerId { get; set; }
    public EmilyClothPromise Promise { get; set; } = new();
    public EmilyDesignSession Session { get; set; } = new();
    public EmilyTreeState Tree { get; set; } = new();
    public int LastReminderDay { get; set; } = -1;
    public bool IsValid(long farmer) => Version == 1 && FarmerId == farmer && Promise?.IsValid() == true
        && Session?.IsValid() == true && Tree?.IsValid() == true && LastReminderDay >= -1;
}

public sealed class EmilyDesignSession
{
    public string Status { get; set; } = "none";
    public int OfferDay { get; set; } = -1;
    public int MeetingDay { get; set; } = -1;
    public int LastDeclineDay { get; set; } = -100;
    public int Step { get; set; }
    public string Mood { get; set; } = "";
    public string Pattern { get; set; } = "";
    public bool IsValid() => Status is "none" or "offered" or "accepted" or "active" or "completed" or "missed"
        && OfferDay is >= -1 and <= 10000000 && MeetingDay is >= -1 and <= 10000000 && LastDeclineDay >= -100
        && Step is >= 0 and <= 3 && Mood is "" or "quiet" or "playful" && Pattern is "" or "natural" or "geometric"
        && (Status == "none" || OfferDay >= 0 && MeetingDay >= OfferDay)
        && (Status != "completed" || Step == 3) && (Status is "active" or "completed" || Step == 0)
        && (Step < 1 ? Mood == "" : Mood != "") && (Step < 2 ? Pattern == "" : Pattern != "");
    public bool CanOffer(int day) => IsValid() && day is >= 0 and <= 9999993 && Status is "none" or "missed" && day - LastDeclineDay >= 3;
    public bool Offer(int day, int meetingDay)
    {
        if (!CanOffer(day) || meetingDay < day || meetingDay > day + 7) return false;
        Status = "offered"; OfferDay = day; MeetingDay = meetingDay; Step = 0; Mood = Pattern = ""; return true;
    }
    public bool Answer(int day, bool accept)
    {
        if (!IsValid() || Status != "offered" || day != OfferDay) return false;
        Status = accept ? "accepted" : "none"; if (!accept) LastDeclineDay = day; return true;
    }
    public bool Begin(int day, int minute)
    {
        if (!IsValid() || Status != "accepted" || day != MeetingDay || minute is < 660 or > 690) return false;
        Status = "active"; return true;
    }
    public bool Advance(int day, int minute, string choice, bool designObserved)
    {
        if (!IsValid() || Status != "active" || day != MeetingDay || minute is < 660 or > 720) return false;
        if (Step == 0 && choice is "quiet" or "playful") Mood = choice;
        else if (Step == 1 && choice is "natural" or "geometric") Pattern = choice;
        else if (Step != 2 || choice != "finish" || !designObserved) return false;
        Step++; if (Step == 3) Status = "completed"; return true;
    }
    public void CheckTime(int day, int minute)
    {
        if (Status == "offered" && day > OfferDay) Status = "none";
        if (Status == "accepted" && (day > MeetingDay || day == MeetingDay && minute > 690)
            || Status == "active" && (day != MeetingDay || minute > 720)) Interrupt();
    }
    public void Interrupt()
    {
        if (Status is not ("accepted" or "active")) return;
        Status = "missed"; Step = 0; Mood = Pattern = "";
    }
}
