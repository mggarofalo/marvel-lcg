using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic card overlay: phase timing source is synthetic; Kang's defeat row is actual.
public sealed class KangStageTwoTimingReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("ForcedInterrupt", true)]
    [InlineData("ForcedResponse", false)]
    [Rule("rr:interrupt.3")]
    [Rule("rr:delayed-effect.1")]
    public void DefeatDuringPhaseEndingJoinsAtTheNextActualEnding(string timing, bool sameEnding)
    {
        // Interrupt resolves "before that triggering condition resolves"; delayed effects
        // resolve immediately after their timing point and before its responses.
        var world = new World(Cards, 2, 42) { FirstPlayer = 1 };
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01001a,01001b" : "01019a,01019b", seat.Hero);
            world.CreateCard("01089", seat.Deck);
            world.Join(PlayArea.Of(player), world.CreateGameArea(), "prepared split", []);
        }
        world.Detach(PlayArea.Villains, "prepared split", []);
        var source = world.GameAreaOf(PlayArea.Of(0));
        var destination = world.GameAreaOf(PlayArea.Of(1));
        var kang = world.CreateCard("11004", world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        var scheme = world.CreateCard("11011b", world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)));
        world.CreateCard("11003", world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)));
        world.CreateCard("11010b", world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(1)));
        world.CreateCard("01011", world.Seats[0].Hero);
        var rows = new object[] {
            new { card = "11004", abilities = new[] { new {
                trigger = new { @event = "WhenCardDefeated", timing = "WhenDefeated", subject = "this" },
                effect = new { captureKangStageTwoDefeat = "this" } } } },
            new { card = "01011", abilities = new[] { new {
                trigger = new { @event = "WhenPlayerPhaseEnds", timing, subject = "game" },
                effect = new { dealDamage = new { cards = new { titled = "Kang (Rama-Tut)" }, amount = 100 } } } } }
        };
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(JsonSerializer.Serialize(new { cards = rows })));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 1, 5));
        var prompt = Sequence.Work(world, Cards, world.Abilities, events);
        Assert.Equal(DeckType.RemovedArea, kang.Area.Type);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.Same(source, world.GameAreaOf(PlayArea.Of(0)));
        if (!sameEnding)
        {
            Assert.Null(prompt);
            Assert.Single(world.Agenda.KangTransitions.Pending);
            world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase), 1, 6));
            prompt = Sequence.Work(world, Cards, world.Abilities, events);
        }
        var join = Assert.IsType<Prompt>(prompt);
        Assert.Equal(1, join.Player);
        Assert.Equal(destination!.Id, Assert.Single(join.Affordances).Id);
        Sequence.Answer(world, Cards, world.Abilities, join, Decision.Take(destination.Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Same(destination, world.GameAreaOf(PlayArea.Of(0)));
        Assert.Single(events.OfType<PlayAreaJoined>());
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }
}
