using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorDefenderIndependentReviewTests : CoreCardTestBase
{
    [Fact]
    [Rule("ruling:6e301a347a5392fb")]
    public void EmptySearchCannotSpendConfusedEvenWithAThreatenedSideScheme()
    {
        // "fail to engage a minion, you cannot remove 3 threat or discard Confused".
        var world = ThorActualFixture.Board("06001a");
        ClearEncounterDeck(world);
        world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        var side = world.CreateCard("01129", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 5);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        var source = Play(world, 0);
        new RedSkullFixture(world, AuthoredCards.Runner()).Finish(_ =>
            throw new InvalidOperationException("Unpaid effect cannot choose a scheme"));
        Assert.Equal(5, side.Tokens["k_threat"]);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens[EncounterDeck.AccelerationToken]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:cost-arrow-icon")]
    public void LastEncounterCardRemainsTheCapturedMinionAcrossTheImmediateReset()
    {
        // "text before the cost arrow icon must be paid and/or resolved in full".
        var world = ThorActualFixture.Board("06001a");
        ClearEncounterDeck(world);
        world.CreateCard("01100", world.AreaOf(DeckType.EncounterDiscardPile));
        var found = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 7);
        Play(world, 0);
        new RedSkullFixture(world, AuthoredCards.Runner()).Finish(p => Decision.Take(main.ObjectId));
        Assert.Equal(DeckType.EngagedEnemiesArea, found.Area.Type);
        Assert.Equal(PlayArea.Of(0), found.Area.PlayArea);
        Assert.Equal(4, main.Tokens["k_threat"]);
        Assert.Equal(1, main.Tokens[EncounterDeck.AccelerationToken]);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:cost-arrow-icon.1")]
    [Rule("rr:quickstrike.1")]
    public void ActualThorResponseCanBeDeclinedAfterQuickstrikeBeforeThwartSelection()
    {
        // "Responses to the text preceding the cost arrow icon resolve before" the effect.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 7);
        world.CreateCard("01167", world.AreaOf(DeckType.EncounterDeck));
        Play(world, 0);
        int stage = 0;
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.Equal(7, main.Tokens["k_threat"]);
            if (prompt.Asking == Question.Defender)
            {
                Assert.Equal(0, stage++);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
            }
            else if (prompt.Affordances.Any(a => a.Timing == AbilityType.Response))
            {
                Assert.Equal(1, stage++);
                Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
            }
            else
            {
                Assert.Equal(2, stage++);
                Assert.Empty(world.Seats[0].Hand.Cards);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(main.ObjectId), []);
            }
        }
        Assert.Equal(3, stage);
        Assert.Equal(4, main.Tokens["k_threat"]);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("ruling:a1f4982cfebcd768")]
    public void SecondPlayersAcquiredPatrolExcludesMainAndKeepsPaymentAtActingSeat()
    {
        // "if paying the cost ... causes a minion with patrol to engage ... cannot initiate" main thwart.
        var world = new World(Cards, 2);
        for (int p = 0; p < 2; p++)
        {
            var seat = world.CreateSeat($"p{p}");
            seat.IdentityCard = world.CreateCard(p == 0 ? "01001a" : "06001a", seat.Hero);
            for (int i = 0; i < 8; i++) world.CreateCard("01088", seat.Deck);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 8; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 7);
        var side = world.CreateCard("01129", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 5);
        var patrol = world.CreateCard("16119", world.AreaOf(DeckType.EncounterDeck));
        var source = Play(world, 1);
        int choices = 0;
        new RedSkullFixture(world, AuthoredCards.Runner()).Finish(prompt =>
        {
            choices++;
            Assert.Equal(side.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
            return Decision.Take(side.ObjectId);
        });
        Assert.Equal(1, choices);
        Assert.Equal(PlayArea.Of(1), patrol.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, patrol.Area.Type);
        Assert.Equal(7, main.Tokens["k_threat"]);
        Assert.Equal(2, side.Tokens["k_threat"]);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(PlayArea.Of(1), source.Area.PlayArea);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static Card Play(World world, int player)
    {
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("06003", world.Seats[player].Hand);
        var action = Assert.Single(runner.Actions(world, player), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        return source;
    }

    private static void ClearEncounterDeck(World world)
    {
        foreach (var card in world.AreaOf(DeckType.EncounterDeck).Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.AsideDeck));
    }
}
