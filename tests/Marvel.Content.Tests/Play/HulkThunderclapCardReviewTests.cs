using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkThunderclapCardReviewTests
{
    [Fact]
    [Rule("rr:choose-game-element.1")]
    public void NonFirstPlayerChoosesTwoEnemiesAcrossBothSeatsAndLeavesTheRestAlone()
    {
        // "The player resolving the ability ... is the player who makes the choice."
        // Printed 10005: "Choose up to 3 different enemies. Deal 3 damage to each of them."
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, owner: 1);
        HulkFirstSliceFixture.EmptyHand(world, 1);
        var source = world.CreateCard("10005", world.Seats[1].Hand);
        var payment = world.CreateCard("10007", world.Seats[1].Hand);
        Card[] enemies = [world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
            world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1))),
            world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)))];
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, 1), item => item.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(1, prompt.Player);
        var offer = Assert.Single(prompt.Affordances);
        Assert.All(enemies, enemy => Assert.Contains(enemy.ObjectId, offer.Targets!.Legal));
        Sequence.Answer(world, world.Facts, runner, prompt,
            new(offer.Id, [enemies[1].ObjectId, enemies[0].ObjectId]), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(3, enemies[0].Damage);
        Assert.Equal(3, enemies[1].Damage);
        Assert.Equal(0, enemies[2].Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)), source.Area);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)), payment.Area);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.IsOver);
    }

    [Fact]
    public void UnderpaymentPreservesTheUnprobedControlThenLegalRetryCompletes()
    {
        var f = Board();
        var control = Board();
        string before = Snapshot(f.World);
        var action = Assert.Single(f.World.Abilities!.Actions(f.World, 0), item => item.Card == f.Source.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => f.World.Abilities.ExecuteAction(f.World,
            new PlayerAction(action, [f.Payment[0].ObjectId], [])));
        Assert.Equal(before, Snapshot(f.World));
        var actual = ResolveVillain(f.World, f.Source, f.Payment);
        var expected = ResolveVillain(control.World, control.Source, control.Payment);
        Assert.Equal(Snapshot(control.World), Snapshot(f.World));
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        Assert.Equal(3, f.World.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, f.Source.Area.Type);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("eventDamage", 4)]
    [InlineData("attackDamage", 3)]
    [Rule("rr:event.5")]
    [Rule("rr:labeled-ability.2")]
    public void EventBonusModifiesEveryRecipientButAttackBonusDoesNot(string kind, int expected)
    {
        // "Each of those instances is modified." An ability "labeled (attack)" is an attack.
        // Printed Thunderclap has no attack label. Modifier kind names are engine choices.
        var f = Board();
        var runner = AuthoredCards.Runner();
        f.World.Effects.Register(new(EffectSource.LastingEffect, kind, 1,
            Card: f.Source.ObjectId, Affects: f.Source.ObjectId));
        var enemy = f.World.CreateCard("01184", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var villain = f.World.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(runner.Actions(f.World, 0), item => item.Card == f.Source.ObjectId);
        var events = runner.ExecuteAction(f.World, new PlayerAction(action,
            f.Payment.Select(card => card.ObjectId).ToArray(), [])).Events.ToList();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(f.World, f.World.Facts, runner, events));
        var offer = Assert.Single(prompt.Affordances);
        Sequence.Answer(f.World, f.World.Facts, runner, prompt,
            new(offer.Id, [enemy.ObjectId, villain.ObjectId]), events);
        Sequence.Finish(f.World, f.World.Facts, runner, events);
        Assert.Equal(expected, enemy.Damage);
        Assert.Equal(expected, villain.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.DoesNotContain(events, item => item.Verb == "Attack");
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    private static (World World, Card Source, Card[] Payment) Board()
    {
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        return (world, world.CreateCard("10005", world.Seats[0].Hand),
            [world.CreateCard("01090", world.Seats[0].Hand), world.CreateCard("10002", world.Seats[0].Hand)]);
    }

    private static List<GameEvent> ResolveVillain(World world, Card source, Card[] payment)
    {
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action,
            payment.Select(card => card.ObjectId).ToArray(), [])).Events.ToList();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Sequence.Answer(world, world.Facts, runner, prompt,
            new(Assert.Single(prompt.Affordances).Id, [world.TheCardIn(DeckType.VillainArea)!.ObjectId]), events);
        Sequence.Finish(world, world.Facts, runner, events);
        return events;
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Cards = world.Digest().Canonical(), Rng = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, Current = world.Agenda.Current,
        Pending = world.Agenda.Outstanding,
    });
}
