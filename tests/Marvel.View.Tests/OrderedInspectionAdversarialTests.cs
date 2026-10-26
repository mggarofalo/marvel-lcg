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

/// <summary>Independent synthetic inspection boundary tests; no expansion admission.</summary>
public sealed class OrderedInspectionAdversarialTests
{
    [Fact]
    [Rule("rr:look-looked-at")]
    [Rule("rr:encounter-deck.1")]
    public void InvalidAnswersPreservePendingAndAllowExactRetryWithoutShuffle()
    {
        // rr:look-looked-at: "only the player who is resolving the ability can look".
        // rr:encounter-deck.1 permits order changes only when "instructed to do so".
        var (world, facts, runner, events, discard) = Begin();
        int[] initial = discard.ContextCardIds.ToArray();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        int bottom = deck.Cards[0].ObjectId;
        string before = Snapshot(world, events);
        foreach (var answer in new[] { Decision.Decline, Decision.Take(-100), Decision.Take(bottom) })
        {
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, facts, runner, discard, answer, events));
            Assert.Equal(before, Snapshot(world, events));
        }
        Sequence.Answer(world, facts, runner, discard, Decision.Take(initial[1]), events);
        var order = Sequence.Work(world, facts, runner, events)!;
        int[] survivors = [initial[2], initial[0]];
        before = Snapshot(world, events);
        foreach (int[] targets in new int[][] { [], [survivors[0]], [survivors[0], survivors[0]], [initial[1], survivors[0]], [bottom, survivors[0]], [survivors[0], survivors[1], bottom] })
        {
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, facts, runner, order,
                new Decision(order.Affordances[0].Id, targets), events));
            Assert.Equal(before, Snapshot(world, events));
        }
        Sequence.Answer(world, facts, runner, order, new Decision(order.Affordances[0].Id, survivors), events);
        Sequence.Finish(world, facts, runner, events);
        Assert.Equal(new[] { bottom, survivors[1], survivors[0] }, deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(0L, world.Random.Generator.WordsConsumed);
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        var observer = WorldProjection.For(world, null, events, new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        Assert.DoesNotContain(observer.Events, happened => happened is AreaReordered);
        Assert.All(observer.World.Areas.Single(area => area.Id == deck.Id).Cards, card => { Assert.Null(card.Face); Assert.Null(card.Id); });
        var publicDiscard = observer.World.Areas.Single(area => area.Id == world.AreaOf(DeckType.EncounterDiscardPile).Id);
        Assert.Contains(publicDiscard.Cards, card => card.Id == initial[1] && card.Face is not null);
        Assert.All(WorldProjection.For(world, null, [], new RestrictedVisibilityPolicy(1).Authorize(null, world.Players))
            .World.Areas.Single(area => area.Id == deck.Id).Cards, card => Assert.Null(card.Face));
    }

    [Theory]
    [InlineData(false, "order")]
    [InlineData(false, "membership")]
    [InlineData(false, "incarnation")]
    [InlineData(true, "order")]
    [InlineData(true, "membership")]
    [InlineData(true, "incarnation")]
    public void FrozenGroupRejectsChangedPhysicalCardsAtEitherStage(bool ordering, string mutation)
    {
        var (world, facts, runner, events, prompt) = Begin();
        if (ordering)
        {
            Sequence.Answer(world, facts, runner, prompt, Decision.Take(prompt.ContextCardIds[1]), events);
            prompt = Sequence.Work(world, facts, runner, events)!;
        }
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var top = world.Cards[prompt.ContextCardIds[0]];
        if (mutation == "order") World.MoveToTop(world.Cards[prompt.ContextCardIds[^1]], deck);
        else
        {
            World.MoveToTop(top, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            if (mutation == "incarnation") World.MoveToTop(top, deck);
        }
        string before = Snapshot(world, events);
        var answer = ordering ? new Decision(prompt.Affordances[0].Id, prompt.ContextCardIds) : Decision.Take(top.ObjectId);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, facts, runner, prompt, answer, events));
        Assert.Equal(before, Snapshot(world, events));
    }

    private static (World World, CardCatalog Facts, AbilityRunner Runner, List<GameEvent> Events, Prompt Prompt) Begin()
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 19);
        for (int index = 0; index < 2; index++)
        {
            var seat = world.CreateSeat($"p{index}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int index = 0; index < 4; index++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01001a","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"inspectDiscardOrderEncounterTop":3}
            }]}]}
            """));
        world.Abilities = runner;
        var events = runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 1)), [], [])).Events.ToList();
        var look = Sequence.Work(world, facts, runner, events)!;
        Sequence.Answer(world, facts, runner, look, Decision.Take(0), events);
        return (world, facts, runner, events, Sequence.Work(world, facts, runner, events)!);
    }

    private static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage, Pending = world.Agenda.Outstanding.ToArray(), Events = events.Select(EventJson.Write),
    });
}
