using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SurgeHistoryReviewTests
{
    [Fact]
    public void AdmittedCoreGainedSurgeRetainsItsRecordedAbilityTrigger()
    {
        // Event spelling is an engine compatibility choice. Existing admitted histories
        // record the originating When Revealed trigger on a gained-surge deal.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(cards, 1, 8472) { Abilities = runner };
        var seat = world.CreateSeat("Peter");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 10; i++) world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck));
        var original = world.CreateCard("01104", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4, Plan: true));
        var events = new List<GameEvent>();
        Sequence.Finish(world, cards, runner, events);
        Assert.Equal(DeckType.EncounterDiscardPile, original.Area.Type);
        Assert.Single(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        var deal = Assert.Single(events.OfType<CardsMoved>(), e => e.Verb == "Deal");
        Assert.Equal("WhenCardRevealed", deal.Trigger);
    }
}
