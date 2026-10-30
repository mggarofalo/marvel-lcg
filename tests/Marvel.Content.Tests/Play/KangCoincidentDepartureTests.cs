using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual-card nonpublic overlay; production Kang admission remains closed.
public sealed class KangCoincidentDepartureTests : CoreCardTestBase
{
    [Theory]
    [InlineData(2, false, 0, false, false)]
    [InlineData(3, false, 0, false, false)]
    [InlineData(4, false, 0, false, false)]
    [InlineData(2, true, 1, false, true)]
    [InlineData(3, true, 1, false, true)]
    [InlineData(4, true, 1, false, true)]
    [InlineData(2, false, 2, false, false)]
    [InlineData(3, false, 2, false, true)]
    [InlineData(4, true, 2, false, false)]
    [InlineData(4, false, 2, true, false)]
    [Rule("rr:first-player.3")]
    [Rule("rr:delayed-effect.1")]
    public void FirstPlayerOrdersWholeGroupDeparturesBeforeOneCentralReunion(
        int players, bool expert, int completionMode, bool alreadyGrouped, bool villainPhase)
    {
        // "If two or more effects would resolve simultaneously, the first player
        // decides the order in which to resolve them."
        // Delayed effects resolve "before responses to that point or condition".
        // 11002-11005/11035-11038 join at phase end; 11009b-11012b remove
        // their own villain/stage then combine. MC11 moves the combined group.
        Assert.Equal(Run(players, expert, completionMode, alreadyGrouped, villainPhase),
            Run(players, expert, completionMode, alreadyGrouped, villainPhase));
    }

