using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorDefenderCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("ruling:6e301a347a5392fb")]
    public void UnpaidEngagementDiscardsEventWithoutThreatRemovalOrConfused(bool blockedEntry)
    {
        // "you must discard a minion ... & engage it before you can remove 3 threat or remove Confused."
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 6);
        foreach (var card in world.AreaOf(DeckType.EncounterDeck).Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.AsideDeck));
        world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        Card? found = null;
        if (blockedEntry)
        {
            world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            found = world.CreateCard("01167", world.AreaOf(DeckType.EncounterDeck));
        }
        Statuses.Give(world, seat.IdentityCard, Statuses.Confused);
        var source = world.CreateCard("06003", seat.Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        new RedSkullFixture(world, runner).Finish();
        Assert.Equal(6, main.Tokens["k_threat"]);
        Assert.True(Statuses.Has(world, seat.IdentityCard, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(1, main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        if (found is not null) Assert.False(DeckTypes.IsInPlay(found.Area.Type));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:cost-arrow-icon.1")]
    [Rule("rr:quickstrike.1")]
    public void QuickstrikeAndActualThorDrawPrecedeTheSchemeChoice()
    {
        // "Responses to the text preceding the cost arrow icon resolve before the text following the icon resolves."
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 6);
        var side = world.CreateCard("01129", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 4);
        var minion = world.CreateCard("01167", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("06003", seat.Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        int defenses = 0, responses = 0, choices = 0;
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.Equal(6, main.Tokens["k_threat"]);
            Assert.Equal(4, side.Tokens["k_threat"]);
            var before = world.Digest().Canonical();
            var rng = world.Random.Generator.GetState();
            var pending = world.Agenda.Outstanding.ToArray();
            var rejection = Record.Exception(() => Sequence.Answer(world, world.Facts, runner,
                prompt, Decision.Take(int.MaxValue), []));
            Assert.True(rejection is RulesNotImplementedException or ArgumentException);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(pending, world.Agenda.Outstanding);
            Decision answer;
            if (prompt.Asking == Question.Defender)
            {
                defenses++;
                answer = Decision.Decline;
            }
            else if (prompt.Affordances.SingleOrDefault(a => a.Timing == AbilityType.Response) is { } response)
            {
                responses++;
                Assert.Equal(1, defenses);
                Assert.Equal(3, seat.IdentityCard.Damage);
                answer = Decision.Take(response.Id);
            }
            else
            {
                choices++;
                Assert.Equal(1, responses);
                Assert.Equal(2, seat.Hand.Cards.Count);
                Assert.Equal(new[] { main.ObjectId, side.ObjectId }.Order(),
                    prompt.Affordances.Select(a => a.AnchorId).Order());
                answer = Decision.Take(side.ObjectId);
            }
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);
        }
        Assert.Equal(1, choices);
        Assert.Equal(1, side.Tokens["k_threat"]);
        Assert.Equal(6, main.Tokens["k_threat"]);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("ruling:a1f4982cfebcd768")]
    public void AcquiredPatrolRevalidatesMainSchemeButStillAllowsASideScheme(bool sideScheme)
    {
        // "patrol engages ... at step 5 ... cannot initiate ... remove threat" from the main scheme.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 6);
        var side = sideScheme ? world.CreateCard("01129", world.AreaOf(DeckType.SideSchemesArea)) : null;
        side?.PlaceTokens("k_threat", 4);
        var patrol = world.CreateCard("16119", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("06003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        new RedSkullFixture(world, runner).Finish(prompt =>
        {
            Assert.NotNull(side);
            Assert.Equal(side.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
            return Decision.Take(side.ObjectId);
        });
        Assert.Equal(DeckType.EngagedEnemiesArea, patrol.Area.Type);
        Assert.Equal(6, main.Tokens["k_threat"]);
        if (side is not null) Assert.Equal(1, side.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:cost-arrow-icon")]
    [Rule("ruling:6e301a347a5392fb")]
    public void SuccessfulEngagementPaysBeforeConfusedCancelsTheThwart()
    {
        // "must ... engage it before you can ... remove Confused."
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 6);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        var source = world.CreateCard("06003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        new RedSkullFixture(world, runner).Finish();
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(6, main.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void OfferQueriesPreserveTheUnprobedEntryCostContinuation()
    {
        var probed = ThorActualFixture.Board("06001a");
        var control = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        foreach (var world in new[] { probed, control })
        {
            world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 6);
            world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
            world.CreateCard("06003", world.Seats[0].Hand);
        }
        var before = probed.Digest().Canonical();
        var rng = probed.Random.Generator.GetState();
        var pending = probed.Agenda.Outstanding.ToArray();
        var source = Assert.Single(probed.Seats[0].Hand.Cards);
        for (int i = 0; i < 3; i++)
            Assert.Single(runner.Actions(probed, 0), a => a.Card == source.ObjectId);
        Assert.Equal(before, probed.Digest().Canonical());
        Assert.Equal(rng.Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(pending, probed.Agenda.Outstanding);
        foreach (var world in new[] { probed, control })
        {
            var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, [], []));
            new RedSkullFixture(world, runner).Finish(prompt =>
                Decision.Take(world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId));
        }
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(control.Agenda.Outstanding, probed.Agenda.Outstanding);
    }

    [Fact]
    public void AlterEgoCannotOfferDefenderWhileHeroWithThreatCan()
    {
        var world = ThorActualFixture.Board("06001b");
        var runner = AuthoredCards.Runner();
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("06003", world.Seats[0].Hand);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("06001a");
        Assert.Contains(runner.Actions(world, 0), a => a.Card == source.ObjectId);
    }
}
