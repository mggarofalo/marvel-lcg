using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class KangPlayerCountTests : CoreCardTestBase
{
    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, false, false)]
    [InlineData(3, false, false)]
    [InlineData(4, false, false)]
    [InlineData(1, true, false)]
    [InlineData(3, true, false)]
    [InlineData(4, true, false)]
    [InlineData(1, false, true)]
    [InlineData(3, false, true)]
    [InlineData(4, false, true)]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:delayed-effect.1")]
    public void SequentialWholeGroupDeparturesReachCentralExactlyOnce(int players, bool expert, bool completion)
    {
        // "A defeated card leaves play after its When Defeated ability is resolved."
        // "Delayed effects resolve automatically and immediately ... before responses".
        // 11008b permits central only when no other areas remain; 11013a/b reveal
        // III and put each player's physical nemesis into play, then shuffle.
        var first = Run(players, expert, completion);
        var second = Run(players, expert, completion);
        Assert.Equal(first, second);
    }

    private static string Run(int players, bool expert, bool completion)
    {
        var world = KangPlayerCountFixture.Board(players, expert, completion);
        var events = new List<GameEvent>();
        KangPlayerCountFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        KangPlayerCountFixture.Boundary(world, 1, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var order = world.PlayerOrder.ToArray();
        var locals = order.Select(player => Assert.Single(world.AreaOf(
            DeckType.VillainArea, PlayArea.Of(player)).Cards)).ToArray();
        for (int index = 0; index < players; index++)
        {
            int player = order[index];
            var source = world.GameAreaOf(PlayArea.Of(player));
            Assert.NotNull(source);
            Assert.Equal(index + 1, source.PlayAreas.Count);
            var scheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(player)).Cards);
            Depart(world, locals[index], scheme, completion, player, events);
            Assert.Single(world.Agenda.KangTransitions.Pending);
            Assert.Null(world.GameAreaOf(PlayArea.Villains));
            var identity = world.Seats[order[0]].IdentityCard;
            identity.Exhaust();
            int beforeBoundary = events.Count;
            KangPlayerCountFixture.Boundary(world, index + 2, events);
            world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), index + 2, 5));
            var prompt = Sequence.Work(world, Cards, world.Abilities, events);
            if (index + 1 < players)
            {
                ChooseDestination(world, prompt, order, index, beforeBoundary, events);
            }
            else
            {
                Assert.Null(prompt);
                AssertCentral(world, expert, completion, beforeBoundary, identity, events);
            }
            Assert.Empty(world.Agenda.Outstanding);
            Assert.Empty(world.Agenda.KangTransitions.Pending);
            Assert.Equal(Outcome.Unfinished, world.Result);
        }
        int count = events.Count;
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(count, events.Count);
        return KangPlayerCountFixture.Snapshot(world, events);
    }

    private static void Depart(World world, Card villain, Card scheme, bool completion, int player, List<GameEvent> events)
    {
        if (completion)
        {
            Agendas.Happening(world);
            Threat.Schedule(world, new ThreatBatch([scheme], null, 9, ThreatCause.CardAbility,
                "prepared local completion", player));
            Sequence.Finish(world, Cards, world.Abilities, events);
        }
        else KangPlayerCountFixture.Defeat(world, villain, events);
    }

    private static void ChooseDestination(World world, Prompt? prompt, int[] order, int index,
        int beforeBoundary, List<GameEvent> events)
    {
        Assert.NotNull(prompt);
        Assert.Equal(world.FirstPlayer, prompt.Player);
        Assert.False(prompt.Cancellable);
        var destinations = world.GameAreas.Where(area => !ReferenceEquals(area, world.GameAreaOf(PlayArea.Of(order[index])))
            && area.PlayAreas.Any(play => play.IsPlayers)).OrderBy(area => area.Id).ToArray();
        Assert.Equal(world.Players - index - 1, destinations.Length);
        Assert.Equal(destinations.Select(area => area.Id), prompt.Affordances.Select(offer => offer.Id));
        Assert.DoesNotContain(events.Skip(beforeBoundary).OfType<FieldSet>(),
            field => field.Card == world.Seats[order[0]].IdentityCard.ObjectId && field.Verb == "Ready");
        var target = world.GameAreaOf(PlayArea.Of(order[index + 1]))!;
        string beforeInvalid = KangPlayerCountFixture.Snapshot(world, events);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, world.Abilities,
            prompt, Decision.Take(int.MaxValue), events));
        Assert.Equal(beforeInvalid, KangPlayerCountFixture.Snapshot(world, events));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(target.Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.All(order.Take(index + 2), seat => Assert.Same(target, world.GameAreaOf(PlayArea.Of(seat))));
        Assert.Null(world.GameAreaOf(PlayArea.Villains));
    }

    private static void AssertCentral(World world, bool expert, bool completion, int beforeBoundary,
        Card identity, List<GameEvent> events)
    {
        var central = world.GameAreaOf(PlayArea.Villains);
        Assert.NotNull(central);
        Assert.All(world.Seats, seat => Assert.Same(central, world.GameAreaOf(PlayArea.Of(seat.Index))));
        var final = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        Assert.Equal(expert ? "11039" : "11006", final.FaceId);
        Assert.Equal((expert ? 25 : 20) * world.Players, DamagePlacement.Health(world, Cards, final));
        var finalScheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);
        Assert.Equal("11013b", finalScheme.FaceId);
        Assert.Equal(0, finalScheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(completion ? world.Players : 0, world.Cards.Count(card => card.FaceId == "11023"
            && card.Area.Type == DeckType.SideSchemesArea && card.FaceUp));
        foreach (var seat in world.Seats)
        {
            var nemesis = Assert.Single(seat.NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
            Assert.Equal(DeckType.EngagedEnemiesArea, nemesis.Area.Type);
            Assert.Equal(PlayArea.Of(seat.Index), nemesis.Area.PlayArea);
        }
        int shuffle = events.FindLastIndex(item => item is AreaReordered { Trigger: "11013b", Verb: "Shuffle" });
        int ready = events.FindLastIndex(item => item is FieldSet field && field.Card == identity.ObjectId && field.Verb == "Ready");
        Assert.True(shuffle >= beforeBoundary && ready > shuffle);
    }
}
