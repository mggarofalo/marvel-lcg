using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorStandardIndependentTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:you-your.6")]
    public void OnlyTheDefeatingIdentitiesControllerGainsGlory(int attacker)
    {
        // "the controlling player’s identity attacks and defeats a minion".
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var halls = Enumerable.Range(0, 2).Select(player => world.CreateCard("06017",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(player), cardOwner: player))).ToArray();
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        minion.TakeDamage(2);
        BasicPowerInitiation.BasicAttack(world, world.Facts, attacker, minion, []);
        int answers = 0;
        for (int step = 0; step < 20; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.DoesNotContain(prompt.Affordances, offer => offer.AnchorId == halls[1 - attacker].ObjectId);
            var offered = prompt.Affordances.SingleOrDefault(offer => offer.AnchorId == halls[attacker].ObjectId);
            if (offered is not null) answers++;
            Sequence.Answer(world, world.Facts, runner, prompt,
                offered is null ? Decision.Decline : Decision.Take(offered.Id), []);
        }
        Assert.Equal(1, answers);
        Assert.Equal(1, halls[attacker].Tokens.GetValueOrDefault("c_glory"));
        Assert.Equal(0, halls[1 - attacker].Tokens.GetValueOrDefault("c_glory"));
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void BattleFuryDoesNotReadyAfterAMinionSurvivesTheHeroAttack()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[0].IdentityCard;
        var fury = world.CreateCard("06018", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), hero.ObjectId, cardOwner: 0));
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.True(prompt is null || prompt.Affordances.All(offer => offer.AnchorId != fury.ObjectId));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.False(hero.Ready);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(DeckType.UpgradesArea, fury.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void TricksterCountsThreeDifferentDiscardedTypesAndNoOlderDiscard()
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var seat = world.Seats[1];
        world.CreateCard("06022", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        var ally = world.CreateCard("06002", seat.Deck);
        var attack = world.CreateCard("06005", seat.Deck);
        var upgrade = world.CreateCard("06008", seat.Deck);
        Assert.Equal(CardKind.Ally, world.Facts.Kind(ally.FaceId));
        Assert.Equal(CardKind.Event, world.Facts.Kind(attack.FaceId));
        Assert.Equal(CardKind.Upgrade, world.Facts.Kind(upgrade.FaceId));
        var fixture = new RedSkullFixture(world, AuthoredCards.Runner());
        fixture.Reveal("06030", 1);
        fixture.Finish();
        Assert.Equal(3, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.All(new[] { ally, attack, upgrade }, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Equal(12, seat.Deck.Cards.Count);
        Assert.Equal(12, world.Seats[0].Deck.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
