using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>pack:mc03:new-rules and adjustable-difficulty, through engine defeat and agenda paths.</summary>
public sealed class WreckingVillainDefeatTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PromotionPreservesTheDefeatedGroupsVillainArea(bool active)
    {
        var fixture = new WreckingRuntimeFixture(extreme: true);
        var world = fixture.World;
        var group = fixture.Groups[active ? 0 : 2];
        var area = world.CreateArea(DeckType.VillainArea);
        World.MoveToTop(group.Villain, area);
        var events = new List<GameEvent>();
        var result = DamagePlacement.Resolve(world, world.Facts,
            new(world.Seats[0].IdentityCard, group.Villain, 5, "test", "Damage"), events);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Same(area, group.Stages[1].Area);
        Assert.Same(active ? group.Stages[1] : fixture.Groups[0].Villain, EncounterRouting.Villain(world));
        Assert.Contains(events.OfType<CardsMoved>(), moved =>
            moved.To == Places.Reference(area) && moved.Cards.Any(card => card.Card == group.Stages[1].ObjectId));
    }

    [Fact]
    public void ReconstructedTopologyReplaysTheSameChoiceEventsDigestAndRandomConsumption()
    {
        var first = Replay(new WreckingRuntimeFixture());
        var second = Replay(new WreckingRuntimeFixture());
        Assert.Equal(first.Digest, second.Digest);
        Assert.Equal(first.Prompt, second.Prompt);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(first.Words, second.Words);
        Assert.True(first.Words > 0);
    }

    private static (string Digest, string Prompt, string[] Events, long Words) Replay(WreckingRuntimeFixture fixture)
    {
        var world = fixture.World;
        var events = new List<GameEvent>();
        world.EncounterTopology!.SelectActive(fixture.Groups[2].Villain, "test", events);
        foreach (var card in fixture.Groups[2].Members) Discard.Card(world, card, "test", events);
        Assert.True(EncounterDeck.Reset(world, "test", events));
        world.EncounterTopology.SelectActive(fixture.Groups[0].Villain, "test", events);
        var result = DamagePlacement.Resolve(world, world.Facts,
            new(world.Seats[0].IdentityCard, fixture.Groups[0].Villain, 5, "test", "Damage"), events);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, events)!;
        string offered = System.Text.Json.JsonSerializer.Serialize(prompt);
        Sequence.Answer(world, world.Facts, world.Abilities, prompt,
            Decision.Take(fixture.Groups[2].Villain.ObjectId), events);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, events));
        Assert.True(result.IsComplete);
        return (world.Digest().Canonical(), offered,
            events.Select(EventJson.Write).ToArray(), world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void CounterTransferDoesNotGrantAnotherOrdinaryActivationAgainstTheSamePlayer()
    {
        var fixture = new WreckingRuntimeFixture();
        var world = fixture.World;
        world.EncounterTopology!.SelectActive(fixture.Groups[2].Villain, "test", []);
        world.Agenda.Add(new PhaseStep(new ActivationPlanOperation(
            ActivatedEnemies: [fixture.Groups[0].Villain.ObjectId], Players: [0]), 1, 2, Plan: true));
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, []));
        Assert.False(world.Agenda.IsBusy);
        Assert.All(fixture.Groups, group => Assert.Equal(2, group.Deck.Cards.Count));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void DamageRemainsUnfinishedUntilFirstPlayerResolvesTheActiveCounterTie()
    {
        var fixture = new WreckingRuntimeFixture();
        var world = fixture.World;
        var events = new List<GameEvent>();
        var result = DamagePlacement.Resolve(world, world.Facts,
            new(world.Seats[0].IdentityCard, fixture.Groups[0].Villain, 5, "test", "Damage"), events);
        Assert.False(result.IsComplete);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, events)!;
        Assert.NotNull(prompt);
        Assert.Equal("ChooseActiveVillain", prompt.Trigger);
        Assert.False(result.IsComplete);
        Sequence.Answer(world, world.Facts, world.Abilities, prompt,
            Decision.Take(fixture.Groups[2].Villain.ObjectId), events);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(Damage.Outcome.Defeated, result.Outcome);
        Assert.Same(fixture.Groups[2].Villain, EncounterRouting.Villain(world));
    }

    [Fact]
    public void InactiveDefeatRemovesOnlyItsSchemeAndKeepsItsEncounterCards()
    {
        // "Any encounter cards from that villain's deck that are in play remain in play."
        var fixture = new WreckingRuntimeFixture();
        var world = fixture.World;
        var group = fixture.Groups[2];
        World.MoveToTop(group.Members[0], world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.False(Defeat.FinalizeCharacter(world, world.Facts, group.Villain, "test", []));
        Assert.Equal(DeckType.RemovedArea, group.Villain.Area.Type);
        Assert.Equal(DeckType.RemovedArea, group.Scheme.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, group.Members[0].Area.Type);
        Assert.Same(fixture.Groups[0].Villain, EncounterRouting.Villain(world));
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    public void ActiveDefeatWithUniqueMostThreatMovesCounterWithoutChoice()
    {
        var fixture = new WreckingRuntimeFixture();
        var world = fixture.World;
        fixture.Groups[2].Scheme.PlaceTokens("k_threat", 3);
        Assert.False(Defeat.FinalizeCharacter(world, world.Facts,
            fixture.Groups[0].Villain, "test", []));
        Assert.Same(fixture.Groups[2].Villain, EncounterRouting.Villain(world));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void TiedDefeatAsksFirstPlayerAndRejectsUntiedOrExtraneousAnswers()
    {
        // "In case of a tie, the first player decides." The third survivor is not tied.
        var fixture = new WreckingRuntimeFixture();
        var world = fixture.World;
        fixture.Groups[1].Scheme.PlaceTokens("k_threat", 3);
        fixture.Groups[2].Scheme.PlaceTokens("k_threat", 3);
        Assert.True(Defeat.FinalizeCharacter(world, world.Facts,
            fixture.Groups[0].Villain, "test", []));
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, [])!;
        Assert.Equal(1, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Equal([fixture.Groups[1].Villain.ObjectId, fixture.Groups[2].Villain.ObjectId],
            prompt.Affordances.Select(offer => offer.Id));
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, world.Abilities,
            prompt, Decision.Take(fixture.Groups[3].Villain.ObjectId), []));
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, world.Abilities,
            prompt, Decision.Decline, []));
        Sequence.Answer(world, world.Facts, world.Abilities, prompt,
            Decision.Take(fixture.Groups[2].Villain.ObjectId), []);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, []));
        Assert.Same(fixture.Groups[2].Villain, EncounterRouting.Villain(world));
    }

    [Fact]
    public void OnlyDefeatingEveryVillainWins()
    {
        // "If the players defeat all 4 villains, they win the game!"
        var fixture = new WreckingRuntimeFixture();
        var world = fixture.World;
        foreach (var group in fixture.Groups.Skip(1))
        {
            Assert.False(Defeat.FinalizeCharacter(world, world.Facts, group.Villain, "test", []));
            Assert.Equal(Outcome.Unfinished, world.Result);
        }
        Assert.False(Defeat.FinalizeCharacter(world, world.Facts, fixture.Groups[0].Villain, "test", []));
        Assert.Equal(Outcome.PlayersWin, world.Result);
        Assert.All(fixture.Groups, group => Assert.Equal(DeckType.RemovedArea, group.Scheme.Area.Type));
    }

    [Fact]
    public void ExtremeDefeatPromotesOnlyItsOwnVersionBAndRetainsItsScheme()
    {
        // Version B enters when A is defeated; winning requires all B versions defeated.
        var fixture = new WreckingRuntimeFixture(extreme: true);
        var world = fixture.World;
        var first = fixture.Groups[0];
        first.Scheme.PlaceTokens("k_threat", 4);
        var events = new List<GameEvent>();
        Assert.False(Defeat.FinalizeCharacter(world, world.Facts, first.Villain, "test", events));
        Assert.Same(first.Stages[1], EncounterRouting.Villain(world));
        Assert.Equal(DeckType.RemovedArea, first.Villain.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, first.Scheme.Area.Type);
        Assert.Equal(4, first.Scheme.Tokens["k_threat"]);
        Assert.All(fixture.Groups.Skip(1), group => Assert.Equal(DeckType.VillainDeck, group.Stages[1].Area.Type));
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Contains(events.OfType<FieldSet>(), field => field.Card == first.Stages[1].ObjectId
            && field.Field == "active_villain" && field.To == 1);
    }
}