    private static string Run(int players, bool expert, int completionMode, bool alreadyGrouped, bool villainPhase)
    {
        var world = KangPlayerCountFixture.Board(players, expert, completion: true);
        var events = new List<GameEvent>();
        KangPlayerCountFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        KangPlayerCountFixture.Boundary(world, 1, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var locals = Enumerable.Range(0, players).Select(player => Assert.Single(world.AreaOf(
            DeckType.VillainArea, PlayArea.Of(player)).Cards)).ToArray();
        int start = PrepareGroup(world, locals, alreadyGrouped, events);
        var schemes = Enumerable.Range(start, players - start).Select(player => Assert.Single(world.AreaOf(
            DeckType.MainSchemesArea, PlayArea.Of(player)).Cards)).ToArray();
        int dominions = CaptureAll(world, locals, schemes, start, completionMode, events);
        Assert.Equal(players - start, world.Agenda.KangTransitions.Pending.Count);
        if (completionMode == 0)
            Assert.DoesNotContain(world.Cards, card => KangLocalCompletion.IsLocal(card)
                && card.Area.Type == DeckType.MainSchemesArea);
        foreach (var seat in world.Seats.Skip(start)) world.CreateCard("01011", seat.Hero);
        var identity = world.Seats[0].IdentityCard;
        identity.Exhaust();
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(villainPhase
            ? PhaseTransitionKind.EndVillainPhase : PhaseTransitionKind.EndPlayerPhase), 3, 4));
        var owner = world.Agenda.Current!.Value;
        var origin = world.Agenda.Occurrence!;
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 3, 5));
        var pendingSeats = Enumerable.Range(start, players - start).Reverse().ToList();
        int currentSeat = -1;
        int orders = 0;
        int discards = 0;
        for (int answer = 0; answer < 40; answer++)
        {
            var prompt = Sequence.Work(world, Cards, world.Abilities, events);
            if (prompt is null) break;
            Assert.True(world.Agenda.IsOutstanding(owner, origin));
            Assert.False(identity.Ready);
            Assert.Equal(players - 1, prompt.Player);
            Assert.False(prompt.Cancellable);
            Assert.Null(world.GameAreaOf(PlayArea.Villains));
            AssertPurePrompt(world, prompt, events);
            int selected;
            if (prompt.Trigger == KangDepartureOrderOperation.Order)
            {
                currentSeat = pendingSeats[0];
                var captured = world.Agenda.Current!.Value.Require<KangDepartureOrderOperation>().Remaining;
                Assert.All(captured.Where(item => item.CompletedScheme is not null), item =>
                {
                    Assert.Equal(DeckType.VillainArea, world.Cards[item.Defeated.Card].Area.Type);
                    Assert.Equal(DeckType.MainSchemesArea, world.Cards[item.CompletedScheme!.Card].Area.Type);
                });
                Assert.Equal(pendingSeats.Select(seat => locals[seat].ObjectId).Order(),
                    prompt.Affordances.Select(offer => offer.Id));
                selected = locals[currentSeat].ObjectId;
                pendingSeats.RemoveAt(0);
                orders++;
            }
            else if (prompt.Affordances[0].Verb == "Join_Game_Area")
            {
                Assert.Equal(KangJoinOperation.Join, prompt.Trigger);
                Assert.DoesNotContain(prompt.Affordances, offer => offer.AnchorPlayer != world.FirstPlayer);
                selected = world.GameAreaOf(PlayArea.Of(pendingSeats[0]))!.Id;
                Assert.Contains(prompt.Affordances, offer => offer.Id == selected);
                var child = world.Agenda.Current!.Value.Require<KangJoinOperation>();
                Assert.Equal(owner, child.Owner);
                Assert.Same(origin, child.Origin);
                Assert.Equal(locals[currentSeat].ObjectId, child.Departure.Defeated.Card);
                Assert.Equal(child.Departure.Source.PlayAreas.OrderBy(area => area.Player), child.Departure.Group);
            }
            else
            {
                Assert.All(prompt.Affordances, offer => Assert.Equal("Discard_Joined_Unique", offer.Verb));
                selected = prompt.Affordances[0].Id;
                discards++;
            }
            Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(selected), events);
        }
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(players - start - 1, orders);
        Assert.Equal(players - start - 1, discards);
        AssertCentral(world, expert, dominions, identity, events);
        return Snapshot(world, events);
    }

    private static int PrepareGroup(World world, Card[] locals, bool alreadyGrouped, List<GameEvent> events)
    {
        if (!alreadyGrouped) return 0;
        KangPlayerCountFixture.Defeat(world, locals[0], events);
        KangPlayerCountFixture.Boundary(world, 2, events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Sequence.Answer(world, Cards, world.Abilities, prompt,
            Decision.Take(world.GameAreaOf(PlayArea.Of(1))!.Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Same(world.GameAreaOf(PlayArea.Of(0)), world.GameAreaOf(PlayArea.Of(1)));
        return 1;
    }

    private static int CaptureAll(World world, Card[] locals, Card[] schemes, int start,
        int mode, List<GameEvent> events)
    {
        int dominions = 0;
        for (int player = start; player < world.Players; player++)
        {
            if (mode == 1 || mode == 2 && player % 2 == 0)
            {
                Agendas.Happening(world);
                Threat.Schedule(world, new ThreatBatch([schemes[player - start]], null, 9,
                    ThreatCause.CardAbility, "prepared local completion", player));
                Sequence.Finish(world, Cards, world.Abilities, events);
                Assert.Equal(DeckType.VillainArea, locals[player].Area.Type);
                dominions++;
            }
            else KangPlayerCountFixture.Defeat(world, locals[player], events);
        }
        return dominions;
    }

    private static void AssertPurePrompt(World world, Prompt prompt, List<GameEvent> events)
    {
        string before = Snapshot(world, events);
        var repeated = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(prompt.Trigger, repeated.Trigger);
        Assert.Equal(prompt.Affordances.Select(offer => offer.Id), repeated.Affordances.Select(offer => offer.Id));
        Assert.Equal(before, Snapshot(world, events));
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, world.Abilities,
            prompt, Decision.Take(int.MaxValue), events));
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, world.Abilities,
            prompt, Decision.Decline, events));
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, world.Abilities,
            prompt, new Decision(prompt.Affordances[0].Id, [0]), events));
        Assert.Equal(before, Snapshot(world, events));
    }

    private static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        State = KangPlayerCountFixture.Snapshot(world, events),
        Captures = world.Agenda.Outstanding.SelectMany(step => step.Operation switch
        {
            KangDepartureOrderOperation order => order.Remaining,
            KangJoinOperation join => new[] { join.Departure }.Concat(join.Following ?? []),
            _ => Enumerable.Empty<KangDeparture>(),
        }).Select(item => new { item.Defeated, item.Source.Id, item.CompletedScheme,
            Group = item.Group.Select(area => area.Player).ToArray() }).ToArray(),
    });

    private static void AssertCentral(World world, bool expert, int dominions, Card identity, List<GameEvent> events)
    {
        var central = world.GameAreaOf(PlayArea.Villains);
        Assert.NotNull(central);
        Assert.All(world.Seats, seat => Assert.Same(central, world.GameAreaOf(PlayArea.Of(seat.Index))));
        Assert.Equal(expert ? "11039" : "11006", Assert.Single(world.AreaOf(DeckType.VillainArea).Cards).FaceId);
        Assert.Equal("11013b", Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards).FaceId);
        Assert.Equal(dominions, world.Cards.Count(card => card.FaceId == "11023"
            && card.Area.Type == DeckType.SideSchemesArea && card.FaceUp));
        Assert.All(world.Seats, seat => Assert.Equal(DeckType.EngagedEnemiesArea,
            Assert.Single(seat.NemesisSet, card => Cards.IsNemesisMinion(card.FaceId)).Area.Type));
        Assert.Single(events.OfType<PlayAreaJoined>(), item => item.PlayArea == -1);
        int shuffle = events.FindLastIndex(item => item is AreaReordered { Trigger: "11013b", Verb: "Shuffle" });
        int ready = events.FindLastIndex(item => item is FieldSet field && field.Card == identity.ObjectId && field.Verb == "Ready");
        Assert.True(shuffle >= 0 && ready > shuffle);
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }
}
