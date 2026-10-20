using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic authored fixtures for captive return and aggregate printed-cost instructions.</summary>
public sealed class CampaignCaptiveReleaseTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void DefeatReturnsEveryCaptiveToItsPhysicalOwnerWithoutDiscardingThem(bool removeHost)
    {
        // When Defeated resolves before the defeated card leaves play.
        var (world, scheme, first, second) = Board();
        var runner = Runner($$$"""{"releaseBeneath":{"removeHost":"{{{removeHost.ToString().ToLowerInvariant()}}}"}}""",
            "WhenDefeated", "WhenCardDefeated");
        world.Abilities = runner;
        var events = new List<GameEvent>();

        Defeat.Scheme(world, Cards, scheme, "synthetic defeat", events, by: 1, recordOn: new Occurrence(1, []));
        Sequence.Finish(world, Cards, runner, events);

        Assert.Same(world.Seats[0].Hand, first.Area);
        Assert.Same(world.Seats[1].Hand, second.Area);
        Assert.True(first.FaceUp);
        Assert.True(second.FaceUp);
        Assert.Equal(removeHost ? DeckType.RemovedArea : DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), item => item.Verb == "Discard"
            && item.Cards.Any(card => card.Card == first.ObjectId || card.Card == second.ObjectId));
    }

    [Fact]
    public void PrisonAddsTheTotalPrintedCostInOneThreatPlacement()
    {
        var (world, scheme, _, _) = Board();
        var runner = Runner("""{"placeThreat":{"scheme":"this","amount":{"sumPrintedCost":{"query":"beneathThis"}}}}""",
            "WhenRevealed", "WhenCardRevealed");
        world.Abilities = runner;

        var resolution = runner.ExecuteReveal(world, scheme, 0);

        var events = resolution.Events.ToList();
        Sequence.Finish(world, Cards, runner, events);
        Assert.True(resolution.IsComplete);
        Assert.Equal(6, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(events.OfType<FieldSet>(), item => item.Field == "k_threat");
        Assert.All(BeneathCards.Of(world, scheme), card => Assert.False(card.FaceUp));
    }

    private static AbilityRunner Runner(string effect, string timing, string occurrence) => new(AbilityCatalog.Parse($$$"""
        {"cards":[{"card":"04122","abilities":[{
          "trigger":{"event":"{{{occurrence}}}","timing":"{{{timing}}}","subject":"this"},
          "effect":{{{effect}}}
        }]}]}
        """));

    private static (World World, Card Scheme, Card First, Card Second) Board()
    {
        var world = new World(Cards, 2);
        for (int i = 0; i < 2; i++)
        {
            var seat = world.CreateSeat($"p{i}");
            seat.IdentityCard = world.CreateCard(i == 0 ? "01001a" : "04031a", seat.Hero);
        }
        var scheme = world.CreateCard("04122", world.AreaOf(DeckType.SideSchemesArea));
        var first = world.CreateCard("01002", world.Seats[0].Hand);
        var second = world.CreateCard("04032", world.Seats[1].Hand);
        BeneathCards.Place(world, first, scheme, false, "synthetic setup", []);
        BeneathCards.Place(world, second, scheme, false, "synthetic setup", []);
        return (world, scheme, first, second);
    }
}
