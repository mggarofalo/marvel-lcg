using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HarvestActualIndependentTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:exhausted.1")]
    [Rule("rr:surge.1")]
    public void SecondPrintedHarvestSurgesDespitePriorExhaustionAndReadyOutOfPlayPersonas(int resolver)
    {
        // "An exhausted card cannot be exhausted again until it is ready."
        // Surge: "When Revealed: Deal yourself 1 facedown encounter card."
        // 05029: "If no Persona support was exhausted this way, this card gains surge."
        var runner = AuthoredCards.Runner();
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int index = 0; index < 4; index++)
            world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var persona = world.CreateCard("05006", world.AreaOf(
            DeckType.SupportsArea, PlayArea.Of(1 - resolver), cardOwner: 1 - resolver));
        var ordinary = world.CreateCard("01091", world.AreaOf(
            DeckType.SupportsArea, PlayArea.Of(resolver), cardOwner: resolver));
        var hand = world.CreateCard("05007", world.Seats[resolver].Hand);
        var deck = world.CreateCard("05008", world.AreaOf(DeckType.PlayerDeck, PlayArea.Of(resolver)));
        var discard = world.CreateCard("01006", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(resolver)));
        var fixture = new RedSkullFixture(world, runner);
        villain.TakeDamage(5);
        var first = fixture.Reveal("05029", 1 - resolver);
        fixture.Finish();
        Assert.False(persona.Ready);
        Assert.Equal(4, villain.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(4, world.AreaOf(DeckType.EncounterDeck).Cards.Count);

        var second = world.CreateCard("05029", world.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(resolver)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4));
        var events = fixture.Finish();

        Assert.Equal(4, villain.Damage);
        Assert.DoesNotContain(events.OfType<FieldSet>(), change => change.Verb == "Exhaust");
        Assert.All(new[] { ordinary, hand, deck, discard }, card => Assert.True(card.Ready));
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        Assert.Single(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(resolver)).Cards);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1 - resolver)).Cards);
        Assert.Equal(3, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(resolver)).Cards);
        Assert.Empty(world.AreaOf(DeckType.RevealingArea).Cards);
        Assert.False(world.Agenda.IsBusy);
    }
}
