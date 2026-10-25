using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ExhaustionCostReceiptTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.1")]
    public void AdmissionUsesThePossibleReceiptWithoutExhaustingItsMembers()
    {
        // Step 1 checks that "the effect of that ability can change the game state."
        // A synthetic damage effect makes zero-versus-positive receipt admission observable.
        var runner = Runner("01006", "Action", """
            {"dealDamage":{"cards":{"query":"villain"},"amount":{"cardsExhaustedForCost":"game"}}}
            """, cost: """
            {"exhaustChosen":{"from":{"query":"alliesYouControl"},"anyNumber":"true"}}
            """);
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            ally = board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        Assert.True(ally!.Ready);
        string digest = world.Digest().Canonical();
        Assert.Single(runner.Actions(world, 0), offer => offer.Card == source!.ObjectId);
        Assert.Equal(digest, world.Digest().Canonical());
        game.Resolve(new Decision(action.Id, [ally.ObjectId]));
        Assert.False(ally.Ready);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.DoesNotContain(runner.Actions(world, 0), offer => offer.Card == source!.ObjectId);
    }

    [Fact]
    [Rule("rr:cost.1")]
    public void ReceiptSurvivesPaidAlliesLeavingPlayBeforeTheEffectCountsThem()
    {
        // rr:cost.1: "pay cost resolve effect". A later departure does not
        // change which physical cards paid the already committed exhaustion.
        var runner = Runner("01006", "Action", """
            {"seq":[{"discard":{"last":{"query":"alliesYouControl"}}},
              {"forEach":{"count":{"cardsExhaustedForCost":"game"},
                "effect":{"draw":{"player":"you","count":1}}}}]}
            """, cost: """
            {"exhaustChosen":{"from":{"query":"alliesYouControl"},"anyNumber":"true"}}
            """);
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            ally = board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        int before = world.Seats[0].Hand.Cards.Count;
        game.Resolve(new Decision(action.Id, [ally!.ObjectId]));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(before + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [Rule("rr:initiating-abilities.step.5")]
    public void DrawsOnlyForSelectedCostCardsAndLeavesOtherAlliesAlone(int count)
    {
        // Step 5: "Pay all costs." The effect reads that receipt, not all
        // exhausted allies on the board. This synthetic support exercises the
        // Strength in Numbers cost/effect shape independently of pack admission.
        var runner = Runner("01006", "Action", """
            {"forEach":{"count":{"cardsExhaustedForCost":"game"},
              "effect":{"draw":{"player":"you","count":1}}}}
            """, cost: """
            {"exhaustChosen":{"from":{"query":"alliesYouControl"},"anyNumber":"true"}}
            """);
        Card? source = null;
        Card[] allies = [];
        Card? foreign = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            allies = [.. Enumerable.Range(0, 4).Select(_ => board.CreateCard("01002",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)))];
            allies[3].Exhaust();
            foreign = board.CreateCard("01020", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        Assert.Equal(3, action.Targets!.Max);
        Assert.DoesNotContain(allies[3].ObjectId, action.Targets.Legal);
        Assert.DoesNotContain(foreign!.ObjectId, action.Targets.Legal);
        int before = world.Seats[0].Hand.Cards.Count;
        game.Resolve(new Decision(action.Id, [.. allies.Take(count).Select(card => card.ObjectId)]));
        Assert.Equal(before + count, world.Seats[0].Hand.Cards.Count);
        Assert.All(allies.Take(count), card => Assert.False(card.Ready));
        Assert.All(allies.Skip(count).Take(3 - count), card => Assert.True(card.Ready));
        Assert.True(foreign.Ready);
    }
}
