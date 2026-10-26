using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Timing;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeFoiledTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:scheme-enemy-activation.step.2")]
    public void FoiledCancelsExactSchemeCardIncludingSyntheticNestedOccurrence(bool nested)
    {
        // "Resolve the following steps one card at a time" preserves each boost.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = Runner();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var source = world.CreateCard("09038", world.Seats[1].Hand);
        var second = world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        // Weapons Runner's star puts itself into play; its numeric icons are separate.
        var first = world.CreateCard("01121", world.AreaOf(DeckType.BoostCardsDeck, villain.Area.PlayArea, host: villain.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "boost_const", 1, Affects: first.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfActivation)));
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var events = new List<GameEvent>();
        if (nested)
        {
            world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));
            var attack = FindFaceup(world, events);
            Assert.DoesNotContain(attack.Affordances, a => a.AnchorId == source.ObjectId);
            // Synthetic suspension proves occurrence ownership; it is not a complete nested activation.
            world.Agenda.Now(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));
        }
        else world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));
        var prompt = FindFaceup(world, events, source.ObjectId);
        Assert.False(world.Activation!.Attacking);
        var occurrence = world.Agenda.Occurrence!;
        var physical = occurrence.Subject;
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId).Id), events);
        Assert.Equal(0, occurrence.BoostIcons!.Remaining);
        if (nested)
        {
            Assert.Equal(second.ObjectId, physical);
            Assert.NotNull(world.Attack);
            Assert.Equal(DeckType.DiscardPile, source.Area.Type);
            return;
        }
        for (int i = 0; i < 30; i++)
        {
            var remaining = Sequence.Work(world, world.Facts, runner, events);
            if (remaining is null) break;
            Assert.DoesNotContain(remaining.Affordances, a => a.AnchorId == source.ObjectId);
            Sequence.Answer(world, world.Facts, runner, remaining, Decision.Decline, events);
        }
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Contains(events.OfType<BoostResolved>(), e => e.Icons == 0);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
        Assert.Equal(DeckType.EngagedEnemiesArea, first.Area.Type);
        if (!nested)
        {
            Assert.Equal(first.ObjectId, physical);
            Assert.Equal(DeckType.EngagedEnemiesArea, first.Area.Type);
            Assert.Equal(new long[] { 0, 2 }, events.OfType<BoostResolved>().Select(e => e.Icons));
            Assert.Equal(3, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
            Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        }
    }

    [Fact]
    [Rule("rr:attack-enemy-activation.step.3")]
    public void FoiledDoesNotCancelAnAttackBoostCard()
    {
        // "Add all boost icons" applies; Foiled specifically requires a scheme activation.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = Runner();
        var source = world.CreateCard("09038", world.Seats[1].Hand);
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var boost = world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));
        var prompt = FindFaceup(world, events);
        Assert.True(world.Activation!.Attacking);
        Assert.Equal(boost.ObjectId, world.Agenda.Occurrence!.Subject);
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        for (int i = 0; i < 30; i++)
        {
            Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == source.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
            prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(2, Assert.Single(events.OfType<BoostResolved>()).Icons);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
    }

    private static AbilityRunner Runner()
    {
        // A synthetic generic observer exposes attack faceup windows where Foiled is absent.
        var json = System.Text.Json.Nodes.JsonNode.Parse(AuthoredCards.Text)!;
        var rows = json["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == "01092"));
        rows.Add(System.Text.Json.Nodes.JsonNode.Parse("""
            {"card":"01092","abilities":[{"trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","subject":"game"},"effect":{"draw":{"player":"you","count":1}}}]}
            """));
        return new(AbilityCatalog.Parse(json.ToJsonString()));
    }

    private static Prompt FindFaceup(World world, List<GameEvent> events, int? source = null)
    {
        var runner = Runner();
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events)!;
            Assert.NotNull(prompt);
            if (world.Agenda.Occurrence?.Is(BoostFaceupOperation.Faceup) == true
                && (source is null || prompt.Affordances.Any(a => a.AnchorId == source))) return prompt;
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        }
        throw new InvalidOperationException("No faceup window");
    }
}
