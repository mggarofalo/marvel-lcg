using Marvel.Rules.Events;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

public sealed class EventCueCopyTests : EventPresentationTestBase
{
    [Theory]
    [InlineData("health", 11, 9, "HP 11 → 9")]
    [InlineData("k_threat", 2, 5, "threat 2 → 5")]
    [InlineData("damage", 0, 2, "damage 0 → 2")]
    public void NumericCuesRetainTheOccurrenceSubjectAndBothValues(
        string field, long before, long after, string change)
    {
        var happened = new FieldSet(9, field, before, after)
        {
            Subjects = new Dictionary<int, string> { [9] = "Ultron" },
        };
        EventPresentation cue = EventPresenter.Present(happened, World());
        Assert.Equal("Ultron · " + change, cue.CueSummary);
        Assert.Contains("Ultron", cue.Summary);
        Assert.DoesNotContain("Rhino", cue.CueSummary);
    }

    [Fact]
    public void AbsentValuesAreNotInventedAsZero()
    {
        var cue = EventPresenter.Present(new FieldSet(9, "health", null, 17), World());
        Assert.Equal(cue.Summary, cue.CueSummary);
        Assert.DoesNotContain("0 →", cue.CueSummary);
    }

    [Fact]
    public void BoostCueKeepsTheAuthorizedOccurrenceName()
    {
        var happened = new CardsFlipped([12], true)
        {
            Verb = "Boost",
            Subjects = new Dictionary<int, string> { [12] = "Android Efficiency" },
        };
        var cue = EventPresenter.Present(happened, World());
        Assert.Equal("Boost · Android Efficiency", cue.CueSummary);
        Assert.Contains("boost", cue.Summary);
    }

    [Fact]
    public void UnnamedConcealedCardDoesNotAcquireAFaceFromTheCue()
    {
        var cue = EventPresenter.Present(new CardsFlipped([12], false), World());
        Assert.DoesNotContain("Android", cue.CueSummary);
        Assert.Contains("face-down", cue.CueSummary);
    }
}
