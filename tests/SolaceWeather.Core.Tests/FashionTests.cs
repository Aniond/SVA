using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class FashionTests
{
    [Fact]
    public void OutfitValuesAreDeterministicAndNpcTastesDiffer()
    {
        var outfit = new[] { new FashionPiece("shirt", "(S)1", "Shirt", "112233", false,
            new FashionDefinition { FashionValue = 60, StyleTags = new[] { "practical" }, Practicality = 3 }) };
        var practical = new NpcFashionTaste { Standard = 50, PreferredTags = new[] { "practical" }, Practicality = 3 };
        var polished = new NpcFashionTaste { Standard = 70, PreferredTags = new[] { "polished" }, Practicality = 0 };
        Assert.True(FashionRules.Evaluate(outfit, practical).Score > FashionRules.Evaluate(outfit, polished).Score);
        Assert.Equal(FashionRules.Fingerprint(outfit), FashionRules.Fingerprint(outfit.AsEnumerable().Reverse()));
        Assert.NotEqual(FashionRules.Fingerprint(outfit), FashionRules.Fingerprint(new[] { outfit[0] with { Dye = "ffffff" } }));
        Assert.Equal(FashionRules.Fingerprint(new[] { outfit[0] with { Prismatic = true } }),
            FashionRules.Fingerprint(new[] { outfit[0] with { Dye = "ffffff", Prismatic = true } }));
        Assert.Equal(50, FashionRules.Evaluate(new[] { outfit[0] with { Definition = null } }, practical).Score);
    }
    [Fact]
    public void OnlyObservedOutfitsCanBeRecalledAndCommentsDoNotRepeat()
    {
        var state = new FashionMemoryState { FarmerId = 7 };
        Assert.Null(state.LastSeen("Haley"));
        state.Observe("Haley", "one", "blue shirt", 1);
        Assert.True(state.CanComment("Haley", "one", 1, 3));
        state.MarkComment("Haley", "one", 1);
        Assert.False(state.CanComment("Haley", "one", 5, 3));
        state.Observe("Haley", "two", "red shirt", 2);
        Assert.False(state.CanComment("Haley", "two", 2, 3));
        Assert.True(state.CanComment("Haley", "two", 4, 3));
        Assert.Null(state.LastSeen("Emily")); Assert.Equal("red shirt", state.LastSeen("Haley")!.Description);
        Assert.True(state.IsValid(7)); Assert.False(state.IsValid(8));
    }
}
