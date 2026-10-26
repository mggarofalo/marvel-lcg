using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowInterrogationRoomTests
{
    [Theory]
    [InlineData("identity", true)]
    [InlineData("other", false)]
    [InlineData("ally", false)]
    [InlineData("event", true)]
    [Rule("rr:you-your.6")]
    [Rule("rr:you-your.12")]
    [Rule("rr:you-your.15")]
    public void OnlyThisIdentitysMinionDefeatOffersTheRoom(string source, bool expected)
    {
        // "when that player's identity attacks and defeats a minion" excludes allies;
        // event abilities are "also considered to be performed by that player's identity."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var room = InPlay(world, "08015");
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 4);
        var minion = world.CreateCard("08028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        minion.TakeDamage(2);
        if (source == "event")
        {
            var card = world.CreateCard("01022", world.Seats[0].Hand);
            var payment = world.CreateCard("08020", world.Seats[0].Hand);
            runner.ExecuteAction(world, new PlayerAction(new PendingAbility(card.ObjectId, AbilityType.Action, 0),
                [payment.ObjectId], []));
        }
        else if (source == "ally")
        {
            var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            AllyBasicPowers.AllyPower(world, world.Facts, ally, minion, BasicPowers.AttackVerb, []);
        }
        else BasicPowerInitiation.BasicAttack(world, world.Facts, source == "other" ? 1 : 0, minion, []);
        int responses = 0;
        for (int step = 0; step < 20; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            var offered = prompt.Affordances.SingleOrDefault(a => a.AnchorId == room.ObjectId);
            if (offered is not null) responses++;
            Sequence.Answer(world, world.Facts, runner, prompt, Answer(prompt, room, scheme), []);
        }
        Assert.Equal(expected ? 1 : 0, responses);
        Assert.Equal(expected ? 3 : 4, scheme.Tokens["k_threat"]);
        Assert.Equal(!expected, room.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static Decision Answer(Prompt prompt, Card room, Card scheme)
    {
        var response = prompt.Affordances.SingleOrDefault(a => a.AnchorId == room.ObjectId);
        if (response is not null) return Decision.Take(response.Id);
        var target = prompt.Affordances.SingleOrDefault(a => a.AnchorId == scheme.ObjectId);
        return target is not null ? Decision.Take(target.Id) : Decision.Decline;
    }
}
