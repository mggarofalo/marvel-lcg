using Marvel.Rules.State;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Xunit;
using static Marvel.View.Tests.VisibilityFixture;

namespace Marvel.View.Tests;

public sealed class CardRetaliatePresentationTests
{
    [Fact]
    public void PublicRetaliationFollowsTheEvaluatedValueForBothViewerPolicies()
    {
        var world = Board();
        Card villain = world.AreaOf(DeckType.VillainArea).Cards[0];
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "retaliate", Amount: 2, Affects: villain.ObjectId));
        ViewScope[] scopes = [new RestrictedVisibilityPolicy(0).Authorize(null, world.Players),
            new PermissiveVisibilityPolicy().Authorize(null, world.Players)];
        var digest = world.Digest().Canonical();
        foreach (ViewScope scope in scopes)
        {
            WorldDescriptor descriptor = WorldProjection.For(world, null, [], scope).World;
            BoardCardPresentation card = BoardPresentation.From(descriptor).Areas
                .SelectMany(area => area.Cards).Single(card => card.TargetId == villain.ObjectId);
            Assert.Equal(2, card.Retaliate);
            Assert.Equal(digest, world.Digest().Canonical());
        }
    }

    [Theory]
    [InlineData("VillainArea", true, 3L)]
    [InlineData("VillainArea", false, null)]
    [InlineData("EncounterDiscardPile", true, null)]
    public void RetaliationIsNotCarriedByHiddenOrOutOfPlayFaces(string zone, bool visible, long? expected)
    {
        var face = new CardFaceDescriptor("test", "Visible villain", "", CardKind.EncounterVillain,
            new Dictionary<string, long> { ["retaliate"] = 3 });
        var descriptor = new CardDescriptor(1, CardBack.Encounter, visible, true, -1, visible ? face : null);
        var world = new WorldDescriptor([], [new AreaDescriptor(1, zone, -1, -1, [descriptor], [])], [], Outcome.Unfinished);
        BoardCardPresentation card = Assert.Single(Assert.Single(BoardPresentation.From(world).Areas).Cards);
        Assert.Equal(expected, card.Retaliate);
    }
}
