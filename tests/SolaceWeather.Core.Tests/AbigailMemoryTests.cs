using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class AbigailMemoryTests
{
    [Fact]
    public void OnlyObservedLinesAreStoredWithBoundedDistinctRecall()
    {
        var memory = new AbigailMemory();
        memory.RememberLine(2, "A line actually shown");
        memory.RememberLine(2, "A line actually shown");
        Assert.Single(memory.Days[0].SpokenLines);
        for (int i = 0; i < 20; i++) memory.RememberLine(2, $"Line {i}");
        Assert.Equal(8, memory.Days[0].SpokenLines.Count);
        Assert.False(memory.Days[0].Talked); // Seeing text does not grant native conversation credit.
    }
    [Fact]
    public void ObservationsAreIdempotentAndDoNotInventConversation()
    {
        var memory = new AbigailMemory();
        memory.Observe(2, false, 0);
        Assert.Empty(memory.Days);
        memory.Observe(2, true, 1);
        memory.Observe(2, true, 1);
        Assert.Single(memory.Days);
        Assert.Equal(1, memory.Days[0].Gifts);
        Assert.True(memory.Days[0].Talked);
    }

    [Fact]
    public void SaveRestoresAndNewFarmIsEmpty()
    {
        var memory = new AbigailMemory();
        memory.Observe(3, true, 2);
        var restored = JsonSerializer.Deserialize<AbigailMemory>(JsonSerializer.Serialize(memory))!;
        restored.Observe(3, true, 2);
        Assert.Single(restored.Days);
        Assert.Equal(2, restored.Days[0].Gifts);
        Assert.Empty(new AbigailMemory().Days);
    }

    [Fact]
    public void RetentionIsBoundedAndDoesNotRecordEmptyDays()
    {
        var memory = new AbigailMemory();
        for (int day = 0; day < 200; day++) memory.Observe(day, true, 0);
        Assert.Equal(112, memory.Days.Count);
        Assert.Equal(88, memory.Days[0].Day);
        memory.Observe(201, false, 0);
        Assert.Equal(112, memory.Days.Count);
    }

    [Fact]
    public void UnsupportedOrMalformedStateCannotBeSilentlyRewritten()
    {
        Assert.False(new AbigailMemory { Version = 99 }.IsValid());
        Assert.False(new AbigailMemory { Days = null! }.IsValid());
        var memory = new AbigailMemory();
        memory.Days.Add(new AbigailDay { Day = -1 });
        Assert.False(memory.IsValid());
    }
}
