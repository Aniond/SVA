using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class QuestTextTests
{
    [Fact]
    public void HighlightsWholeItemNamesWithoutColoringUnrelatedWords()
    {
        const string text = "A fish, a SARDINE, and a largemouth bass. Fishing isn't selfish.";
        var ranges = QuestText.FindItems(text, new[] { "fish", "sardine", "largemouth bass" });
        Assert.Equal(new[] { "fish", "SARDINE", "largemouth bass" }, ranges.Select(r => text.Substring(r.Start, r.Length)));
    }

    [Fact]
    public void OverlappingNamesAreMergedAndEmptyNamesIgnored()
    {
        var ranges = QuestText.FindItems("Give a Rainbow Trout, then a Trout.", new[] { "", "Trout", "Rainbow Trout", "Trout" });
        Assert.Equal(2, ranges.Count);
        Assert.Equal("Rainbow Trout".Length, ranges[0].Length);
        Assert.Empty(QuestText.FindItems("Hello!", Array.Empty<string>()));
    }
}
