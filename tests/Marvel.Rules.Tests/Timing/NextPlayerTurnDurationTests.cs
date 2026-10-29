using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

public sealed class NextPlayerTurnDurationTests
{
    [Fact]
    [Rule("rr:lasting-effects.1")]
    [Rule("rr:lasting-effects.5")]
    public void NextTurnBelongsToTheSpecifiedSeatAndOutlivesItsSource()
    {
        // rr:lasting-effects.1: the effect lasts whether its source remains in play.
        // rr:lasting-effects.5: it expires as soon as its specified timing point is reached.
        var world = new World(new Facts(), players: 2);
        var first = world.CreateSeat("first");
        var second = world.CreateSeat("second");
        first.IdentityCard = world.CreateCard("first", first.Hero);
        second.IdentityCard = world.CreateCard("second", second.Hero);
        var source = world.CreateCard("source", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, source.ObjectId, first.IdentityCard.ObjectId,
            Duration.UntilNextTurnEnds(0)));
        World.MoveToTop(source, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        Assert.False(FormChangePermissions.CanChange(world, first.IdentityCard));
        Assert.True(FormChangePermissions.CanChange(world, second.IdentityCard));
        PhaseEnd.EndPlayerTurn(world, 0, []);
        world.Effects.BeginNextTurn(1);
        PhaseEnd.EndPlayerTurn(world, 1, []);
        Assert.False(FormChangePermissions.CanChange(world, first.IdentityCard));
        world.Effects.BeginNextTurn(0);
        Assert.False(FormChangePermissions.CanChange(world, first.IdentityCard));
        PhaseEnd.EndPlayerTurn(world, 0, []);
        Assert.True(FormChangePermissions.CanChange(world, first.IdentityCard));
    }

    [Fact]
    [Rule("rr:lasting-effects.5")]
    public void OrdinaryEndOfTurnStillExpiresAtTheCurrentTurnEnd()
    {
        // rr:lasting-effects.5: expiry occurs at the specified timing point.
        var world = new World(new Facts(), players: 1);
        world.CreateSeat("first");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attack", 1,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfTurn)));
        PhaseEnd.EndPlayerTurn(world, 0, []);
        Assert.Empty(world.Effects.Registered);
    }
    [Fact]
    [Rule("rr:lasting-effects.5")]
    public void SimultaneousHealthExpiryDoesNotDefeatAnAllyThatSurvivesTheFinalValue()
    {
        // rr:lasting-effects.5: each effect expires at its stated timing point.
        var world = new World(new Facts(), players: 1);
        world.CreateSeat("first");
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(1);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "health", 2,
            Affects: ally.ObjectId, Lasts: Duration.UntilNextTurnEnds(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "health", -1,
            Affects: ally.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfTurn)));
        world.Effects.BeginNextTurn(0);
        PhaseEnd.EndPlayerTurn(world, 0, []);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(1, ally.Damage);
        Assert.Empty(world.Effects.Registered);
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId == "ally" ? CardKind.Ally : CardKind.Unknown;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => faceId == "ally" && attribute == "HP" ? 2 : fallback;
    }
}
