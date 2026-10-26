using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent published-payment rejection controls for discard play.</summary>
public sealed class DiscardPlayFinalReviewTests
{
    [Theory]
    [InlineData("short")]
    [InlineData("duplicate")]
    [InlineData("unique")]
    [InlineData("stale-payment")]
    [InlineData("stale-generator")]
    [InlineData("stale-cost")]
    [InlineData("stale-allocation")]
    public void RejectedAnswerRetainsInformationAndCanBeRetried(string invalid)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05018","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"allowPlayFromDiscard":"this"}
            }]},{"card":"01063","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"generate":"G"}
            }]}]}
            """));
        Card ally = null!;
        Card? generator = null;
        ContinuousEffects.Registration? reduction = null;
        int[] paying = [];
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("05018", board.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            paying = Enumerable.Range(0, 4).Select(_ => board.CreateCard(
                "01093", board.Seats[0].Hand).ObjectId).ToArray();
            if (invalid == "stale-generator")
            {
                generator = board.CreateCard("01063", board.AreaOf(
                    DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                paying = [.. paying.Take(3), generator.ObjectId];
            }
            if (invalid == "stale-cost")
            {
                reduction = CardPayment.ReduceNextCardCost(board, ally, 0, 1);
                paying = paying.Take(3).ToArray();
            }
        }, abilities: runner);
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
        var answer = PaidDecision.Take(offer, paying) with
        { Targets = [world.Seats[0].IdentityCard.ObjectId] };
        Assert.True(DecisionAssessment.Assess(prompt, answer).IsAccepted);
        if (invalid == "short") answer = answer with { Resources = paying.Take(3).ToArray() };
        if (invalid == "duplicate") answer = answer with
        { Resources = [paying[0], paying[0], paying[2], paying[3]] };
        var restore = InvalidatePublishedBoard(invalid, world, ally, paying, generator, reduction);
        world.RecordInformation(InformationKind.Reveal);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        var rejected = Record.Exception(() => game.Resolve(answer));
        Assert.NotNull(rejected);
        Assert.Same(prompt, game.Pending);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Single(new Resolution(world, game.Pending, []).Information,
            item => item.Kind == InformationKind.Reveal);
        Assert.IsType<ArgumentException>(rejected);
        restore();
        game.Resolve(PaidDecision.Take(offer, paying) with { Targets = answer.Targets });
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static Action InvalidatePublishedBoard(
        string invalid, World world, Card ally, int[] paying, Card? generator,
        ContinuousEffects.Registration? reduction)
    {
        // Keep each stale-board mutation paired with the correction used by the
        // accepted retry. Short and duplicate answers leave the board untouched.
        switch (invalid)
        {
            case "unique":
                var blocker = world.CreateCard("05018", world.AreaOf(
                    DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                return () => World.MoveToTop(blocker, world.Seats[0].Deck);
            case "stale-payment":
                World.MoveToTop(world.Cards[paying[0]], world.Seats[0].Deck);
                return () => World.MoveToTop(world.Cards[paying[0]], world.Seats[0].Hand);
            case "stale-generator":
                generator!.Exhaust();
                return generator.Refresh;
            case "stale-cost":
                reduction!.Dispose();
                return () => CardPayment.ReduceNextCardCost(world, ally, 0, 1);
            case "stale-allocation":
                var added = CardPayment.ReduceNextCardCost(world, ally, 0, 1);
                return added.Dispose;
            default:
                return () => { };
        }
    }
}
