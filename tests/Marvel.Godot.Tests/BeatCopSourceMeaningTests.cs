using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class BeatCopSourceMeaningTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void ActualProjectedSupportThreatRemainsInAccessibleSourceSummary(int threat)
    {
        var catalog = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(catalog, 1, seed: 7);
        var seat = world.CreateSeat("P0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var cop = world.CreateCard("10029", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        BoardCardPresentation Project() => Assert.Single(BoardPresentation.From(WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World).Areas
            .SelectMany(area => area.Cards), card => card.FaceId == "10029");
        Assert.DoesNotContain(CardSourceSummary.Lines(Project()), line => line.Contains("threat here", StringComparison.Ordinal));
        cop.PlaceTokens("k_threat", threat);
        var shown = Project();
        Assert.Contains($"{threat} threat here", CardSourceSummary.Lines(shown));
        Assert.Empty(CardSourceSummary.Lines(shown with { Concealed = true }));
        Assert.DoesNotContain(CardSourceSummary.AbilityLines(shown), line => line.Contains("threat here", StringComparison.Ordinal));
    }
    [Fact]
    public void SourceNamedActionsExposeDifferentSuppliedCostsBeforeSelection()
    {
        var move = new AffordancePresentation(1, "Beat Cop", null, "Action", "Beat Cop", 8, 0, null, "", [])
        { CostDescription = "Exhaust Beat Cop." };
        var discard = move with { Id = 2, CostDescription = "Exhaust Beat Cop. Discard Beat Cop." };
        Assert.Equal("Beat Cop · Exhaust Beat Cop.", DecisionCopy.Choice(move));
        Assert.Equal("Beat Cop · Exhaust Beat Cop. Discard Beat Cop.", DecisionCopy.Choice(discard));
        Assert.Equal("Beat Cop", DecisionCopy.Choice(move with { CostDescription = null }));
    }}
