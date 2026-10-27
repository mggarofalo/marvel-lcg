using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkThunderclapCardTests
{
    [Fact]
    public void OneActualFaceAppendsAndToeToToeRemainsUnknown()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(716, rows.Length);
        Assert.Equal("10005", rows[708].GetProperty("card").GetString());
        Assert.False(rows[708].TryGetProperty("startingCounters", out _));
        Assert.Equal(31, AuthoredCards.Book.Authored.Count(id => id.StartsWith("10", StringComparison.Ordinal)));
        Assert.DoesNotContain("10015", AuthoredCards.Book.Authored);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [Rule("rr:choose-game-element.3.1")]
    [Rule("rr:target.3.6")]
    [Rule("rr:guard.1")]
    [Rule("rr:stun-stunned.5")]
    [Rule("rr:retaliate-x.1")]
    public void PaidHeroActionSelectsOneDistinctGroupAndNeverAttacks(int count)
    {
        // "The same target cannot be chosen multiple times this way." Prevented damage affects a target.
        // Guard says "cannot attack any villain"; Stun replaces an "attack or ... attack ability".
        // Retaliate triggers "After this character is attacked".
        // Printed 10005 says "Choose up to 3 different enemies. Deal 3 damage to each of them."
        var f = Board();
        Statuses.Give(f.World, f.Targets[1], Statuses.Tough);
        Statuses.Give(f.World, f.World.Seats[0].IdentityCard, Statuses.Stunned);
        f.World.Effects.Register(new(EffectSource.LastingEffect, "guard", 1, Affects: f.Targets[2].ObjectId));
        f.World.Effects.Register(new(EffectSource.LastingEffect, "retaliate", 2, Affects: f.Targets[0].ObjectId));
        var events = new List<GameEvent>();
        var prompt = Begin(f.World, f.Runner, f.Source, f.Payment, events);
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(0, offer.Targets!.Min);
        Assert.Equal(3, offer.Targets.Max);
        Assert.False(offer.Targets.AllowRepeated);
        Assert.Contains(f.Targets[2].ObjectId, offer.Targets.Legal);
        Assert.DoesNotContain(f.World.Seats[0].IdentityCard.ObjectId, offer.Targets.Legal);
        Assert.All(f.Targets, target => Assert.Equal(0, target.Damage));
        Sequence.Answer(f.World, f.World.Facts, f.Runner, prompt,
            new(offer.Id, f.Targets.Take(count).Select(card => card.ObjectId).Reverse().ToArray()), events);
        Sequence.Finish(f.World, f.World.Facts, f.Runner, events);
        Assert.Equal(count > 0 ? 3 : 0, f.Targets[0].Damage);
        Assert.Equal(0, f.Targets[1].Damage);
        Assert.Equal(count == 3 ? 3 : 0, f.Targets[2].Damage);
        Assert.Equal(count < 2, Statuses.Has(f.World, f.Targets[1], Statuses.Tough));
        Assert.True(Statuses.Has(f.World, f.World.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.DoesNotContain(events, item => item.Verb == "Attack");
        Assert.All(f.Payment.Append(f.Source), card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("maximum")]
    [InlineData("stale")]
    public void RejectedGroupPreservesRngPromptAndPendingWorkThenAcceptsRetry(string kind)
    {
        var f = Board();
        var events = new List<GameEvent>();
        var prompt = Begin(f.World, f.Runner, f.Source, f.Payment, events);
        var offer = Assert.Single(prompt.Affordances);
        if (kind == "stale") World.MoveToTop(f.Targets[1], f.World.AreaOf(DeckType.EncounterDiscardPile));
        int[] chosen = kind switch
        {
            "duplicate" => [f.Targets[1].ObjectId, f.Targets[1].ObjectId],
            "maximum" => offer.Targets!.Legal.Take(4).ToArray(),
            _ => [f.Targets[1].ObjectId],
        };
        var before = Snapshot(f.World);
        Assert.ThrowsAny<ArgumentException>(() => Sequence.Answer(f.World, f.World.Facts,
            f.Runner, prompt, new(offer.Id, chosen), events));
        Assert.Equal(before, Snapshot(f.World));
        Assert.Same(offer, Assert.Single(prompt.Affordances));
        Sequence.Answer(f.World, f.World.Facts, f.Runner, prompt, new(offer.Id, []), events);
        Sequence.Finish(f.World, f.World.Facts, f.Runner, events);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Fact]
    public void ActualOfferQueriesPreserveTheUnprobedControlAndSelectionOrder()
    {
        var f = Board();
        var control = Board();
        var before = Snapshot(f.World);
        for (int i = 0; i < 3; i++)
            foreach (var action in f.Runner.Actions(f.World, 0))
                _ = f.Runner.Describe(f.World, action);
        Assert.Equal(before, Snapshot(f.World));
        var actualEvents = new List<GameEvent>();
        var expectedEvents = new List<GameEvent>();
        var prompt = Begin(f.World, f.Runner, f.Source, f.Payment, actualEvents);
        var expected = Begin(control.World, control.Runner, control.Source, control.Payment, expectedEvents);
        before = Snapshot(f.World);
        for (int i = 0; i < 3; i++)
            _ = f.Runner.Choosing(f.World, f.World.Agenda.Current!.Value);
        Assert.Equal(before, Snapshot(f.World));
        Sequence.Answer(f.World, f.World.Facts, f.Runner, prompt,
            new(Assert.Single(prompt.Affordances).Id, f.Targets.Take(3).Reverse().Select(card => card.ObjectId).ToArray()), actualEvents);
        Sequence.Answer(control.World, control.World.Facts, control.Runner, expected,
            new(Assert.Single(expected.Affordances).Id, control.Targets.Take(3).Select(card => card.ObjectId).ToArray()), expectedEvents);
        Sequence.Finish(f.World, f.World.Facts, f.Runner, actualEvents);
        Sequence.Finish(control.World, control.World.Facts, control.Runner, expectedEvents);
        Assert.Equal(Snapshot(control.World), Snapshot(f.World));
        Assert.Equal(JsonSerializer.Serialize(expectedEvents), JsonSerializer.Serialize(actualEvents));
    }

    [Fact]
    public void AlterEgoCannotInitiateWhileSamePaidHeroStateCan()
    {
        var f = Board();
        f.World.Seats[0].IdentityCard.TurnTo("10001b");
        var before = Snapshot(f.World);
        Assert.DoesNotContain(f.Runner.Actions(f.World, 0), action => action.Card == f.Source.ObjectId);
        Assert.Equal(before, Snapshot(f.World));
        f.World.Seats[0].IdentityCard.TurnTo("10001a");
        Assert.Single(f.Runner.Actions(f.World, 0), action => action.Card == f.Source.ObjectId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:damage.step.1")]
    [Rule("rr:damage.step.5")]
    [Rule("rr:damage.step.6")]
    public void ActualRowKeepsSimultaneousPlacementAcrossPreventionAndDefeatSuspension(bool defeat)
    {
        // "Would deal/be dealt" precedes "Placing of damage", then "would be defeated".
        // Only the minion's synthetic interrupt is substituted; canonical 10005 is loaded unchanged.
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = root["cards"]!.AsArray();
        var target = rows.Single(row => row!["card"]!.GetValue<string>() == "01184")!;
        target["abilities"] = JsonNode.Parse(defeat ? """
            [{"trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"this"},
              "effect":{"draw":{"player":"you","count":1}}}]
            """ : """
            [{"trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"this"},
              "effect":{"choose":{"options":[
                {"preventDamage":{"card":"trigger.target","amount":1}},
                {"preventDamage":{"card":"trigger.target","amount":2}}]}}}]
            """);
        var f = Board(new(AbilityCatalog.Parse(root.ToJsonString())));
        var minion = f.World.CreateCard("01184", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        if (defeat) minion.TakeDamage(Damage.Health(f.World, f.World.Facts, minion) - 3);
        long initial = minion.Damage;
        var events = new List<GameEvent>();
        var prompt = Begin(f.World, f.Runner, f.Source, f.Payment, events);
        Sequence.Answer(f.World, f.World.Facts, f.Runner, prompt,
            new(Assert.Single(prompt.Affordances).Id, [minion.ObjectId, f.Targets[0].ObjectId]), events);
        var suspension = Assert.IsType<Prompt>(Sequence.Work(f.World, f.World.Facts, f.Runner, events));
        Assert.Equal(defeat ? Question.Opportunity : Question.Option, suspension.Asking);
        Assert.Equal(defeat ? 3 : 0, f.Targets[0].Damage);
        Assert.Equal(initial + (defeat ? 3 : 0), minion.Damage);
        Sequence.Answer(f.World, f.World.Facts, f.Runner, suspension,
            defeat ? Decision.Decline : Decision.Take(suspension.Affordances[0].Id), events);
        while (Sequence.Work(f.World, f.World.Facts, f.Runner, events) is { } next)
        {
            Assert.True(next.Cancellable);
            Sequence.Answer(f.World, f.World.Facts, f.Runner, next, Decision.Decline, events);
        }
        Assert.Equal(3, f.Targets[0].Damage);
        if (defeat) Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        else Assert.Equal(2, minion.Damage);
        Assert.Equal(DeckType.DiscardPile, f.Source.Area.Type);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    private static (World World, AbilityRunner Runner, Card Source, Card[] Payment, Card[] Targets) Board(AbilityRunner? runner = null)
    {
        runner ??= AuthoredCards.Runner();
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, runner: runner);
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var source = world.CreateCard("10005", world.Seats[0].Hand);
        Card[] payment = [world.CreateCard("01090", world.Seats[0].Hand), world.CreateCard("10002", world.Seats[0].Hand)];
        Card[] targets = [world.TheCardIn(DeckType.VillainArea)!,
            world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1))),
            world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
            world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)))];
        return (world, runner, source, payment, targets);
    }

    private static Prompt Begin(World world, AbilityRunner runner, Card source, Card[] payment, List<GameEvent> events)
    {
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        events.AddRange(runner.ExecuteAction(world, new PlayerAction(action,
            payment.Select(card => card.ObjectId).ToArray(), [])).Events);
        return Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Cards = world.Digest().Canonical(), Rng = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, Current = world.Agenda.Current,
        Pending = world.Agenda.Outstanding,
    });
}
