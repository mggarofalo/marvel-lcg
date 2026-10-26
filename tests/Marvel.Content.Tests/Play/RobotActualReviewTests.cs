using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RobotActualReviewTests
{
    [Fact]
    [Rule("rr:resource.2")]
    public void ActualRobotAcceptsAWildResourceThroughGameResolve()
    {
        // "Wild resources can be used as their type or any of the other types."
        Card? robot = null;
        Card? payment = null;
        var runner = AuthoredCards.Runner();
        var (game, world) = Playing(board =>
        {
            robot = board.CreateCard("05028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            payment = board.CreateCard("01011", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        Assert.Equal([Resources.Wild], Resources.GeneratedBy(payment!.FaceId, world.Facts));
        var offer = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == robot!.ObjectId);

        game.Resolve(PaidDecision.Take(offer, [payment.ObjectId]));

        Assert.True(PrintedText.IsBlanked(world, robot!));
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.True(runner.CanTakeDamage(world, robot!, world.Seats[0].IdentityCard));
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == robot!.ObjectId);
    }

    [Fact]
    public void ActualRobotOfferExplainsBlankingAndItsPhaseDuration()
    {
        // Engine descriptions are a repository product contract, rather than a rule.
        Card? robot = null;
        var (game, _) = Playing(board =>
        {
            robot = board.CreateCard("05028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01089", board.Seats[0].Hand);
        }, hero: true, abilities: AuthoredCards.Runner());
        var offer = Assert.Single(game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == robot!.ObjectId);
        Assert.Contains("blank", offer.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("phase", offer.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("printedTextBlanked", offer.Description);
        Assert.Contains("blank", offer.DisplayLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("phase", offer.CommitLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ActualRobotPersistentActionDoesNotExposeAnInternalNumericField()
    {
        Card? robot = null;
        var (_, world) = Playing(board =>
        {
            robot = board.CreateCard("05028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01089", board.Seats[0].Hand);
        }, hero: true, abilities: AuthoredCards.Runner());
        var facts = PersistentCardFacts.Describe(world, robot!);
        Assert.NotNull(facts);
        var action = Assert.Single(facts.Abilities, ability => ability.Trigger.Timing == "Action");
        Assert.NotEmpty(action.Effects);
        Assert.DoesNotContain(action.Effects, effect => effect.Field == "printedTextBlanked");
    }
}
