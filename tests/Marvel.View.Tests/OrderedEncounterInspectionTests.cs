using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

/// <summary>Synthetic primitive proof; no Heimdall runtime admission.</summary>
public sealed class OrderedEncounterInspectionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [Rule("rr:look-looked-at")]
    [Rule("rr:look-looked-at.1")]
    [Rule("rr:discard.5")]
    [Rule("rr:encounter-deck.1")]
    public void PrivatePhysicalGroupDiscardsMiddleAndOrdersSurvivors(int count)
    {
        // "only the player who is resolving the ability can look"; looked-at
        // cards "are still considered part of that deck" (rr:look-looked-at).
        // rr:discard.5 considers the selected looked-at card discarded "from the top".
        // rr:encounter-deck.1: an empty deck is "immediately shuffled".
        var (world, facts, runner, source) = Fixture(count);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var initial = deck.Cards.Reverse().Take(3).Select(card => card.ObjectId).ToArray();
        string before = Snapshot(world);
        for (int repeat = 0; repeat < 3; repeat++)
        {
            var preview = runner.PreviewChoice(world, source, 1, 0)!;
            Assert.Empty(preview.ContextCardIds);
            Assert.False(preview.ExposesConcealedCandidates);
            Assert.Empty(preview.Affordances.Single().Targets?.Legal ?? []);
            Assert.Equal(before, Snapshot(world));
        }
        var action = Assert.Single(runner.Actions(world, 1));
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, facts, runner, events)!;
        Sequence.Answer(world, facts, runner, prompt, Decision.Take(0), events);
        prompt = Sequence.Work(world, facts, runner, events)!;
        Assert.Equal(1, prompt.Player);
        Assert.Equal(initial, prompt.ContextCardIds);
        Assert.Equal(PublicDecisionKind.CardLook, prompt.PublicKind);
        Assert.Contains("Discard", prompt.DisplayQuestion);
        Assert.Equal(initial.Length, Project(world, prompt, 1).World.Areas.Single(area => area.Id == deck.Id).Cards.Count(card => card.Face is not null));
        Assert.Null(Project(world, prompt, 0).Prompt);
        Assert.All(Project(world, prompt, 0).World.Areas.Single(area => area.Id == deck.Id).Cards,
            card => { Assert.Null(card.Face); Assert.Null(card.Id); });
        int discarded = initial[initial.Length / 2];
        string choosing = Snapshot(world);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, facts, runner, prompt,
            Decision.Take(source.ObjectId), events));
        Assert.Equal(choosing, Snapshot(world));
        Sequence.Answer(world, facts, runner, prompt, Decision.Take(discarded), events);
        var remaining = initial.Where(id => id != discarded).Reverse().ToArray();
        if (remaining.Length > 0)
        {
            prompt = Sequence.Work(world, facts, runner, events)!;
            Assert.Contains("first selected", prompt.Description);
            Assert.Equal(Question.Order, prompt.Asking);
            string pending = Snapshot(world);
            int[] invalid = remaining.Length > 1 ? [remaining[0], remaining[0]] : [discarded];
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, facts, runner, prompt,
                new Decision(source.ObjectId, invalid), events));
            Assert.Equal(pending, Snapshot(world));
            Sequence.Answer(world, facts, runner, prompt, new Decision(source.ObjectId, remaining), events);
        }
        Sequence.Finish(world, facts, runner, events);
        if (count == 1)
        {
            Assert.Contains(world.Cards[discarded], deck.Cards);
            Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens[EncounterDeck.AccelerationToken]);
            Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Verb == "Discard" && moved.Cards.Any(card => card.Card == discarded));
        }
        else
        {
            Assert.Equal(remaining, deck.Cards.Reverse().Take(remaining.Length).Select(card => card.ObjectId));
            Assert.Equal(DeckType.EncounterDiscardPile, world.Cards[discarded].Area.Type);
            Assert.True(world.Cards[discarded].FaceUp);
        }
        Assert.All(Project(world, null, 1).World.Areas.Single(area => area.Id == deck.Id).Cards, card => Assert.Null(card.Face));
        Assert.DoesNotContain(WorldProjection.For(world, null, events,
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).Events, happened => happened is AreaReordered);
    }

    [Fact]
    public void StalePhysicalGroupRejectsWithoutConsumingPendingOrRandomness()
    {
        var (world, facts, runner, _) = Fixture(3);
        var action = Assert.Single(runner.Actions(world, 1));
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, facts, runner, events)!;
        Sequence.Answer(world, facts, runner, prompt, Decision.Take(0), events);
        prompt = Sequence.Work(world, facts, runner, events)!;
        var top = world.Cards[prompt.ContextCardIds[0]];
        World.MoveToTop(top, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        World.MoveToTop(top, world.AreaOf(DeckType.EncounterDeck));
        string before = Snapshot(world);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, facts, runner, prompt, Decision.Take(top.ObjectId), events));
        Assert.Equal(before, Snapshot(world));
    }

    [Fact]
    public void EmptyDeckDoesNotOfferInspection()
    {
        var (world, _, runner, _) = Fixture(0);
        Assert.Empty(runner.Actions(world, 1));
    }

    private static (World World, CardCatalog Facts, AbilityRunner Runner, Card Source) Fixture(int count)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 19);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int index = 0; index < count; index++) world.CreateCard(index % 2 == 0 ? "01104" : "01121", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDiscardPile));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01001a","abilities":[{
            "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
            "effect":{"inspectDiscardOrderEncounterTop":3}}]}]}
            """));
        world.Abilities = runner;
        return (world, facts, runner, world.Seats[1].IdentityCard);
    }

    private static VisibleResult Project(World world, Prompt? prompt, int seat) =>
        WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(seat).Authorize(null, world.Players));

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Agenda = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject, step.Plan }),
    });
}
