using Marvel.Rules.Fold;
using Xunit;

namespace Marvel.Rules.Tests.Fold;

/// <summary>
/// The fold's input, and the one distinction it exists to carry.
/// </summary>
/// <remarks>
/// Declining is not nothing: it is the only input the recorded fixtures
/// exercise, and answering the main-turn prompt with one ends the turn. The
/// Python engine spells it as the empty command <c>{}</c>; the risk in porting
/// that is a decline arriving as "affordance 0", which is a real affordance id.
/// </remarks>
public sealed class DecisionTests
{
    [Fact]
    public void DecliningTakesNothing()
    {
        Assert.True(Decision.Decline.IsDecline);
        Assert.Empty(Decision.Decline.Targets);
    }

    [Fact]
    public void AffordanceZeroIsNotADecline()
    {
        // The whole point of the type. Ids start at 0, so a decline modelled as
        // a falsy id would silently take the first option on offer.
        var first = Decision.Take(0);
        Assert.False(first.IsDecline);
        Assert.Equal(0, first.Affordance);
    }

    [Fact]
    public void TargetsKeepTheOrderTheyWereChosenIn()
    {
        // Several rules care which was chosen first -- the order minions
        // activate in, the order cards go back on top of a deck -- so this is
        // a sequence and not a set.
        var decision = new Decision(4, [9, 2, 7]);
        Assert.Equal([9, 2, 7], decision.Targets);
    }
}
