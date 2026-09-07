using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

/// <summary>One farm-owned source of relationship evidence, shared by chat, appointments and native actions.</summary>
internal sealed class RomanceService
{
    internal sealed class FarmRelationships
    {
        public int Version { get; set; } = 1;
        public long FarmerId { get; set; }
        public RomanceSaveState State { get; set; } = new();
        public Dictionary<string, AbigailMemory> Memories { get; set; } = new();
        public Dictionary<string, int> AcknowledgedNoShows { get; set; } = new();
        public Dictionary<string, string> Notices { get; set; } = new();
        public Dictionary<string, int> LastFlirtDay { get; set; } = new();
        public bool IsValid(long farmer) => Version == 1 && FarmerId == farmer && State != null && State.IsValid()
            && Memories != null && Memories.Count <= 12 && Memories.All(p => RomanceRules.IsCandidate(p.Key) && p.Value != null && p.Value.IsValid())
            && Notices != null && Notices.Count <= 128 && Notices.All(p => p.Key.Length <= 100 && p.Value != null && p.Value.Length <= 1200)
            && LastFlirtDay != null && LastFlirtDay.Count <= 12 && LastFlirtDay.All(p => RomanceRules.IsCandidate(p.Key) && p.Value >= 0)
            && AcknowledgedNoShows != null && AcknowledgedNoShows.Count <= 12 && AcknowledgedNoShows.All(p => RomanceRules.IsCandidate(p.Key) && p.Value >= 0);
    }

    private const string SaveKey = "global-relationships";
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly AbigailRelationship abigail;
    private FarmRelationships? farm;
    private readonly RomanceSaveState unavailable = new() { Version = -1 };
    private string selected = "Abigail";
    private readonly Dictionary<string, HashSet<string>> selectedMemories = new();
    internal RomanceSaveState State => farm?.State ?? unavailable;
    internal bool Ready => farm != null && Context.IsWorldReady && !Context.IsMultiplayer && config.EnableAbigailMemory;
    internal RomanceDates Dates { get; }
    internal RomanceNative Native { get; }
    internal Action<NPC>? OpenConversation { get; set; }
    internal Func<string, object>? PhoneContactContext { get; set; }
    internal Func<string, object>? PublicChatterContext { get; set; }
    internal Func<string, object>? OutingContext { get; set; }
    internal Func<string, bool, object>? CharacterLifeContext { get; set; }
    internal Func<string, bool, object>? FashionContext { get; set; }
    internal Action<string, ConversationReply, string>? PhoneReplyRemembered { get; set; }
    internal Action<string>? OpenCharacterTree { get; set; }
    internal AbigailMemory? CharacterMemory(string name) => Ready && RomanceRules.IsCandidate(name) ? Memory(name) : null;

