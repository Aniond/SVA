using System.Text.Json;
using SolaceWeather.Core;

namespace SolaceWeather.Core.Tests;

public class PhoneTests
{
    private static PhoneState WithNumbers(long farmer = 0)
    {
        var state = new PhoneState { FarmerId = farmer };
        foreach (string name in new[] { "Abigail", "Sam" }) { state.TryOfferNumber(name, 0); state.AnswerNumberOffer(name, true, 0); }
        return state;
    }
    [Fact]
    public void ReusingShortcutCreatesNewRequestAndFreshReply()
    {
        var state = WithNumbers(7);
        var first = state.Send("Abigail", "  Hello!  ", 3, 900)!;
        Assert.Null(state.Send("Abigail", "duplicate click", 3, 900));
        Assert.True(state.Complete(first.Id, "Hello farmer!"));
        var second = state.Send("Abigail", state.Thread("Abigail").Shortcuts.Single(), 3, 910)!;
        Assert.NotEqual(first.Id, second.Id);
        Assert.True(state.Complete(second.Id, "How is your morning?"));
        Assert.Equal(4, state.Thread("Abigail").Messages.Count);
        Assert.Single(state.Thread("Abigail").Shortcuts);
        Assert.Equal("How is your morning?", state.Thread("Abigail").Messages.Last().Text);
    }

    [Fact]
    public void FailureRetryAndLateCompletionNeverDuplicateMessages()
    {
        var state = WithNumbers();
        var sent = state.Send("Abigail", "Hello", 1, 900)!;
        state.Fail(sent.Id);
        Assert.Null(state.Send("Abigail", "another", 1, 900));
        Assert.Same(sent, state.Retry("Abigail"));
        Assert.True(state.Complete(sent.Id, "Hey!"));
        Assert.False(state.Complete(sent.Id, "Duplicate"));
        Assert.Null(state.Retry("Abigail"));
        Assert.Equal(2, state.Thread("Abigail").Messages.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("line\nline")]
    [InlineData("\u0000")]
    public void RejectsInvalidInput(string value)
    {
        var state = WithNumbers();
        Assert.Null(state.Send("Abigail", value, 1, 900));
        Assert.Empty(state.Threads);
    }

    [Fact]
    public void SaveRoundTripIsFarmOwnedAndInterruptedRequestIsRetryable()
    {
        var state = WithNumbers(7);
        state.Send("Abigail", "I like rainy days.", 1, 900);
        var loaded = JsonSerializer.Deserialize<PhoneState>(JsonSerializer.Serialize(state))!;
        Assert.True(loaded.IsValid(7));
        Assert.False(loaded.IsValid(8));
        loaded.Interrupt();
        Assert.Equal("failed", loaded.Thread("Abigail").Messages.Single().Status);
        Assert.NotNull(loaded.Retry("Abigail"));
        Assert.Empty(new PhoneState { FarmerId = 8 }.Threads);
    }

    [Fact]
    public void RetentionAndProactiveRequestsAreBounded()
    {
        var state = WithNumbers();
        Assert.Null(state.Send("Lewis", "Hi", 1, 900));
        Assert.Null(state.Send("Abigail", new string('x', 501), 1, 900));
        for (int i = 0; i < 100; i++)
        {
            var message = state.Send("Abigail", "Hello " + i, 1, 900)!;
            state.Complete(message.Id, "Hey");
        }
        Assert.Equal(80, state.Thread("Abigail").Messages.Count);
        Assert.Equal(12, state.Thread("Abigail").Shortcuts.Count);
        Assert.False(state.TryInitiative("Abigail", 3)); // Do not stack unsolicited texts on unread replies.
        state.Thread("Abigail").Unread = false;
        Assert.True(state.TryInitiative("Abigail", 3));
        Assert.False(state.TryInitiative("Sam", 3));
        Assert.False(state.TryInitiative("Abigail", 4));
        Assert.True(state.TryInitiative("Sam", 4));
        Assert.True(state.IsValid(0));
    }
}
