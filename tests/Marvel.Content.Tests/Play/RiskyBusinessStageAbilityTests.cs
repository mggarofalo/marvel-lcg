using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic research boards exercise the authored book without admitting the pack.
public sealed class RiskyBusinessStageAbilityTests : CoreCardTestBase
{
    [Theory]
    [InlineData("02001", "01001a", 3)]
    [InlineData("02001", "01001b", 0)]
    [InlineData("02002", "01001a", 3)]
    [InlineData("02002", "01001b", 3)]
    [InlineData("02003", "01001a", 4)]
    [InlineData("02003", "01001b", 4)]
    [Rule("rr:when-revealed-abilities")]
    public void LastInfamyExecutesTheActualStageReveal(string stage, string form, long damage)
    {
        // 02001b: "Deal 3 indirect damage to each player in hero form."
        // 02002b: "Deal 3 indirect damage to each player."
        // 02003b: "Deal 4 damage to each player."
        var world = Hero(form);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard(stage + "a," + stage + "b", world.AreaOf(DeckType.VillainArea));
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        Reveal.EnterPlay(world, Cards, environment, [], abilities: runner);
        var events = new List<GameEvent>();

        GreenGoblinTransition.RemoveCounters(world, environment, "infamy", 2, "Test", events);
        Resolve(world, events);

        Assert.Equal(stage + "b", villain.FaceId);
        Assert.Equal("02006b", environment.FaceId);
        Assert.Equal(2, environment.Tokens["c_madness"]);
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("02001b", "01001a")]
    [InlineData("02002b", "01001b")]
    [Rule("rr:indirect-damage.1")]
    public void IndirectRevealOffersOnlyTheResolvingPlayersCharacters(string face, string form)
    {
        // "Indirect damage dealt to a player can be divided as that player
        // chooses among characters under their control."
        var world = Hero(form, players: 2);
        world.FirstPlayer = 1;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard(face, world.AreaOf(DeckType.VillainArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var otherAlly = world.CreateCard("01020", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var events = runner.ExecuteReveal(world, villain, 0).Events.ToList();
        var order = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(1, order.Player);
        Sequence.Answer(world, Cards, runner, order,
            Decision.Take(Assert.Single(order.Affordances).Id,
                [world.Seats[1].IdentityCard.ObjectId, world.Seats[0].IdentityCard.ObjectId], []), events);
        var allocation = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Element, allocation.Asking);
        Assert.Equal(1, allocation.Player);
        var choice = Assert.Single(allocation.Affordances);
        Assert.DoesNotContain(otherAlly.ObjectId, choice.Targets!.Legal);
        Assert.DoesNotContain(world.Seats[0].IdentityCard.ObjectId, choice.Targets.Legal);

        Sequence.Answer(world, Cards, runner, allocation,
            Decision.Take(choice.Id, [ally.ObjectId, world.Seats[1].IdentityCard.ObjectId,
                world.Seats[1].IdentityCard.ObjectId], []), events);
        var secondAllocation = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(0, secondAllocation.Player);
        var secondChoice = Assert.Single(secondAllocation.Affordances);
        Sequence.Answer(world, Cards, runner, secondAllocation,
            Decision.Take(secondChoice.Id, Enumerable.Repeat(world.Seats[0].IdentityCard.ObjectId, 3).ToArray(), []), events);
        Resolve(world, events);

        Assert.Equal(1, ally.Damage);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, otherAlly.Damage);
    }

    [Fact]
    [Rule("rr:you-your.2")]
    public void StageThreeDirectDamageNeverOffersAllies()
    {
        // "If a card ability deals damage to 'you,' the damage is dealt
        // to the player's identity."
        var world = Hero("01001b", players: 2);
        world.Abilities = AuthoredCards.Runner();
        var villain = world.CreateCard("02003b", world.AreaOf(DeckType.VillainArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var events = AuthoredCards.Runner().ExecuteReveal(world, villain, 0).Events.ToList();
        Resolve(world, events);

        Assert.All(world.Seats, seat => Assert.Equal(4, seat.IdentityCard.Damage));
        Assert.Equal(0, ally.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("02001", "02002", 'a', 0)]
    [InlineData("02001", "02002", 'b', 3)]
    [InlineData("02002", "02003", 'a', 0)]
    [InlineData("02002", "02003", 'b', 4)]
    [Rule("rr:villain-defeat.1")]
    [Rule("rr:villain-defeat.2")]
    public void PromotionRevealsTheNextStagesSameSide(string stage, string following, char side, long damage)
    {
        // "The next sequential stage of the villain deck is revealed."
        // "Excess damage ... does not carry over to the new stage."
        // pack:mc02:new-rules#when-a-villain-stage-is-defeated keeps the same side.
        var world = Hero("01001b");
        world.Abilities = AuthoredCards.Runner();
        var villain = world.CreateCard(stage + "a," + stage + "b", world.AreaOf(DeckType.VillainArea));
        villain.TurnTo(stage + side);
        villain.TakeDamage(100);
        var next = world.CreateCard(following + "a," + following + "b", world.AreaOf(DeckType.VillainDeck));
        next.TurnTo(following + (side == 'a' ? 'b' : 'a'));
        var events = new List<GameEvent>();

        // Synthetic lethal state isolates stage entry from Norman's damage replacement.
        Defeat.Character(world, Cards, villain, "Test", events, recordOn: new Occurrence(1, []));
        Resolve(world, events);

        Assert.Same(next, world.TheCardIn(DeckType.VillainArea));
        Assert.Equal(following + side, next.FaceId);
        Assert.Equal(0, next.Damage);
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.RemovedArea, villain.Area.Type);
    }

    [Theory]
    [InlineData("02006a", "infamy", 1)]
    [InlineData("02006b", "madness", 1)]
    [InlineData("02006a", "infamy", 2)]
    [InlineData("02006b", "madness", 2)]
    [InlineData("02006a", "infamy", 4)]
    [InlineData("02006b", "madness", 4)]
    [Rule("rr:per-player-icon")]
    public void BothEnvironmentFacesEnterWithTheirPrintedPool(string face, string counter, int players)
    {
        // "Multiplied by the number of players who started the scenario."
        var world = Hero("01001b", players);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var environment = world.CreateCard(face, world.AreaOf(DeckType.EnvironmentArea));
        var events = new List<GameEvent>();

        Reveal.EnterPlay(world, Cards, environment, events, abilities: runner);

        Assert.Equal(2L * players, environment.Tokens["c_" + counter]);
        Assert.Equal(DeckType.EnvironmentArea, environment.Area.Type);
        Assert.Contains(events.OfType<FieldSet>(), change => change.Card == environment.ObjectId
            && change.Field == "c_" + counter && change.To == 2L * players);
    }

    private static void Resolve(World world, List<GameEvent> events)
    {
        var runner = AuthoredCards.Runner();
        for (int count = 0; count < 12; count++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) return;
            Assert.Equal(Question.Order, prompt.Asking);
            var order = Assert.Single(prompt.Affordances);
            Sequence.Answer(world, Cards, runner, prompt,
                Decision.Take(order.Id, order.Targets!.Legal, []), events);
        }
        Assert.Fail("Stage reveal did not settle within its bounded player ordering.");
    }
}