    internal RomanceService(IModHelper helper, IMonitor monitor, ModConfig config, AbigailRelationship abigail, string modId)
    {
        this.helper = helper; this.monitor = monitor; this.config = config; this.abigail = abigail;
        Native = new(helper, monitor, config, () => State, npc => OpenConversation?.Invoke(npc));
        Native.Register(modId);
        Dates = new(() => State, npc => Ready && Supported(npc), null, RememberActivity, NarrateActivity, Signal);
        abigail.GlobalJournal = () => OpenJournal(selected);
        helper.Events.GameLoop.SaveLoaded += (_, _) => Guard(Load);
        helper.Events.GameLoop.Saving += (_, _) => Guard(() =>
        {
            if (!Ready) return;
            Observe();
            if (!farm!.IsValid(Game1.player.UniqueMultiplayerID)) throw new InvalidDataException("Relationship state failed validation; save data preserved.");
            helper.Data.WriteSaveData(SaveKey, farm);
        });
        helper.Events.GameLoop.DayStarted += (_, _) => Guard(() =>
        {
            if (!Ready) return;
            Native.ImportNative(Today);
            RomanceRules.AdvanceDay(State, Today);
            foreach (var memory in farm!.Memories.Values) memory.Personal.StartDay(Today, true);
            QueueWarnings(); Dates.OnDayStarted();
        });
        helper.Events.GameLoop.DayEnding += (_, _) => Guard(() => { if (Ready) { Dates.CancelForTransition(); FinalizeTransitions(); } });
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { Dates.CancelForTransition(); farm = null; selectedMemories.Clear(); Native.OnReturnedToTitle(); };
        helper.Events.GameLoop.TimeChanged += (_, _) => Guard(() => { if (Ready) { Dates.OnTimeChanged(); SpreadNews(); } });
        helper.Events.GameLoop.UpdateTicked += (_, e) => Guard(() =>
        {
            if (!Ready) return;
            Dates.OnUpdateTicked();
            if (Game1.activeClickableMenu is LetterViewerMenu letter)
                foreach (var pair in State.Characters)
                    if (letter.mailTitle != null && letter.mailTitle.StartsWith("SolaceRomance_" + pair.Key + "_", StringComparison.Ordinal)
                        && farm!.Notices.ContainsKey(letter.mailTitle)) RomanceRules.DeliverWarning(State, pair.Key, Today);
            if (e.IsMultipleOf(60)) Observe();
        });
        helper.Events.Content.AssetRequested += (_, e) =>
        {
            if (!e.NameWithoutLocale.IsEquivalentTo("Data/mail") || !Ready) return;
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                foreach (var notice in farm!.Notices) data[notice.Key] = notice.Value;
                foreach (var pair in State.Characters.Where(p => p.Value.InConflict))
                    data[WarningKey(pair.Key, pair.Value)] = Boundary(pair.Key) + "^We need to discuss what happens next. Another romantic boundary violation after this warning means time apart.^-" + pair.Key + "[#]A relationship boundary";
            });
        };
    }

    private void FinalizeTransitions()
    {
        var pending = State.Characters.Where(p => p.Value.PendingTransition != null).Select(p => (p.Key, Outcome: p.Value.PendingTransition)).ToArray();
        Native.OnDayEnding();
        foreach (var (name, outcome) in pending)
        {
            if (State.Characters[name].PendingTransition != null) continue;
            string key = $"SolaceRomanceOutcome_{name}_{Today}";
            string message = outcome == "divorce" ? "Our divorce is now finalized. The children and the farm stay with you. Our shared history still matters, but our marriage has ended." : "Our dating relationship has ended. We can find our way back to ordinary friendship with time. Any future romance needs a new, explicit beginning.";
            farm!.Notices[key] = message + "^-" + name + "[#]A change in our relationship";
            while (farm.Notices.Count > 128) farm.Notices.Remove(farm.Notices.Keys.First());
            if (!Game1.player.hasOrWillReceiveMail(key)) Game1.player.mailForTomorrow.Add(key);
        }
        if (pending.Length > 0) helper.GameContent.InvalidateCache("Data/mail");
    }

    private static int Today => Game1.Date.TotalDays;
    internal static bool Supported(NPC? npc) => npc != null && RomanceRules.IsCandidate(npc.Name) && npc.datable.Value;
    internal void Contact(NPC npc)
    {
        if (!Ready || !Supported(npc)) return;
        selected = npc.Name;
        RomanceRules.Talk(State, npc.Name, Today);
        Memory(npc.Name).Observe(Today, true, 0);
    }
    private AbigailMemory Memory(string name)
    {
        if (!farm!.Memories.TryGetValue(name, out var memory)) farm.Memories[name] = memory = new();
        return memory;
    }
    private void Load()
    {
        farm = null;
        if (!config.EnableAbigailMemory || Context.IsMultiplayer) return;
        var loaded = helper.Data.ReadSaveData<FarmRelationships>(SaveKey);
        if (loaded != null && !loaded.IsValid(Game1.player.UniqueMultiplayerID)) throw new InvalidDataException("Unsupported relationship save; existing data preserved.");
        farm = loaded ?? new() { FarmerId = Game1.player.UniqueMultiplayerID };
        if (!farm.Memories.ContainsKey("Abigail") && abigail.Memory != null) farm.Memories["Abigail"] = abigail.Memory;
        if (farm.Memories.TryGetValue("Abigail", out var legacy))
        {
            abigail.AdoptMemory(legacy);
            if (loaded == null)
                foreach (var recorded in legacy.Days.Where(d => d.Talked && d.Day <= Today).OrderBy(d => d.Day))
                    RomanceRules.Talk(State, "Abigail", recorded.Day);
        }
        Native.ImportNative(Today);
        RomanceRules.AdvanceDay(State, Today);
        Observe(); QueueWarnings(); Dates.EnsureQuest();
        helper.GameContent.InvalidateCache("Data/Characters");
    }
    private void Observe()
    {
        if (!Ready) return;
        foreach (string name in RomanceRules.Candidates)
            if (Game1.player.friendshipData.TryGetValue(name, out var friendship) && friendship.TalkedToToday)
            {
                RomanceRules.Talk(State, name, Today);
                Memory(name).Observe(Today, true, friendship.GiftsToday);
            }
        foreach (var pair in State.Characters.Where(p => p.Value.InConflict))
        {
            var incident = State.Incidents.FirstOrDefault(i => i.Sequence == pair.Value.LastViolationSequence);
            if (incident == null) continue;
            var knowledge = State.Knowledge.FirstOrDefault(k => k.KnowerNpc == pair.Key && k.IncidentId == incident.Id);
            if (knowledge == null) continue;
            string source = knowledge.Hops == 0 ? "personally witnessed" : "heard from " + knowledge.Source + "; the original eyewitness was " + knowledge.OriginalEyewitness;
            Memory(pair.Key).Experiences.Record("boundary:" + incident.ActorNpc + ":" + incident.Kind, "romantic-boundary", knowledge.Day,
                $"On {AbigailDeliveryQuest.DateLabel(knowledge.Day)}, {pair.Key} {source}. The verified romantic action was {incident.Kind} with {incident.ActorNpc}, on {AbigailDeliveryQuest.DateLabel(incident.Day)}. It disappointed this partner because exclusivity was agreed. This is not evidence of any other action.",
                "commitment jealousy disappointment boundary repair " + incident.ActorNpc);
        }
        foreach (var moment in State.History.Where(h => h.Kind is "repaired" or "separation" or "breakup" or "divorce"))
        {
            string fact = moment.Kind switch
            {
                "repaired" => "The game confirmed that the repair agreement was fulfilled through shared activities and reliable behavior. The disappointment and the effort to repair it remain part of our history.",
                "separation" => "A second distinct known violation after the delivered warning started fourteen days apart. This did not erase earlier affection or shared experiences.",
                "breakup" => "The separation repair requirements were not fulfilled by the deadline. The game announced that the dating relationship would end.",
                _ => "The separation repair requirements were not fulfilled by the deadline. The game began divorce proceedings; final cleanup may await a pending birth or adoption."
            };
            Memory(moment.Npc).Experiences.Record("relationship:" + moment.Kind, "relationship", moment.Day, fact, "affection disappointment separation repair history");
        }
    }

    internal object GetContext(string name, string message)
        => BuildContext(name, message, false);

    internal object GetPhoneContext(string name, string message)
        => BuildContext(name, message, true);

    private object BuildContext(string name, string message, bool phone)
    {
        if (!Ready || !RomanceRules.IsCandidate(name)) return new JsonObject { ["Unavailable"] = true };
        var memory = Memory(name);
        JsonObject context = name == "Abigail" ? JsonSerializer.SerializeToNode(phone ? abigail.GetPhoneContext(message) : abigail.GetConversationContext(message))!.AsObject() : new();
        var c = State.Characters.GetValueOrDefault(name);
        var experiences = memory.Experiences.Select(message, Today);
        if (!phone) selectedMemories[name] = experiences.Select(e => e.Id).ToHashSet();
        context["Personality"] = JsonSerializer.SerializeToNode(RomanceProfiles.Get(name));
        if (name == "Penny") context["FamilyHome"] = JsonSerializer.SerializeToNode(new {
            PamHouseUpgraded = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"),
            PamHome = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade") ? "upgraded house" : "trailer",
            Rule = "This identifies Pam's current home, not proof Penny still lives there after marriage or that the farmer paid for the upgrade." });
        if (CharacterLifeContext != null && name is "Haley" or "Emily") context[name + "Life"] = JsonSerializer.SerializeToNode(CharacterLifeContext(name, phone));
        if (FashionContext != null) context["Fashion"] = JsonSerializer.SerializeToNode(FashionContext(name, phone));
        if (OutingContext != null) context["AvailableOuting"] = JsonSerializer.SerializeToNode(OutingContext(name));
        if (PublicChatterContext != null) context["WitnessedPublicChatter"] = JsonSerializer.SerializeToNode(PublicChatterContext(name));
        if (PhoneContactContext != null) context["PhoneContact"] = JsonSerializer.SerializeToNode(PhoneContactContext(name));
        context["RomanceJourney"] = JsonSerializer.SerializeToNode(new
        {
            Stage = State.GetJourney(name, Today).BondStage,
            AdditionalState = State.GetJourney(name, Today).Stage,
            Intention = c?.FriendshipOnly == true ? "friendship only; never steer toward romance" : c?.InterestExpressed == true ? "farmer expressed interest; not automatically reciprocated" : "not discussed",
            Agreement = c?.IsDating == true ? c.NeedsReaffirmation ? "imported relationship; exclusivity must be reaffirmed" : "exclusive relationship" : "no romantic commitment",
            DatingReadiness = State.GetJourney(name, Today).CanDate && !BrokenPromise(name),
            ProposalReadiness = State.GetJourney(name, Today).CanPropose && !BrokenPromise(name),
            RepairAgreement = c?.InConflict == true ? "Acknowledge the boundary, complete two designated repair activities on different days, and seven consecutive days without a further violation. If separated, resolve only at the fixed fourteen-day deadline. Failed repair then ends dating or proceeds toward divorce." : null,
            c?.InConflict, c?.SeparationUntilDay, c?.RepairAcknowledged, c?.RepairActivities, c?.LastViolationDay,
            KnownIncidents = State.Knowledge.Where(k => k.KnowerNpc == name).Select(k => new { Evidence = State.Incidents.FirstOrDefault(i => i.Id == k.IncidentId), k.Source, k.OriginalEyewitness, k.Hops, k.Day, Meaning = k.Hops == 0 ? "personally witnessed" : "heard from source, not personally witnessed" }).ToArray(),
            RecentSharedTime = State.History.Where(h => h.Npc == name).TakeLast(16).ToArray(),
            UpcomingPlan = State.Booking?.Npc == name ? State.Booking : null,
            AvailableChoices = Choices(name).Select(q => q.Label).ToArray(),
            Household = c?.IsMarried == true ? new { Children = Game1.player.getChildrenCount(), Home = Game1.player.homeLocation.Value } : null,
            Rule = "Only explicit validated choices change intentions, dates, commitments or consequences. Ordinary friendships and gifts are never betrayal. Never invent witnessed events, punishments or eligibility. Dates are not completed until three recorded segments. Known history is evidence; quoted chat is attributed speech. A pending separation outcome is not yet a finalized divorce."
        });
        Game1.player.friendshipData.TryGetValue(name, out var nativeFriendship);
        context["NativeRelationship"] = JsonSerializer.SerializeToNode(new { Hearts = (nativeFriendship?.Points ?? 0) / 250, Status = nativeFriendship?.Status.ToString(), Rule = "Keep private backstory and unseen heart events undisclosed; hearts do not override authored relationship readiness." });
        context["PublicTownState"] = JsonSerializer.SerializeToNode(new { CommunityCenterRestored = Game1.MasterPlayer.mailReceived.Contains("ccIsComplete"), JojaMembership = Game1.MasterPlayer.mailReceived.Contains("JojaMember") });
        context["Farmer"] = JsonValue.Create(Game1.player.Name);
        context["Day"] = JsonValue.Create(Today);
        context["Time"] = JsonValue.Create(Game1.timeOfDay);
        context["Location"] = JsonValue.Create(Game1.currentLocation.Name);
        context["Conversations"] = JsonSerializer.SerializeToNode(memory.Exchanges);
        context["PersistentDetails"] = JsonSerializer.SerializeToNode(memory.Personal.Details);
        context["SharedExperiences"] = JsonSerializer.SerializeToNode(experiences);
        context["MayOfferInitiative"] = JsonValue.Create(memory.Personal.LastFollowUpDay != Today && memory.Promises.LastReminderDay != Today);
        if (name != "Abigail")
        {
            var followUp = memory.Personal.FollowUp(Today);
            context["OfferedFollowUp"] = JsonSerializer.SerializeToNode(followUp);
            context["MaySpontaneouslyRecall"] = JsonValue.Create(followUp == null && memory.Personal.LastFollowUpDay != Today);
            context["ActivityEvidence"] = JsonValue.Create("Only recorded activities shared with this character are verified. Other private farmer activities are unknown.");
        }
        return context;
    }

    internal void RememberReply(string name, string message, ConversationReply reply)
    {
        if (!Ready || !RomanceRules.IsCandidate(name)) return;
        if (name == "Abigail") { abigail.RememberReply(message, reply); return; }
        var memory = Memory(name);
        memory.Personal.Apply(Today, message, reply.Memories);
        memory.Exchanges.Add(new() { Day = Today, Farmer = message, Reply = reply.Reply });
        memory.Exchanges = memory.Exchanges.TakeLast(32).ToList();
        if (selectedMemories.GetValueOrDefault(name)?.Contains(reply.RecalledExperienceId ?? "") == true)
            memory.Experiences.Reflect(reply.RecalledExperienceId!, Today, message, reply.Reply);
        if (!string.IsNullOrEmpty(reply.AskedTopic)) memory.Personal.MarkAsked(reply.AskedTopic, Today);
        if (reply.SpontaneousRecall) memory.Personal.LastFollowUpDay = Today;
    }

    internal void RememberPhoneReply(string name, string message, ConversationReply reply, string snapshot)
    {
        if (!Ready || !RomanceRules.IsCandidate(name)) return;
        // Phone requests can finish while an in-person conversation has selected other context.
        // Validate recalls against this request's snapshot, never those mutable selections.
        var context = JsonNode.Parse(snapshot)?["Relationship"];
        var memory = Memory(name);
        if (message.Length > 0) memory.Personal.Apply(Today, message, reply.Memories);
        memory.Exchanges.Add(new() { Day = Today, Farmer = message, Reply = reply.Reply });
        memory.Exchanges = memory.Exchanges.TakeLast(32).ToList();
        string? offeredTopic = context?["OfferedFollowUp"]?["Topic"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(reply.AskedTopic) && reply.AskedTopic == offeredTopic)
        {
            memory.Personal.MarkAsked(reply.AskedTopic, Today);
            if (name == "Abigail")
            {
                memory.Personal.LastFollowUpDay = Today;
                memory.Promises.LastReminderDay = Today;
            }
        }
        bool recordedRecall = !string.IsNullOrEmpty(reply.RecalledExperienceId) && context?["SharedExperiences"] is JsonArray experiences
            && experiences.Any(e => e?["Id"]?.GetValue<string>() == reply.RecalledExperienceId);
        if (recordedRecall)
        {
            memory.Experiences.Reflect(reply.RecalledExperienceId!, Today, message, reply.Reply);
            if (reply.SpontaneousRecall)
            {
                memory.Personal.LastFollowUpDay = Today;
                if (name == "Abigail") memory.Promises.LastReminderDay = Today;
            }
        }
        // Texting deliberately cannot create an offered quest or execute a relationship choice.
        PhoneReplyRemembered?.Invoke(name, reply, snapshot);
    }
    internal string Fallback(string name)
    {
        var c = State.Characters.GetValueOrDefault(name);
        if (c?.InConflict == true)
            return "What happened hurt, and words alone won't repair it. We need to agree on our boundary, make time for two repair activities on different days, and see seven days of follow-through."
                + (c.SeparationUntilDay.HasValue ? " I still need the full time apart we agreed on." : " We can talk about taking that first step.");
        if (c?.FriendshipOnly == true) return "I'm glad we're clear about being friends. We can still make time for each other without turning it into romance.";
        return name switch
        {
            "Abigail" => "Hey. A little room to explore sounds good to me. We can talk, or find a free hour to get out together.",
            "Alex" => "Hey. It's good to take a breather sometimes. We could find a free hour to hang out.",
            "Elliott" => "A little unhurried company would be welcome. Shall we look for a time when neither of us needs to rush?",
            "Emily" => "I'd like to hear what you've been noticing lately. We could make a little time to talk together.",
            "Haley" => "It's nice when someone actually makes time to listen. We could find a free afternoon to catch up.",
            "Harvey" => "A quiet conversation sounds lovely. I'd like to find a time when I can give it my full attention.",
            "Leah" => "Leaving a little space in the day for company sounds good. We could plan a quiet hour together.",
            "Maru" => "I'd like to hear what's been on your mind. Let's find some time when neither of us has to rush off.",
            "Penny" => "We don't need to do anything elaborate. A little time to listen to each other would be nice.",
            "Sam" => "Hey! A break with some company sounds pretty good. Let's see when we both have a free hour.",
            "Sebastian" => "I don't mind a little quiet company. We don't have to fill every silence, either.",
            "Shane" => "I'm not great at making conversation. But a quiet break wouldn't be the worst thing.",
            _ => "We can make time to talk."
        };
    }
    private void RememberActivity(string name, string fact, string reply)
    {
        if (!Ready) return;
        string group = fact.Contains("complete", StringComparison.OrdinalIgnoreCase) ? "completion" : "segment";
        string activity = fact.Contains("lake", StringComparison.OrdinalIgnoreCase) ? "lake" : fact.Contains("saloon", StringComparison.OrdinalIgnoreCase) ? "saloon" : "walk";
        // Bounded dated memories retain the actual activity and choices, never fabricate a gift or reward.
        Memory(name).Experiences.Record("shared:" + activity + ":" + group + ":" + State.Booking?.SegmentsCompleted, "shared-time", Today,
            fact[..Math.Min(1600, fact.Length)], "walk lake saloon shared time date repair");
    }
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private Task<string> NarrateActivity(NPC npc, string fact)
    {
        string key = Environment.GetEnvironmentVariable("GEMINI_API_KEY", EnvironmentVariableTarget.User) ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        if (!config.EnableAbigailAi || key.Length == 0) return Task.FromResult("");
        string snapshot = JsonSerializer.Serialize(GetContext(npc.Name, fact));
        return Narrate(snapshot, npc.Name, key, fact);
    }
    private async Task<string> Narrate(string snapshot, string name, string key, string fact)
    {
        var reply = await new GeminiConversation(Http).ReplyForCharacter(key, config.GeminiModel, snapshot,
            "React briefly to this game-verified activity segment: " + fact, name, CancellationToken.None).ConfigureAwait(false);
        return reply.Reply;
    }

    private bool BrokenPromise(string name) => name == "Abigail" && abigail.Promises?.Records.Any(p => (p.Status is "overdue" or "abandoned") && p.Id != "fish") == true;
    internal QuestChoice[] Choices(string name)
    {
        if (!Ready || !RomanceRules.IsCandidate(name)) return Array.Empty<QuestChoice>();
        var c = State.Characters.GetValueOrDefault(name);
        var result = new List<QuestChoice>();
        void Add(string key, string label) => result.Add(new("romance:" + key, label));
        if (c?.IsDating != true)
        {
            if (c?.FriendshipOnly != true) Add("friendship", "Friendship only");
            if (c?.InterestExpressed != true) Add("interest", "Interested in something more");
            if (State.GetJourney(name, Today).CanDate && !BrokenPromise(name)) Add("dating", "Let's date exclusively — just you and me");
        }
        if (c?.NeedsReaffirmation == true) Add("reaffirm", "Reaffirm our exclusive relationship");
        if (c?.InConflict == true)
        {
            if (!c.WarningDeliveredDay.HasValue) Add("discuss", "Let's talk about what happened");
            if (!c.RepairAcknowledged) Add("repair", "I agree: respect our boundary and rebuild trust together");
            if (c.RepairAcknowledged) Add("repair-date", "Plan time together to rebuild trust");
        }
        else
        {
            if ((c?.NoShows ?? 0) > farm!.AcknowledgedNoShows.GetValueOrDefault(name)) Add("missed", "I'm sorry I missed our meeting; let's discuss another plan");
            else
            {
                Add("activity", State.Booking == null ? "Plan time together as friends" : "Our shared activity plan");
                if (c?.IsDating == true && c.FriendshipOnly == false && !c.NeedsReaffirmation)
                    Add("romantic-date", "Ask for a romantic date");
            }
            if (c?.IsDating == true && !c.NeedsReaffirmation && farm!.LastFlirtDay.GetValueOrDefault(name, -1) != Today) Add("flirt", "Share a romantic moment together");
            if (State.GetJourney(name, Today).CanPropose && !BrokenPromise(name)) Add("propose", "Ask to marry — offer a Mermaid's Pendant");
        }
        if (c?.IsEngaged == true) Add("cancel-engagement", "Cancel our engagement");
        return result.ToArray();
    }

    internal bool OpenActivity(string name, string key)
    {
        if (!Choices(name).Any(c => c.Key == key) || key is not ("romance:activity" or "romance:romantic-date" or "romance:repair-date")) return false;
        var npc = Game1.getCharacterFromName(name);
        return Beside(npc) && Dates.TryOpen(npc, key == "romance:romantic-date", key == "romance:repair-date");
    }
    internal QuestActionResult? Apply(string name, string key)
    {
        var choice = Choices(name).FirstOrDefault(c => c.Key == key);
        var npc = Game1.getCharacterFromName(name);
        if (string.IsNullOrEmpty(choice.Key) || !Beside(npc)) return null;
        bool applied;
        string fact;
        switch (key)
        {
            case "romance:friendship": applied = RomanceRules.ChooseFriendship(State, name, Today); fact = "We explicitly agreed to friendship only. Shared activities remain welcome."; break;
            case "romance:interest": applied = RomanceRules.ExpressInterest(State, name, Today); fact = "The farmer privately expressed interest in exploring romance. This is not mutual attraction or a commitment."; break;
            case "romance:dating": applied = Native.ApplyDating(npc); fact = "We explicitly agreed to date exclusively. Native dating status changed; no heart points were awarded."; if (applied) Signal(name, "dating"); break;
            case "romance:reaffirm": applied = RomanceRules.Reaffirm(State, name, Today); fact = "We reaffirmed our exclusive relationship. Earlier imported history remains unknown."; break;
            case "romance:discuss": applied = RomanceRules.DeliverWarning(State, name, Today); fact = Boundary(name) + " The farmer has now received the warning. No additional punishment was applied for discussing it."; break;
            case "romance:repair":
                RomanceRules.DeliverWarning(State, name, Today);
                applied = RomanceRules.AcknowledgeConflict(State, name, Today);
                fact = "The farmer acknowledged the repair agreement: two designated shared repair activities on different days and seven days respecting the romantic boundary. Words alone do not finish repair."; break;
            case "romance:missed": farm!.AcknowledgedNoShows[name] = State.Characters[name].NoShows; applied = true; fact = "We discussed the missed meeting and agreed we can make another plan. The disappointment remains in our history; this was not betrayal."; break;
            case "romance:flirt": farm!.LastFlirtDay[name] = Today; Signal(name, "flirt"); applied = true; fact = "We explicitly shared a reciprocated romantic moment. This did not create a new commitment."; break;
            case "romance:propose": applied = Native.TryPropose(npc, out string reason); fact = applied ? "The proposal was accepted. One eligible Pendant was consumed and the native wedding was scheduled." : reason; if (applied) Signal(name, "proposal"); break;
            case "romance:cancel-engagement": applied = Native.CancelEngagement(npc); fact = "We explicitly cancelled the engagement. This was not a divorce. Our shared history remains."; break;
            default: return null;
        }
        if (!applied) { Game1.addHUDMessage(new HUDMessage(fact, HUDMessage.error_type)); return null; }
        Memory(name).Experiences.Record(key, "relationship", Today, fact, "romance friendship trust commitment repair");
        return new(choice.Label, fact, fact);
    }
    private static bool Beside(NPC? npc) => Supported(npc) && npc!.currentLocation == Game1.player.currentLocation
        && Vector2.Distance(npc.Tile, Game1.player.Tile) <= 3 && !Game1.eventUp && !npc.isSleeping.Value;

    internal void Signal(string name, string kind)
    {
        if (!Ready) return;
        string id = "romance-" + Guid.NewGuid().ToString("N");
        if (!RomanceRules.RecordIncident(State, id, name, Today, kind)) return;
        foreach (NPC witness in Game1.currentLocation.characters.Where(n => n.Name != name && Social(n)))
            if (CanSee(witness, Game1.player.Tile, 8)) RomanceRules.LearnIncident(State, id, witness.Name, Today, witness.Name);
    }
    private static bool Social(NPC npc) => npc.CanSocialize && !npc.IsInvisible && !npc.isSleeping.Value;
    internal static bool CanSee(NPC npc, Vector2 target, int range)
    {
        if (!Social(npc) || Vector2.Distance(npc.Tile, target) > range) return false;
        Vector2 start = npc.Tile;
        int steps = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(start, target) * 2));
        for (int i = 1; i < steps; i++)
        {
            Vector2 tile = Vector2.Lerp(start, target, i / (float)steps);
            if (npc.currentLocation.isCollidingPosition(new Rectangle((int)tile.X * 64 + 16, (int)tile.Y * 64 + 16, 24, 24), Game1.viewport, false, 0, false, npc, true, false, false)) return false;
        }
        return true;
    }
    private void SpreadNews()
    {
        Utility.ForEachLocation(location =>
        {
            var people = location.characters.Where(Social).ToArray();
            foreach (NPC sender in people)
            {
                if (State.ReporterLastDay.GetValueOrDefault(sender.Name, -1) == Today) continue;
                foreach (NPC receiver in people.Where(n => n != sender && CanSee(sender, n.Tile, 2)))
                {
                    var report = State.Knowledge.FirstOrDefault(k => k.KnowerNpc == sender.Name && k.Hops < 2 && !State.Knowledge.Any(r => r.KnowerNpc == receiver.Name && r.IncidentId == k.IncidentId));
                    if (report != null && RomanceRules.SendReport(State, sender.Name, receiver.Name, report.IncidentId, Today)) break;
                }
            }
            return true;
        });
    }
    private static string WarningKey(string name, RomanceCharacterState c) => $"SolaceRomance_{name}_{c.LastViolationSequence}";
    private static string Boundary(string name) => $"{name}: We agreed to be exclusive. I learned about a romantic action with someone else, and it hurt. Ordinary friendships are not the problem. I need you to respect the commitment we made.";
    private void QueueWarnings()
    {
        foreach (var pair in State.Characters.Where(p => p.Value.InConflict && p.Value.WarningDeliveredDay == null && p.Value.LastViolationDay < Today))
        {
            string key = WarningKey(pair.Key, pair.Value);
            farm!.Notices[key] = Boundary(pair.Key) + "^We need to discuss what happens next. Another romantic boundary violation after this warning means time apart.^-" + pair.Key + "[#]A relationship boundary";
            while (farm.Notices.Count > 128) farm.Notices.Remove(farm.Notices.Keys.First());
            if (!Game1.player.hasOrWillReceiveMail(key)) Game1.player.mailbox.Add(key);
        }
        helper.GameContent.InvalidateCache("Data/mail");
    }
    internal void OpenJournal(string name)
    {
        if (!Ready) return;
        selected = RomanceRules.IsCandidate(name) ? name : "Abigail";
        Game1.activeClickableMenu = new RomanceJournalMenu(State, selected, Describe, () => { if (OpenCharacterTree != null) OpenCharacterTree(selected); else abigail.TreeService?.OpenTree(); });
    }
    internal string Describe(string name)
    {
        var c = State.Characters.GetValueOrDefault(name);
        string text = $"{name}\n{State.GetJourney(name, Today).BondStage}\n";
        text += c?.FriendshipOnly == true ? "Friendship only.\n" : c?.InterestExpressed == true ? "Interested in something more.\n" : "Intentions have not been discussed.\n";
        text += "Shared time and reliable actions develop this bond. New dating needs four completed activities of two kinds, twelve interaction days and 28 observed days.\n";
        if (c != null) text += $"Shared activities: {c.CompletedActivities}; kinds: {c.ActivityTypes.Count}. Interaction days: {c.TalkDays}.\n";
        if (State.Booking is { } b) text += $"Farm-wide plan: {b.Npc}, {b.Type}, {AbigailDeliveryQuest.DateLabel(b.Day)}, {b.StartMinute / 60:00}:{b.StartMinute % 60:00}. {(b.Repair ? "Repair" : b.Romantic ? "Romantic" : "Friendship")}.\n";
        if (c?.InConflict == true) text += Boundary(name) + "\nRepair: agree to the boundary, complete two repair activities on different days, then seven days without another violation.\n";
        if (c?.SeparationUntilDay is int deadline) text += $"Time apart ends {AbigailDeliveryQuest.DateLabel(deadline)}. Romantic dates and assistance are paused.\n";
        if (c?.NeedsReaffirmation == true) text += "Your existing native relationship is preserved. Discuss exclusivity before the new boundary rules apply.\n";
        if (c?.IsDating == true) text += "Established couple: 28 dating days and four romantic dates. Proposal: 56 dating days, eight dates and fourteen conflict-free days.\n";
        text += "\nShared history\n" + string.Join("\n", State.History.Where(h => h.Npc == name).TakeLast(16).Select(h => AbigailDeliveryQuest.DateLabel(h.Day) + ": " + h.Kind));
        if (farm!.Memories.TryGetValue(name, out var memory)) text += "\n" + string.Join("\n", memory.Experiences.Entries.TakeLast(8).Select(e => e.Fact));
        return text;
    }
    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception ex) { monitor.Log("Global relationships paused after a runtime error: " + ex.Message, LogLevel.Error); Dates.CancelForTransition(); farm = null; }
    }
}
