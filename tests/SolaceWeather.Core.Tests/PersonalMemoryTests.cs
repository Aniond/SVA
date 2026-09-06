using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class PersonalMemoryTests
{
    [Fact]
    public void ExistingFarmMemoryLoadsWithoutInventingPastActivity()
    {
        var old = JsonSerializer.Deserialize<AbigailMemory>("{\"Version\":1,\"Days\":[],\"Exchanges\":[]}")!;
        Assert.True(old.IsValid());
        Assert.Empty(old.Personal.Details);
        Assert.Equal("unknown", old.Personal.MineVisitStatus(10));
    }

    [Fact]
    public void FullSourcePreservesQualificationEvenWhenQuoteIsPartial()
    {
        var store = new PersonalMemoryStore();
        store.Apply(2, "I used to enjoy fishing, but not anymore.", new[] {
            new MemoryProposal { Topic = "fishing", Kind = "preference", Quote = "enjoy fishing" }
        });
        Assert.Contains("not anymore", store.Details[0].SourceMessage);
    }

    [Fact]
    public void RetentionIsBoundedAndInvalidVersionsAreRejected()
    {
        var store = new PersonalMemoryStore();
        for (int day = 0; day < 150; day++)
        {
            store.StartDay(day, true);
            store.Apply(day, "I enjoy this hobby.", new[] { new MemoryProposal { Topic = "hobby_" + day, Kind = "preference", Quote = "I enjoy this hobby." } });
        }
        Assert.Equal(64, store.Details.Count);
        Assert.Equal(112, store.Activities.Count);
        Assert.True(store.IsValid());
        store.Version = 99;
        Assert.False(store.IsValid());
    }

    [Fact]
    public void OnlyOneOfferedPlanIsAskedPerDay()
    {
        var store = new PersonalMemoryStore();
        store.Apply(1, "I will fish today. I will mine today.", new[] {
            new MemoryProposal { Topic = "fishing", Kind = "plan", Quote = "I will fish today.", Timing = "today" },
            new MemoryProposal { Topic = "mining", Kind = "plan", Quote = "I will mine today.", Timing = "today" }
        });
        store.MarkAsked(store.FollowUp(2)!.Topic, 2);
        Assert.Null(store.FollowUp(2));
        Assert.NotNull(store.FollowUp(3));
    }
    [Fact]
    public void OnlyActualFarmerWordsCanBecomeDetails()
    {
        var store = new PersonalMemoryStore();
        store.Apply(2, "I enjoy fishing.", new[] {
            new MemoryProposal { Topic = "fishing", Kind = "preference", Quote = "I enjoy fishing." },
            new MemoryProposal { Topic = "sword", Kind = "preference", Quote = "I love swords." }
        });
        Assert.Single(store.Details);
        Assert.Equal("I enjoy fishing.", store.Details[0].Quote);
    }

    [Fact]
    public void CorrectionsReplaceEarlierDetailAndSurviveSerialization()
    {
        var store = new PersonalMemoryStore();
        store.Apply(2, "I enjoy fishing.", new[] { new MemoryProposal { Topic = "fishing", Kind = "preference", Quote = "I enjoy fishing." } });
        store.Apply(4, "I don't enjoy fishing anymore.", new[] { new MemoryProposal { Topic = "fishing", Kind = "preference", Quote = "I don't enjoy fishing anymore." } });
        var restored = JsonSerializer.Deserialize<PersonalMemoryStore>(JsonSerializer.Serialize(store))!;
        Assert.Single(restored.Details);
        Assert.Contains("don't", restored.Details[0].Quote);
        Assert.True(restored.IsValid());
    }

    [Fact]
    public void TomorrowPlanOnlyBecomesFollowUpAfterPlannedDayAndIsNotRepeated()
    {
        var store = new PersonalMemoryStore();
        store.Apply(3, "I'm going mining tomorrow.", new[] { new MemoryProposal { Topic = "mining", Kind = "plan", Quote = "I'm going mining tomorrow.", Timing = "tomorrow" } });
        Assert.Null(store.FollowUp(4));
        var due = store.FollowUp(5)!;
        Assert.NotNull(due);
        store.MarkAsked(due.Topic, 5);
        Assert.Null(store.FollowUp(6));
    }

    [Fact]
    public void VisitsAreFactsButAbsenceNeedsCoverage()
    {
        var store = new PersonalMemoryStore();
        Assert.Equal("unknown", store.MineVisitStatus(2));
        store.StartDay(2, false);
        Assert.Equal("unknown", store.MineVisitStatus(2));
        store.VisitMine(2, 1100);
        Assert.Equal("visited", store.MineVisitStatus(2));
        store.StartDay(3, true);
        Assert.Equal("no recorded visit", store.MineVisitStatus(3));
    }

    [Fact]
    public void APlanOrClaimCannotCreateARealVisit()
    {
        var store = new PersonalMemoryStore();
        store.StartDay(3, true);
        store.Apply(3, "I went mining today.", new[] { new MemoryProposal { Topic = "mining", Kind = "personal", Quote = "I went mining today." } });
        Assert.Equal("no recorded visit", store.MineVisitStatus(3));
        Assert.Empty(new PersonalMemoryStore().Details);
    }
}
