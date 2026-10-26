using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic permission tests the ordinary decision boundary without pack admission.</summary>
public sealed class DiscardPlayDecisionTests
{
    private static AbilityRunner Permission() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05018","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},
          "effect":{"allowPlayFromDiscard":"this"}
        }]}]}
        """));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecondSeatResolvesOrdinaryDiscardPlayWithPrintedCostAndDiscount(bool discount)
    {
        // Printed 05018: "during your turn (paying his resource cost)"; cost is 4.
        Card ally = null!;
        int[] paying = [];
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("05018", Pile(board, 1));
            paying = Enumerable.Range(0, 4).Select(_ => board.CreateCard("01093", board.Seats[1].Hand).ObjectId).ToArray();
            if (discount) CardPayment.ReduceNextCardCost(board, board.Seats[1].IdentityCard, 1, 1);
        }, heroes: ["spider_man", "captain_marvel"], abilities: Permission());
        Assert.DoesNotContain(game.PromptFor(1)?.Affordances ?? [], item => item.AnchorId == ally.ObjectId);
        game.Resolve(Decision.Decline);
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == ally.ObjectId);
        Assert.Equal(1, game.Pending.Player);
        var result = game.Resolve(PaidDecision.Take(offer, paying.Take(discount ? 3 : 4).ToArray()) with { Targets = [world.Seats[1].IdentityCard.ObjectId] });
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(1), ally.Area.PlayArea);
        Assert.Equal(1, game.Pending!.Player);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(result.IsOver);
        Assert.All(paying.Take(discount ? 3 : 4), id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
        if (discount) Assert.Same(world.Seats[1].Hand, world.Cards[paying[3]].Area);
    }

    [Theory]
    [InlineData("foreign-target")]
    [InlineData("self-payment")]
    [InlineData("deck")]
    [InlineData("foreign-owner")]
    public void RejectedPublishedPlayPreservesInformationAndPendingState(string invalid)
    {
        Card ally = null!;
        int[] paying = [];
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("05018", Pile(board, 0));
            paying = Enumerable.Range(0, 4).Select(_ => board.CreateCard("01093", board.Seats[0].Hand).ObjectId).ToArray();
        }, heroes: ["spider_man", "captain_marvel"], abilities: Permission());
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
        int target = world.Seats[invalid == "foreign-target" ? 1 : 0].IdentityCard.ObjectId;
        var valid = PaidDecision.Take(offer, paying) with { Targets = [target] };
        if (invalid == "deck") World.MoveToTop(ally, world.Seats[0].Deck);
        if (invalid == "foreign-owner") ally.TransferLinkedOwnership(1);
        if (invalid == "self-payment") paying = [ally.ObjectId, .. paying.Take(3)];
        world.RecordInformation(InformationKind.Reveal);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        var answer = invalid == "self-payment" ? valid with { Resources = paying } : valid;
        Assert.Throws<ArgumentException>(() => game.Resolve(answer));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Single(new Resolution(world, game.Pending, []).Information, item => item.Kind == InformationKind.Reveal);

        if (invalid == "foreign-owner") ally.TransferLinkedOwnership(0);
        if (invalid == "deck") World.MoveToTop(ally, Pile(world, 0));
        var retry = PaidDecision.Take(offer, valid.Spent) with { Targets = [world.Seats[0].IdentityCard.ObjectId] };
        game.Resolve(retry);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ally-limit")]
    public void SecondSeatAllyLimitCompletesBeforeSyntheticEntryResponse(bool discardOrigin)
    {
        // "This occurs before abilities that resolve upon entering play."
        // The draw response is synthetic; printed Lockjaw has only the permission.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05018","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"allowPlayFromDiscard":"this"}
            },{
              "trigger":{"timing":"ForcedResponse","event":"WhenCardEntersPlay","subject":"this"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """));
        Card ally = null!;
        Card chosen = null!;
        int[] paying = [];
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("05018", discardOrigin ? Pile(board, 1) : board.Seats[1].Hand);
            var area = board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1);
            chosen = board.CreateCard("01067", area);
            board.CreateCard("01068", area);
            board.CreateCard("01070", area);
            paying = Enumerable.Range(0, 4).Select(_ => board.CreateCard("01093", board.Seats[1].Hand).ObjectId).ToArray();
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        game.Resolve(Decision.Decline);
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == ally.ObjectId);
        int held = world.Seats[1].Hand.Cards.Count;
        game.Resolve(PaidDecision.Take(offer, paying) with { Targets = [world.Seats[1].IdentityCard.ObjectId] });
        int afterPayment = held - (discardOrigin ? 4 : 5);
        Assert.Equal(afterPayment, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(1, game.Pending!.Player);
        Assert.False(game.Pending.Cancellable);
        Assert.False(game.IsRootPrompt);
        Assert.Equal(4, ally.Area.Cards.Count);
        var discard = Assert.Single(game.Pending.Affordances, item => item.AnchorId == chosen.ObjectId);
        var result = game.Resolve(Decision.Take(discard.Id));
        Assert.Equal(DeckType.DiscardPile, chosen.Area.Type);
        Assert.Equal(3, ally.Area.Cards.Count);
        Assert.Equal(afterPayment + 1, world.Seats[1].Hand.Cards.Count);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(result.IsOver);
    }

    private static Area Pile(World world, int player) => world.AreaOf(
        DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);
}



