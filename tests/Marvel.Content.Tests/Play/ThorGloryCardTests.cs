using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorGloryCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:you-your.6")]
    [Rule("rr:you-your.15")]
    public void HallCreditsIdentityButNeverControlledAlly(bool ally)
    {
        // "not when an ally under that player's control attacks and defeats a minion."
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hall = world.CreateCard("06017", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var fury = world.CreateCard("06018", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), world.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Equal(CardKind.Minion, world.Facts.Kind(minion.FaceId));
        minion.TakeDamage(2);
        if (ally)
        {
            var attacker = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            AllyBasicPowers.AllyPower(world, world.Facts, attacker, minion, BasicPowers.AttackVerb, []);
        }
        else BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        int responses = 0;
        for (int step = 0; step < 20; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            if (ally) Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == fury.ObjectId);
            var response = prompt.Affordances.SingleOrDefault(a => a.AnchorId == hall.ObjectId);
            if (response is not null) responses++;
            Sequence.Answer(world, world.Facts, runner, prompt,
                response is null ? Decision.Decline : Decision.Take(response.Id), []);
        }
        Assert.Equal(ally ? 0 : 1, responses);
        Assert.Equal(ally ? 0 : 1, hall.Tokens.GetValueOrDefault("c_glory"));
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("06001a", 3, true, false)]
    [InlineData("06001b", 2, true, false)]
    [InlineData("06001b", 3, false, false)]
    [InlineData("06001b", 3, true, true)]
    public void HallRequiresAlterEgoReadyAndThreeGlory(string face, int glory, bool ready, bool offered)
    {
        var world = ThorActualFixture.Board(face, 2);
        var runner = AuthoredCards.Runner();
        var hall = world.CreateCard("06017", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        // Test the non-first controller and their form, independently of Thor's form.
        world.Seats[1].IdentityCard.TurnTo(face == "06001b" ? "01001b" : "01001a");
        hall.PlaceTokens("c_glory", glory);
        if (!ready) hall.Exhaust();
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == hall.ObjectId);
        var actions = runner.Actions(world, 1).Where(a => a.Card == hall.ObjectId).ToList();
        Assert.Equal(offered ? 1 : 0, actions.Count);
        if (!offered) return;
        runner.ExecuteAction(world, new PlayerAction(actions[0], [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(3, world.Seats[1].Hand.Cards.Count);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.False(hall.Ready);
        Assert.Equal(0, hall.Tokens.GetValueOrDefault("c_glory"));
        Assert.Equal(DeckType.SupportsArea, hall.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
