using Marvel.Rules.Events;
using Marvel.Rules.Play;
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
        var effects = world.Effects;
        effects.Register(new ContinuousEffect(
            EffectSource.ConstantAbility, "attack", Amount: 1, Card: ally.ObjectId, Lasts: Duration.WhileInPlay));

        Assert.Single(effects.Active());

        World.MoveToTop(ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));

        Assert.Empty(effects.Active());
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
        var effects = world.Effects;
        foreach (var card in new[] { "ally", "ally" })
        {
            var made = world.CreateCard(card, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
            effects.Register(new ContinuousEffect(
                EffectSource.ConstantAbility, "attack", Amount: 1, Card: made.ObjectId, Lasts: Duration.WhileInPlay));
        }

        Assert.Equal(2, effects.Active().Count);
        Assert.Equal(2, effects.Active().Sum(effect => effect.Amount));
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
        var effects = world.Effects;
        var played = world.CreateCard("event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attack", Amount: -1,
            Card: played.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase)));

        Assert.Single(effects.Active());
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
        var effects = world.Effects;
        effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attack", Amount: 1,
            Affects: null, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));

        var before = effects.Active();
        world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));

        // Board-wide, so the entry itself is unchanged -- what changed is what
        // it now finds when it is applied.
        Assert.Equal(before.Count, effects.Active().Count);
        Assert.Null(effects.Active()[0].Affects);
    }

    [Rule("rr:lasting-effects.1")]
    [Rule("rr:lasting-effects.3")]
    [Rule("rr:lasting-effects.4")]
    [Rule("rr:lasting-effects.5")]
    [Fact]
    public void APlayersCharactersRemainALiveLastingEffectSet()
    {
        var world = new World(new Facts(), players: 2);
        world.CreateSeat("p0");
        world.CreateSeat("p1");
        world.Seats[0].IdentityCard = world.CreateCard("identity", world.Seats[0].Hero);
        world.Seats[1].IdentityCard = world.CreateCard("identity", world.Seats[1].Hero);
        var source = world.CreateCard(
            "event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var theirs = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));

        world.Effects.GrantToCharactersControlledBy(
            source, player: 0, field: "attack", amount: 1,
            until: TimingPoints.EndOfPlayerPhase);

        Assert.Equal(1, StateFields.Modified(
            world, world.Seats[0].IdentityCard, "attack", world.Facts, world.Players));
        Assert.Equal(1, StateFields.Modified(world, first, "attack", world.Facts, world.Players));
        Assert.Equal(0, StateFields.Modified(world, theirs, "attack", world.Facts, world.Players));

        // Created after registration: the affected set is a predicate, not a
        // snapshot of the cards that happened to exist at resolution time.
        var later = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal(1, StateFields.Modified(world, later, "attack", world.Facts, world.Players));

        world.Effects.Expire(TimingPoints.EndOfPlayerPhase);
        Assert.Equal(0, StateFields.Modified(world, first, "attack", world.Facts, world.Players));
        Assert.Equal(0, StateFields.Modified(world, later, "attack", world.Facts, world.Players));
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
        var effects = world.Effects;
        effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attack", Amount: 1, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPlayerPhase)));
        effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "thwart", Amount: 1, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));

        Assert.Equal(1, effects.Expire(TimingPoints.EndOfPlayerPhase));

        var left = Assert.Single(effects.Active());
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
        var effects = world.Effects;
        var registration = effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect, "deal-damage", Amount: 2, Lasts: Duration.UntilEndOf(TimingPoints.EndOfTurn)));

        Assert.Single(effects.Active());

        registration.Dispose();
        Assert.Empty(effects.Active());

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
        var effects = world.Effects;
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        effects.Register(new ContinuousEffect(
            EffectSource.ConstantAbility, "attack", Amount: 1, Card: ally.ObjectId, Lasts: Duration.WhileInPlay));

        Assert.Single(effects.Active());
        World.MoveToTop(ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        Assert.Empty(effects.Active());
    }

    [Rule("rr:modifiers.2")]
    [Rule("rr:traits.1")]
    [Fact]
    public void AConstantCanDependOnATraitGrantedByAnotherConstant()
    {
        // Modifiers are simultaneous, and traits are game attributes other
        // abilities may reference. The hand-size effect must therefore see the
        // granted TECH trait, even when neither constant appeared in the first
        // pass through the other one's condition.
        var world = Board();
        var identity = world.CreateCard("identity", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = identity;
        var upgrade = world.CreateCard(
            "upgrade", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = new TraitDependentConstants(identity.ObjectId, upgrade.ObjectId);

        var active = world.Effects.Active();

        Assert.Contains(active, effect =>
            effect.Kind == Traits.Granted + "TECH" && effect.Affects == upgrade.ObjectId);
        Assert.Contains(active, effect =>
            effect.Kind == "hand_size" && effect.Affects == identity.ObjectId
            && effect.Amount == 1);
    }

    [Fact]
    public void AConstantDependencyThatOscillatesIsRefusedAndDoesNotPoisonLaterReads()
    {
        // There is no stable simultaneous answer to "gain TECH exactly while
        // you do not have TECH". Choosing either intermediate pass would make
        // a plausible, wrong board, so the engine names the non-game state.
        var world = Board();
        var card = world.CreateCard(
            "upgrade", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = new OscillatingConstant(card.ObjectId);

        var thrown = Assert.Throws<RulesNotImplementedException>(() => world.Effects.Active());
        Assert.Contains("do not settle", thrown.Message, StringComparison.Ordinal);

        world.Abilities = new NoCardAbilities();
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void AnEffectBoundedByUseIsSpentByBeingUsed()
    {
        // "Reduce the cost of the next card you play by 1" states a duration
        // that is not a timing point at all: it ends when a card is played,
        // whenever that is. Nothing expires it and no card leaves play.
        var world = Board();
        var effects = world.Effects;
        var discount = new ContinuousEffect(
            EffectSource.LastingEffect, "cost", Amount: -1, Lasts: Duration.NextUses(1));
        effects.Register(discount);

        Assert.Single(effects.Active());
        Assert.True(effects.Use(discount));

        Assert.Empty(effects.Active());
        Assert.False(effects.Use(discount));
    }

    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void AnEffectBoundedBothWaysEndsAtWhicheverComesFirst()
    {
        // "Reduce the cost of the next ally you play this phase by 1" carries a
        // use and a timing point. Play the ally and it is gone; play nothing and
        // the phase ending takes it. Modelling only one bound would leave a
        // discount available next round, or spend one that was never used.
        var world = Board();
        var used = new ContinuousEffect(
            EffectSource.LastingEffect, "cost", Amount: -1,
            Lasts: new Duration(Until: TimingPoints.EndOfPlayerPhase, Uses: 1));
        var expired = used with { Kind = "cost-other" };

        world.Effects.Register(used);
        world.Effects.Register(expired);

        Assert.True(world.Effects.Use(used));
        Assert.Equal(1, world.Effects.Expire(TimingPoints.EndOfPlayerPhase));
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:delayed-effect.1")]
    [Fact]
    public void ADelayedEffectComesDueWhenItsConditionOccurs()
    {
        // "Resolve automatically and immediately after their specified timing
        // point or future condition occurs or becomes true." The condition is
        // the trigger, and an unrelated one must not spend it.
        var world = Board();
        world.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect, "deal-damage", Amount: 1,
            Lasts: Duration.NextTime("WhenAttacked")));

        Assert.Empty(world.Effects.Occur("WhenRoundEnds"));
        Assert.Single(world.Effects.Active());

        Assert.Single(world.Effects.Occur("WhenAttacked"));
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:delayed-effect.1")]
    [Fact]
    public void ADelayedEffectCanWaitForTheNextSeveralTimes()
    {
        // "The next 2 times an enemy attacks you" is one registration that
        // survives the first occurrence. Removing it on the first would drop
        // half of what the card says.
        var world = Board();
        world.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect, "prevent", Amount: 1,
            Lasts: Duration.NextTime("WhenAttacked", times: 2)));

        Assert.Single(world.Effects.Occur("WhenAttacked"));
        Assert.Single(world.Effects.Active());

        Assert.Single(world.Effects.Occur("WhenAttacked"));
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:ability")]
    [Fact]
    public void AConstantAbilityStatesNoDurationOfItsOwn()
    {
        // "Becomes active as soon as its card enters play and remains active
        // while the card is in play" is the general rule, not something the
        // card says -- so it is the absence of every bound.
        Assert.True(Duration.WhileInPlay.IsWhileInPlay);
        Assert.False(Duration.UntilEndOf(TimingPoints.EndOfRound).IsWhileInPlay);
        Assert.False(Duration.NextUses(1).IsWhileInPlay);
        Assert.False(Duration.NextTime("WhenAttacked").IsWhileInPlay);
    }

    [Rule("rr:delayed-effect")]
    [Fact]
    public void ADelayedEffectOnWhoeverWasNamedDoesNothingWhenNobodyWas()
    {
        // "If a character is damaged by this attack, that character is stunned"
        // is written before there is a character to name, so the effect carries
        // no target and the occurrence supplies one when it comes due.
        //
        // Not every condition names a card. A round ending names nobody, and an
        // effect of this kind that came due there would have no character to
        // stun -- so it stuns none, rather than falling back on whichever card
        // happens to be first.
        var world = Board();
        world.CreateSeat("p1");
        var somebody = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea));

        world.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: DelayedEffects.StunTheSubject,
            Card: somebody.ObjectId,
            Affects: null,
            Lasts: Duration.NextTime("WhenRoundEnds")));

        var events = new List<GameEvent>();
        DelayedEffects.Occur(world, "WhenRoundEnds", events);

        Assert.False(Statuses.Has(world, somebody, Statuses.Stunned));
        Assert.Empty(events);
    }

    private static World Board()
    {
        var world = new World(new Facts(), players: 1);
        world.CreateSeat("p0");
        return world;
    }

    private sealed class TraitDependentConstants(int identity, int upgrade) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == upgrade)
            {
                return [new ContinuousEffect(
                    EffectSource.ConstantAbility,
                    Traits.Granted + "TECH",
                    Card: upgrade,
                    Affects: upgrade,
                    Lasts: Duration.WhileInPlay)];
            }

            if (card.ObjectId != identity)
            {
                return [];
            }

            long tech = world.Cards.Count(candidate =>
                candidate.Area.Type == DeckType.UpgradesArea
                && Traits.Has(world, candidate, "TECH", world.Facts));
            return [new ContinuousEffect(
                EffectSource.ConstantAbility,
                "hand_size",
                Amount: tech,
                Card: identity,
                Affects: identity,
                Lasts: Duration.WhileInPlay)];
        }
    }

    private sealed class OscillatingConstant(int card) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card candidate) =>
            candidate.ObjectId == card
            && !Traits.Has(world, candidate, "TECH", world.Facts)
                ? [new ContinuousEffect(
                    EffectSource.ConstantAbility,
                    Traits.Granted + "TECH",
                    Card: card,
                    Affects: card,
                    Lasts: Duration.WhileInPlay)]
                : [];
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "ally" => CardKind.Ally,
            "event" => CardKind.Event,
            "identity" => CardKind.Hero,
            "upgrade" => CardKind.Upgrade,
            _ => CardKind.Unknown,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            fallback;
    }
}
