using Marvel.Rules.Play;
using Xunit;

namespace Marvel.View.Tests;

public sealed class InvocationAreaMeaningTests
{
    [Fact]
    public void GenericAdditionalAreasDoNotAcquireInvocationMeaningFromTheirZone()
    {
        var world = new WorldDescriptor([new PlayerDescriptor(1, "Owner", false)],
            [new AreaDescriptor(4, "AdditionalDeck", 1, -1, [], []),
             new AreaDescriptor(5, "AdditionalDiscardPile", 1, -1, [], [])], [], Outcome.Unfinished);
        var ordinary = BoardPresentation.From(world);
        Assert.DoesNotContain(ordinary.Areas, area => area.Title.StartsWith("INVOCATION", StringComparison.Ordinal));
        var typed = BoardPresentation.From(world with
            { InvocationDecks = [new InvocationDeckDescriptor(1, 4, 5)] });
        Assert.Equal("INVOCATION DECK", typed.Areas[0].Title);
        Assert.Equal("INVOCATION DISCARD", typed.Areas[1].Title);
        Assert.All(typed.Areas, area => Assert.Equal(1, area.Seat));
        Assert.DoesNotContain(ordinary.Areas, area => area.Title.StartsWith("INVOCATION", StringComparison.Ordinal));
    }
}
