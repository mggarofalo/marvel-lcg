using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

/// <summary>
/// The effects that are simply in force, and how each of the three kinds ends.
/// </summary>
public sealed class ContinuousEffectTests
{
    [Rule("rr:ability.step.1")]
    [Rule("rr:delayed-effect.1.1")]
    [Rule("rr:lasting-effects.2")]
    [Fact]
    public void AllThreeKindsShareOneTier()
    {
        // The rulebook says this three separate times, once per kind, which is
        // why they are one list rather than three.
        Assert.Equal(TimingPriority.Continuous, ContinuousEffect.Priority);
    }

    [Rule("rr:ability")]
    [Fact]
    public void AConstantAbilityIsInForceExactlyWhileItsCardIsInPlay()
    {
        // "A constant ability becomes active as soon as its card enters play
        // and remains active while the card is in play." That makes it a
        // function of the board, so it is derived rather than deregistered --
        // a forgotten deregistration would keep an ally's bonus counting from
        // the discard pile on a board that looks entirely normal.
        var world = Board();
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var effects = new ContinuousEffects();
        effects.Register(new ContinuousEffect(
            EffectSource.ConstantAbility, "attack", Amount: 1, Card: ally.ObjectId));

        Assert.Single(effects.Active(world));

        World.MoveToTop(ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));

        Assert.Empty(effects.Active(world));
        Assert.Single(effects.Registered);
    }

    [Rule("rr:ability.10")]
    [Fact]
    public void TwoCopiesOfTheSameConstantAbilityBothCount()
    {
        // "If multiple instances of the same constant ability are in play, each
        // instance affects the game independently." So the list is a list and
        // not a set, and registering the same shape twice is correct.
        var world = Board();
        var effects = new ContinuousEffects();
        foreach (var card in new[] { "ally", "ally" })
        {
            var made = world.CreateCard(card, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
            effects.Register(new ContinuousEffect(
                EffectSource.ConstantAbility, "attack", Amount: 1, Card: made.ObjectId));
        }

        Assert.Equal(2, effects.Active(world).Count);
        Assert.Equal(2, effects.Active(world).Sum(effect => effect.Amount));
    }

    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void ALastingEffectOutlivesTheCardThatMadeIt()
    {
        // "A lasting effect persists beyond the resolution of the ability that
        // created it." The event is in the discard pile and the effect is still
        // in force, which is exactly why this kind cannot be derived from the
        // board the way a constant ability is.
        var world = Board();
        var effects = new ContinuousEffects();
        var played = world.CreateCard("event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attack", Amount: -1,
            Card: played.ObjectId, Until: "EndOfPhase"));

        Assert.Single(effects.Active(world));
    }

    [Rule("rr:lasting-effects.4")]
    [Fact]
    public void ACardEnteringPlayAfterwardsIsStillAffected()
    {
        // "If a card enters play after the creation of a lasting effect, it is
        // still affected by that lasting effect." So the affected set cannot be
        // resolved when the effect is registered -- an entry names a condition
        // and the condition is re-read against the board every time.
        var world = Board();
        var effects = new ContinuousEffects();
        effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attack", Amount: 1,
            Affects: null, Until: "EndOfRound"));

        var before = effects.Active(world);
        world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));

        // Board-wide, so the entry itself is unchanged -- what changed is what
        // it now finds when it is applied.
        Assert.Equal(before.Count, effects.Active(world).Count);
        Assert.Null(effects.Active(world)[0].Affects);
    }

    [Rule("rr:lasting-effects.5")]
    [Rule("rr:villain-phase.step.6.a")]
    [Fact]
    public void ALastingEffectEndsAtTheTimingPointItNames()
    {
        // "Expires as soon as the timing point specified by its duration is
        // reached." Step 6a of the villain phase is one of those points, and
        // the villain phase does not currently have a step 6 at all -- so
        // nothing in this engine expires yet.
        var world = Board();
        var effects = new ContinuousEffects();
        effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attack", Amount: 1, Until: "EndOfPhase"));
        effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "thwart", Amount: 1, Until: "EndOfRound"));

        Assert.Equal(1, effects.Expire("EndOfPhase"));

        var left = Assert.Single(effects.Active(world));
        Assert.Equal("thwart", left.Kind);
    }

    [Rule("rr:delayed-effect.1")]
    [Fact]
    public void ADelayedEffectIsSpentByDisposingItsRegistration()
    {
        // "Delayed effects resolve automatically and immediately after their
        // specified timing point or future condition occurs." Once resolved it
        // is gone, and there is no card leaving play to signal that -- so this
        // is the case the explicit registration handle exists for.
        var world = Board();
        var effects = new ContinuousEffects();
        var registration = effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect, "deal-damage", Amount: 2, Until: "EndOfTurn"));

        Assert.Single(effects.Active(world));

        registration.Dispose();
        Assert.Empty(effects.Active(world));

        // Idempotent: a delayed effect that resolved and was also expired by
        // its timing point must not throw or remove somebody else's entry.
        registration.Dispose();
        Assert.Empty(effects.Registered);
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void TheListIsReadFreshRatherThanCachedOntoTheBoard()
    {
        // "The game constantly checks and (if necessary) updates the count of
        // any variable quantity that is being modified." Two reads either side
        // of a board change give different answers with nothing having told the
        // list that anything happened.
        var world = Board();
        var effects = new ContinuousEffects();
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        effects.Register(new ContinuousEffect(
            EffectSource.ConstantAbility, "attack", Amount: 1, Card: ally.ObjectId));

        Assert.Single(effects.Active(world));
        World.MoveToTop(ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        Assert.Empty(effects.Active(world));
    }

    private static World Board()
    {
        var world = new World(new Facts(), players: 1);
        world.CreateSeat("p0");
        return world;
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "ally" => CardKind.Ally,
            "event" => CardKind.Event,
            _ => CardKind.Unknown,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            fallback;
    }
}
