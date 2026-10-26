using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PrintedTextSummaryTests
{
    [Fact]
    public void ActualRobotMeaningRetainsHeroMentalCostAndPhaseExpiry()
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var abilities = AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        var world = new World(catalog, 1, seed: 7) { Abilities = new AbilityRunner(abilities) };
        Seat seat = world.CreateSeat("P0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        Card robot = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var visible = WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World;
        BoardCardPresentation card = Assert.Single(BoardPresentation.From(visible).Areas.SelectMany(area => area.Cards),
            candidate => candidate.TargetId == robot.ObjectId);

        Assert.Contains("Hero action: Spend [mental] → blank this card's printed text box until the end of the phase",
            CardSourceSummary.AbilityLines(card));
        Assert.DoesNotContain("printedTextBlanked", string.Join(" ", CardSourceSummary.Lines(card)));
        Assert.Empty(CardSourceSummary.Lines(card with { Concealed = true }));
    }
}
