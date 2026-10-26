using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
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
    public void RejectedAnswerRetainsInformationAndCanBeRetried(string invalid)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05018","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"allowPlayFromDiscard":"this"}
            }]}]}
            """));
        Card ally = null!;
        int[] paying = [];
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("05018", board.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            paying = Enumerable.Range(0, 4).Select(_ => board.CreateCard(
                "01093", board.Seats[0].Hand).ObjectId).ToArray();
        }, abilities: runner);
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
        var answer = PaidDecision.Take(offer, paying) with
        { Targets = [world.Seats[0].IdentityCard.ObjectId] };
        if (invalid == "short") answer = answer with { Resources = paying.Take(3).ToArray() };
        if (invalid == "duplicate") answer = answer with
        { Resources = [paying[0], paying[0], paying[2], paying[3]] };
        Card? blocker = invalid == "unique" ? world.CreateCard("05018", world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)) : null;
        if (invalid == "stale-payment") World.MoveToTop(world.Cards[paying[0]], world.Seats[0].Deck);
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
        if (blocker is not null) World.MoveToTop(blocker, world.Seats[0].Deck);
        if (invalid == "stale-payment") World.MoveToTop(world.Cards[paying[0]], world.Seats[0].Hand);
        game.Resolve(PaidDecision.Take(offer, paying) with { Targets = answer.Targets });
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
