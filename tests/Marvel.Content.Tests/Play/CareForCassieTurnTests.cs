using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CareForCassieTurnTests
{
    [Fact]
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:cannot")]
    public void ActualDiscardedObligationLastsThroughScottsNextTurnAndNoLonger()
    {
        // Lasting effects expire at their specified point; "cannot" is absolute.
        // Prepared pre-turn reveal isolates the actual row and real turn progression.
        var world = CareForCassieFixture.Board("12001a", 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var game = Game.Begin(world, world.Facts, runner);
        var card = new RedSkullFixture(world, runner).Reveal("12025", 0);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(1), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(1), []);
        var selection = Sequence.Work(world, world.Facts, runner, []);
        if (selection is not null)
        {
            var offer = Assert.Single(selection.Affordances);
            Sequence.Answer(world, world.Facts, runner, selection,
                new Decision(offer.Id, [world.Seats[1].Hand.Cards[0].ObjectId], []), []);
        }
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        for (int player = 0; player < 2; player++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                offer => offer.Verb == Game.ResolveMulligans).Id));
        Assert.Equal(0, game.Active);
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[1].IdentityCard));
        game.Resolve(Decision.Decline);
        Assert.Equal(1, game.Active);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[1].IdentityCard));
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.Verb == Game.ChangeForm);
        game.Resolve(Decision.Decline);
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[1].IdentityCard));
        Assert.False(world.IsOver);
    }
}
