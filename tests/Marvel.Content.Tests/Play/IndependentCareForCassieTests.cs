using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual card on a prepared research board; this does not establish public pack admission.
public sealed class IndependentCareForCassieTests
{
    [Fact]
    [Rule("rr:ability.7.1")]
    [Rule("rr:choose-game-element.1")]
    [Rule("rr:form-change-form.2")]
    public void OptionalFlipStillAllowsDiscardInsteadOfExhaustAndHonorsTheChosenCard()
    {
        // The part following "may" is optional; "the player resolving the ability" makes its choice.
        // Changing form retains the identity's "current state (ready or exhausted)".
        var world = CareForCassieFixture.Board("12001c");
        var runner = AuthoredCards.Runner();
        var scott = world.Seats[1];
        var retained = scott.Hand.Cards[0];
        var selected = scott.Hand.Cards[1];
        var card = new RedSkullFixture(world, runner).Reveal("12025", 0);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(0), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal("12001b", scott.IdentityCard.FaceId);
        Assert.True(scott.IdentityCard.Ready);
        Assert.Equal(1, choice.Player);
        Assert.Equal(2, choice.Affordances.Count);
        Assert.Contains(choice.Affordances, item => item.Id == 0);
        Assert.Contains(choice.Affordances, item => item.Id == 1);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(1), []);
        var discard = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, discard.Player);
        Sequence.Answer(world, world.Facts, runner, discard,
            new Decision(Assert.Single(discard.Affordances).Id, [selected.ObjectId], []), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Same(retained, Assert.Single(scott.Hand.Cards));
        Assert.Equal(DeckType.DiscardPile, selected.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Equal("12001b", scott.IdentityCard.FaceId);
        Assert.True(scott.IdentityCard.Ready);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.False(FormChangePermissions.CanChange(world, scott.IdentityCard));
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
    }
}
