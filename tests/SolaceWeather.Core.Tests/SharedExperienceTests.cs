using System.Text.Json;
using SolaceWeather.Core;
using Xunit;

namespace SolaceWeather.Core.Tests;
public class SharedExperienceTests
{
    [Fact] public void RepeatedObservationsDoNotDuplicateAndLaterExperiencesAccumulate()
    {
        var store = new SharedExperienceStore();
        Assert.True(store.Record("flute", "music", 4, "A flute break happened.", "flute music rest"));
        Assert.False(store.Record("flute", "music", 4, "A flute break happened.", "flute music rest"));
        Assert.True(store.Record("flute", "music", 11, "Another flute break happened.", "flute music rest"));
        var memory = Assert.Single(store.Entries);
        Assert.Equal(2, memory.Occurrences); Assert.Equal(4, memory.FirstDay); Assert.Equal(11, memory.LastDay);
    }
    [Fact] public void RelatedOldExperienceOutranksRecentUnrelatedOne()
    {
        var store = new SharedExperienceStore();
        store.Record("flute", "music", 1, "Listened to the flute.", "flute music rest");
        store.Record("kit", "adventure", 20, "Received supplies.", "mines adventure supplies");
        Assert.Equal("flute", store.Select("That music helped me relax", 20).First().Id);
    }
    [Fact] public void ConversationIsSeparateFromVerifiedFactAndUnknownIdsCannotCreateEvents()
    {
        var store = new SharedExperienceStore(); store.Record("study", "curiosity", 2, "Showed a mineral; kept it.", "mineral");
        Assert.False(store.Reflect("invented", 2, "I killed a dragon", "Amazing."));
        Assert.True(store.Reflect("study", 2, "I found it fighting dragons", "You say you found it fighting dragons?"));
        Assert.Equal("Showed a mineral; kept it.", store.Entries[0].Fact);
        Assert.Single(store.Entries[0].Conversations);
        Assert.False(store.Reflect("study", 2, "I found it fighting dragons", "You say you found it fighting dragons?"));
    }
    [Fact] public void ExistingTreeMigrationIsVerifiedAndIdempotent()
    {
        var memory = new AbigailMemory();
        memory.Promises.Offer("fish", 1); memory.Promises.Accept("fish", 1, 0, false); memory.Promises.Complete("fish", "(O)131", "Sardine", 2);
        memory.Tree.Observe(memory.Promises, 3, Array.Empty<int>());
        memory.Experiences.Observe(memory.Promises, memory.Tree); memory.Experiences.Observe(memory.Promises, memory.Tree);
        Assert.Single(memory.Experiences.Entries.Where(e => e.Id == "promise:fish:completed"));
        Assert.Empty(memory.Experiences.Entries.Where(e => e.Id.StartsWith("study:")));
        Assert.DoesNotContain(memory.Experiences.Entries, e => e.Kind == "adventure");
    }
    [Fact] public void SaveRestorationPreservesHistoryAndSeparateFarmsStaySeparate()
    {
        var memory = new AbigailMemory(); memory.Experiences.Record("flute", "music", 4, "Listened to music.", "music");
        memory.Experiences.Reflect("flute", 4, "That helped", "I'm glad.");
        var restored = JsonSerializer.Deserialize<AbigailMemory>(JsonSerializer.Serialize(memory))!;
        Assert.True(restored.IsValid()); Assert.Single(restored.Experiences.Entries[0].Conversations);
        Assert.Empty(new AbigailMemory().Experiences.Entries);
        Assert.True(JsonSerializer.Deserialize<AbigailMemory>("{\"Version\":1}")!.IsValid());
    }
    [Fact] public void MigrationCannotRewriteKnownKitOrInferOldContentsFromNewApproach()
    {
        var tree = new RelationshipTreeState(); var ledger = new PromiseLedger();
        ledger.ObserveMaterials(new[] { "quartz", "iron" });
        foreach (var id in new[] { "fish", "quartz", "iron" })
        {
            ledger.Offer(id, 0); ledger.Accept(id, 0, id == "fish" ? 0 : 1, false);
            ledger.Complete(id, id == "fish" ? "(O)131" : PromiseLedger.Definition(id)!.ItemId, id, 0);
        }
        tree.Observe(ledger, 5, new[] { 0, 1, 2, 3, 4 }); tree.ChooseApproach("bold", 5); tree.Claim("kit", 5);
        var store = new SharedExperienceStore(); store.Observe(ledger, tree);
        Assert.Contains("does not identify its contents", store.Entries.Single(e => e.Id == "kit").Fact);
        var known = new SharedExperienceStore(); known.Record("kit", "adventure", 5, "Received Field Snacks and Torches.", "adventure");
        known.Observe(ledger, tree); Assert.Equal("Received Field Snacks and Torches.", known.Entries.Single(e => e.Id == "kit").Fact);
    }
    [Fact] public void RecentlyRecalledMemoryYieldsToAnotherUnlessDirectlyRelevant()
    {
        var store = new SharedExperienceStore();
        store.Record("flute", "music", 3, "Listened to flute.", "flute music");
        store.Record("study", "curiosity", 2, "Examined quartz.", "quartz mineral");
        store.Reflect("flute", 4, "Nice music", "Thanks.");
        Assert.Equal("study", store.Select("hello", 4).First().Id);
        Assert.Equal("flute", store.Select("flute music", 4).First().Id);
    }
    [Fact] public void LaterTopicsCanRetrieveTheConversationAttachedToAnOldExperience()
    {
        var store = new SharedExperienceStore();
        store.Record("flute", "music", 1, "Listened to flute.", "music");
        store.Reflect("flute", 1, "That reminds me of my grandmother", "You associate music with your grandmother.");
        for (int day = 2; day < 10; day++) store.Record("supplies" + day, "adventure", day, "Received supplies.", "mines");
        Assert.Equal("flute", store.Select("grandmother", 10).First().Id);
        Assert.Equal("Listened to flute.", store.Entries[0].Fact);
    }
    [Fact] public void CorruptFutureDataFailsClosed()
    {
        var store = new SharedExperienceStore { Version = 99 };
        Assert.False(store.IsValid()); Assert.Empty(store.Select("music", 4));
        Assert.False(store.Record("x", "music", 1, "x", "x"));
    }
}
