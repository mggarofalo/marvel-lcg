using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

public sealed class IndependentNextTurnDurationTests
{
    [Fact]
    [Rule("rr:lasting-effects.5")]
    public void EffectsCreatedOnOppositeSidesOfBeginningExpireOnDifferentTurns()
    {
        // rr:lasting-effects.5: a lasting effect expires when its specified timing point is reached.
        var world = new World(new Facts(), players: 1);
        var seat = world.CreateSeat("first");
        seat.IdentityCard = world.CreateCard("identity", seat.Hero);
        var before = new ContinuousEffect(EffectSource.LastingEffect, "attack", 1,
            Affects: seat.IdentityCard.ObjectId, Lasts: Duration.UntilNextTurnEnds(0));
        var after = new ContinuousEffect(EffectSource.LastingEffect, "thwart", 1,
            Affects: seat.IdentityCard.ObjectId, Lasts: Duration.UntilNextTurnEnds(0));
        world.Effects.Register(before);
        world.Effects.BeginNextTurn(0);
        world.Effects.Register(after);
        PhaseEnd.EndPlayerTurn(world, 0, []);
        Assert.DoesNotContain(before, world.Effects.Registered);
        Assert.Contains(after, world.Effects.Registered);
        world.Effects.BeginNextTurn(0);
        PhaseEnd.EndPlayerTurn(world, 0, []);
        Assert.Empty(world.Effects.Registered);
    }

    [Fact]
    [Rule("rr:lasting-effects.1")]
    [Rule("rr:leaves-play.1")]
    public void ReturningSourceDoesNotDuplicateOrRenewItsLastingEffect()
    {
        // rr:lasting-effects.1: the effect continues whether its source is in play.
        // rr:leaves-play.1: the returning card is a new copy.
        var world = new World(new Facts(), players: 1);
        var seat = world.CreateSeat("first");
        seat.IdentityCard = world.CreateCard("identity", seat.Hero);
        var area = world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0);
        var source = world.CreateCard("source", area);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, source.ObjectId, seat.IdentityCard.ObjectId,
            Duration.UntilNextTurnEnds(0)));
        world.Effects.BeginNextTurn(0);
        Discard.Card(world, source, "test", []);
        World.MoveToTop(source, area);
        Assert.False(FormChangePermissions.CanChange(world, seat.IdentityCard));
        Assert.Single(world.Effects.Registered);
        PhaseEnd.EndPlayerTurn(world, 0, []);
        Assert.True(FormChangePermissions.CanChange(world, seat.IdentityCard));
        Assert.Empty(world.Effects.Registered);
    }
    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Unknown;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
