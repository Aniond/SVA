namespace SolaceWeather.Core;

public sealed class PhoneMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Text { get; set; } = "";
    public bool Outgoing { get; set; }
    public string Status { get; set; } = "sent";
    public int Day { get; set; }
    public int Time { get; set; }
}

public sealed class PhoneThread
{
    public List<PhoneMessage> Messages { get; set; } = new();
    public List<string> Shortcuts { get; set; } = new();
    public bool Unread { get; set; }
    public int LastInitiativeDay { get; set; } = -100;
}

public sealed class PhoneContact
{
    public bool Exchanged { get; set; }
    public int LastOfferDay { get; set; } = -100;
    public bool OfferPending { get; set; }
    public string BlockReason { get; set; } = "";
}

/// <summary>Only persisted with the owning farm. Request identity survives a retry.</summary>
public sealed class PhoneState
{
    public int Version { get; set; } = 1;
    public long FarmerId { get; set; }
    public Dictionary<string, PhoneThread> Threads { get; set; } = new();
    public Dictionary<string, PhoneContact> Contacts { get; set; } = new();
    public int LastInitiativeDay { get; set; } = -100;
    public bool HasNumber(string name) => Contacts.TryGetValue(name, out var contact) && contact.Exchanged;
    public bool CanText(string name) => HasNumber(name) && Contacts[name].BlockReason == "";
    public bool TryOfferNumber(string name, int day)
    {
        if (!RomanceRules.IsCandidate(name) || day < 0 || HasNumber(name)) return false;
        if (!Contacts.TryGetValue(name, out var contact)) Contacts[name] = contact = new();
        if (contact.BlockReason != "" || day - contact.LastOfferDay < 3) return false;
        contact.LastOfferDay = day; contact.OfferPending = true;
        return true;
    }
    public bool AnswerNumberOffer(string name, bool accept, int day)
    {
        if (!Contacts.TryGetValue(name, out var contact) || contact.Exchanged || !contact.OfferPending
            || contact.LastOfferDay != day || contact.BlockReason != "") return false;
        contact.OfferPending = false; contact.Exchanged = accept;
        return true;
    }
    public void SetBlock(string name, string reason)
    {
        if (!RomanceRules.IsCandidate(name) || reason is not ("" or "separated" or "ending-relationship" or "divorced")) return;
        if (!Contacts.TryGetValue(name, out var contact))
        {
            if (reason == "") return;
            Contacts[name] = contact = new();
        }
        contact.BlockReason = reason;
        if (reason == "") return;
        contact.OfferPending = false;
        if (Threads.TryGetValue(name, out var thread))
            foreach (var message in thread.Messages.Where(m => m.Status == "pending")) message.Status = "failed";
    }
    public static string RelationshipBlock(RomanceCharacterState? relationship, bool nativeDivorced)
        => nativeDivorced ? "divorced" : relationship?.PendingTransition != null ? "ending-relationship"
            : relationship?.SeparationUntilDay != null ? "separated" : relationship?.PhoneBlockReason ?? "";
    public PhoneThread Thread(string name)
    {
        if (!RomanceRules.IsCandidate(name)) throw new ArgumentException("Unsupported contact.");
        if (!Threads.TryGetValue(name, out var thread)) Threads[name] = thread = new();
        return thread;
    }

    public static bool ValidText(string? text, int limit = 500) => !string.IsNullOrWhiteSpace(text)
        && text.Length <= limit && !text.Any(char.IsControl);

    public PhoneMessage? Send(string name, string text, int day, int time)
    {
        if (!CanText(name) || !ValidText(text) || day < 0 || time < 0) return null;
        var thread = Thread(name);
        if (thread.Messages.Any(m => m.Outgoing && m.Status != "sent")) return null;
        var message = new PhoneMessage { Text = text.Trim(), Outgoing = true, Status = "pending", Day = day, Time = time };
        thread.Messages.Add(message);
        thread.Shortcuts.Remove(message.Text);
        thread.Shortcuts.Insert(0, message.Text);
        thread.Shortcuts = thread.Shortcuts.Take(12).ToList();
        Trim(thread);
        return message;
    }

    public PhoneMessage? Retry(string name)
    {
        if (!CanText(name)) return null;
        var message = Thread(name).Messages.LastOrDefault(m => m.Outgoing && m.Status == "failed");
        if (message != null) message.Status = "pending";
        return message;
    }

    public bool Complete(string id, string reply)
    {
        if (!ValidText(reply, 2400)) return false;
        foreach (var pair in Threads)
        {
            if (!CanText(pair.Key)) continue;
            var thread = pair.Value;
            var message = thread.Messages.FirstOrDefault(m => m.Id == id && m.Outgoing && m.Status == "pending");
            if (message == null) continue;
            message.Status = "sent";
            thread.Messages.Add(new() { Text = reply, Day = message.Day, Time = message.Time });
            thread.Unread = true;
            Trim(thread);
            return true;
        }
        return false;
    }

    public void Fail(string id)
    {
        foreach (var message in Threads.Values.SelectMany(t => t.Messages).Where(m => m.Id == id && m.Status == "pending"))
            message.Status = "failed";
    }

    public void Interrupt()
    {
        foreach (var message in Threads.Values.SelectMany(t => t.Messages).Where(m => m.Status == "pending")) message.Status = "failed";
    }

    public bool TryInitiative(string name, int day)
    {
        if (!CanText(name) || day <= LastInitiativeDay) return false;
        var thread = Thread(name);
        if (day - thread.LastInitiativeDay < 3 || thread.Unread || thread.Messages.Any(m => m.Status != "sent")) return false;
        LastInitiativeDay = thread.LastInitiativeDay = day;
        return true;
    }

    public bool Incoming(string name, string text, int day, int time)
    {
        if (!CanText(name) || !ValidText(text, 2400) || day < 0 || time < 0) return false;
        var thread = Thread(name);
        thread.Messages.Add(new() { Text = text, Day = day, Time = time });
        thread.Unread = true;
        Trim(thread);
        return true;
    }

    private static void Trim(PhoneThread thread) => thread.Messages = thread.Messages.TakeLast(80).ToList();

    public bool IsValid(long farmer) => Version == 1 && FarmerId == farmer && Threads != null && Threads.Count <= 12
        && Contacts != null && Contacts.Count <= 12 && Contacts.All(p => RomanceRules.IsCandidate(p.Key) && p.Value != null
            && p.Value.LastOfferDay >= -100 && p.Value.BlockReason is "" or "separated" or "ending-relationship" or "divorced")
        && LastInitiativeDay >= -100
        && Threads.All(p => RomanceRules.IsCandidate(p.Key) && p.Value != null && p.Value.LastInitiativeDay >= -100
            && p.Value.Messages != null && p.Value.Messages.Count <= 80
            && p.Value.Messages.All(m => m != null && Guid.TryParseExact(m.Id, "N", out _) && ValidText(m.Text, m.Outgoing ? 500 : 2400)
                && m.Day >= 0 && m.Time >= 0 && (m.Status == "sent" || m.Outgoing && m.Status is "pending" or "failed"))
            && p.Value.Messages.Select(m => m.Id).Distinct().Count() == p.Value.Messages.Count
            && p.Value.Shortcuts != null && p.Value.Shortcuts.Count <= 12 && p.Value.Shortcuts.All(s => ValidText(s))
            && p.Value.Shortcuts.Distinct().Count() == p.Value.Shortcuts.Count);
}
