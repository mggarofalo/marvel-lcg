using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Direct delayed effects retain the original copy, not a reusable card id.</summary>
public sealed class DelayedDiscardLifetimeTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:delayed-effect.1")]
    public void DepartedTargetIsNotMovedAtTheDelayedTimingPoint(bool removed)
    {
        // "When a card leaves play, there is no memory of its previous state
        // and it is considered to be a new copy of the card."
        var world = Deal();
        var fury = RegisterFury(world);
        Discard.Card(world, fury, "test departure", []);
        if (removed)
        {
            World.MoveToTop(fury, world.AreaOf(DeckType.RemovedArea));
        }
        var before = world.Digest().Canonical();
        var events = new List<GameEvent>();

        DelayedEffects.Occur(world, "WhenRoundEnds", events);

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(events);
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Kind == DelayedEffects.DiscardFromPlay);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void ReturningCopyIsNotDiscardedByThePreviousCopiesDelayedEffect()
    {
        // A returning card "is considered to be a new copy of the card."
        var world = Deal();
        var fury = RegisterFury(world);
        Discard.Card(world, fury, "test departure", []);
        World.MoveToTop(fury, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var events = new List<GameEvent>();

        DelayedEffects.Occur(world, "WhenRoundEnds", events);

        Assert.Equal(DeckType.AlliesArea, fury.Area.Type);
        Assert.Empty(events);
    }

    [Fact]
    [Rule("rr:delayed-effect.1")]
    public void OriginalCopyIsDiscardedWhenTheTimingPointOccurs()
    {
        // Delayed effects "resolve automatically and immediately after their
        // specified timing point or future condition occurs or becomes true".
        var world = Deal();
        var fury = RegisterFury(world);
        var events = new List<GameEvent>();

        DelayedEffects.Occur(world, "WhenRoundEnds", events);

        Assert.Equal(DeckType.DiscardPile, fury.Area.Type);
        Assert.NotEmpty(events);
    }

    private static Card RegisterFury(World world)
    {
        // Isolate the delayed-discard contract with a real owned Core card.
        var fury = world.Cards.Single(card => card.FaceId == "01084");
        World.MoveToTop(fury, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect, DelayedEffects.DiscardFromPlay,
            Card: fury.ObjectId, Affects: fury.ObjectId,
            Lasts: Duration.NextTime("WhenRoundEnds")));
        return fury;
    }
}
