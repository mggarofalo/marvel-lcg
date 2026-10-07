using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.State;

public sealed class CardValueEvaluationTests
{
    [Rule("rr:base-value")]
    [Rule("rr:modifiers")]
    [Rule("rr:loses.2")]
    [Fact]
    public void ADefinitionReplacesTheBaseBeforeModifiersRegardlessOfRegistrationOrder()
    {
        // "A defined value before modifiers are applied." A lost
        // characteristic "cannot be regained" while the loss remains active.
        var (world, hero) = Board();
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 2, Affects: hero.ObjectId));
        world.Effects.Register(new(EffectSource.ConstantAbility, "attack", 6,
            hero.ObjectId, hero.ObjectId) { ValueRole = ContinuousValueRole.BaseDefinition });

        CardValueEvaluation value = CardValues.Evaluate(world, hero, "attack", world.Facts, 1);
        Assert.Equal(CardValueBaseKind.Defined, value.BaseKind);
        Assert.Equal(6, value.BaseValue);
        Assert.Equal(8, value.CurrentValue);
        Assert.Equal(new[] { CardValueStepKind.DefineBase, CardValueStepKind.Add }, value.Steps.Select(step => step.Kind));
        Assert.Equal(new long[] { 6, 8 }, value.Steps.Select(step => step.Result));

        world.Effects.Register(new(EffectSource.LastingEffect, Characteristics.LossOf("attack"), Affects: hero.ObjectId));
        value = CardValues.Evaluate(world, hero, "attack", world.Facts, 1);
        Assert.Equal(6, value.BaseValue);
        Assert.Equal(0, value.CurrentValue);
        Assert.Equal(new[] { CardValueStepKind.DefineBase, CardValueStepKind.Lost }, value.Steps.Select(step => step.Kind));
    }

    [Rule("rr:modifiers")]
    [Fact]
    public void TheNumericConsumerAndExplanationUseTheSameOrderedEvaluation()
    {
        // "The game constantly checks ... the count of any variable quantity
        // that is being modified." The trace is produced by that calculation.
        var (world, hero) = Board();
        var attached = world.CreateCard("attachment", world.CreateArea(
            DeckType.UpgradesArea, cardOwner: 0, playArea: PlayArea.Of(0), host: hero.ObjectId));
        var source = world.CreateCard("effect", world.Seats[0].Hero);
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 2,
            source.ObjectId, hero.ObjectId, Duration.UntilEndOf(TimingPoints.EndOfRound)));

        CardValueEvaluation value = CardValues.Evaluate(world, hero, "attack", world.Facts, 1);

        Assert.Equal(2, value.BaseValue);
        Assert.Equal(5, value.CurrentValue);
        Assert.Equal(value.CurrentValue, StateFields.Modified(world, hero, "attack", world.Facts, 1));
        Assert.Equal(new long[] { 1, 2 }, value.Steps.Select(step => step.Amount));
        Assert.Equal(new long[] { 3, 5 }, value.Steps.Select(step => step.Result));
        Assert.Equal(new[] { attached.ObjectId, source.ObjectId }, value.Steps.Select(step => step.Source!.CardId));
        Assert.Equal(TimingPoints.EndOfRound, value.Steps[1].Duration!.Until);

        World.MoveToTop(attached, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Equal(2, CardValues.Evaluate(world, hero, "attack", world.Facts, 1).CurrentValue);
    }

    [Rule("rr:modifiers.4")]
    [Rule("rr:dash-value.3")]
    [Rule("rr:loses.2")]
    [Fact]
    public void ClampingDashAndLossAreOperationsRatherThanInventedAdditiveModifiers()
    {
        // Complete modified values have a minimum of zero; a dash is an
        // "unmodifiable value of zero"; a lost characteristic cannot be regained.
        var (world, hero) = Board();
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", -5, Affects: hero.ObjectId));
        world.Effects.Register(new(EffectSource.LastingEffect, "thwart", 3, Affects: hero.ObjectId));

        CardValueEvaluation attack = CardValues.Evaluate(world, hero, "attack", world.Facts, 1);
        Assert.Equal(0, attack.CurrentValue);
        Assert.Equal(-3, attack.Steps[0].Result);
        Assert.Equal(CardValueStepKind.MinimumZero, attack.Steps[1].Kind);
        CardValueEvaluation thwart = CardValues.Evaluate(world, hero, "thwart", world.Facts, 1);
        Assert.Equal(0, thwart.CurrentValue);
        Assert.Equal(CardValueStepKind.Unmodifiable, Assert.Single(thwart.Steps).Kind);

        world.Effects.Register(new(EffectSource.LastingEffect, Characteristics.LossOf("attack"), Affects: hero.ObjectId));
        Assert.Equal(CardValueStepKind.Lost,
            Assert.Single(CardValues.Evaluate(world, hero, "attack", world.Facts, 1).Steps).Kind);
    }

    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void ModifyingMaximumHealthRetainsDamageAndChangesRemainingHealth()
    {
        // A character that gets +X hit points increases its maximum and
        // remaining hit points; the ability does not heal damage on that card.
        var (world, hero) = Board();
        hero.TakeDamage(4);
        using var effect = world.Effects.Register(new(EffectSource.LastingEffect, "health", 3, Affects: hero.ObjectId));

        CardValueEvaluation value = CardValues.MaximumHealth(world, hero, world.Facts);
        Assert.Equal(10, value.BaseValue);
        Assert.Equal(13, value.CurrentValue);
        Assert.Equal(13, Assert.Single(value.Steps).Result);
        Assert.Equal(value.CurrentValue, DamagePlacement.Health(world, world.Facts, hero));
        Assert.Equal(9, CardValues.RemainingHealth(world, hero, world.Facts));
        Assert.Equal(4, hero.Damage);
        effect.Dispose();
        Assert.Equal(6, CardValues.RemainingHealth(world, hero, world.Facts));
        Assert.Equal(4, hero.Damage);
    }

    [Rule("rr:lasting-effects.1")]
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void ALastingSourceRetainsItsOriginalIdentityWhenThePhysicalCardIsReused()
    {
        // The lasting effect outlives its source; a returned card is a new copy
        // with no memory of its previous existence. Its ID is not provenance.
        var (world, hero) = Board();
        var source = world.CreateCard("effect", world.Seats[0].Hero);
        int incarnation = source.Incarnation;
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 1, source.ObjectId, hero.ObjectId));
        World.MoveToTop(source, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        World.MoveToTop(source, world.Seats[0].Hero);
        source.AssignProfile(new("new-profile", "New identity", CardKind.Minion, [],
            ImmutableDictionary<string, long>.Empty.Add("ATK", 7)));

        CardSourceSnapshot origin = Assert.Single(CardValues.Evaluate(world, hero, "attack", world.Facts, 1).Steps).Source!;
        Assert.Equal(incarnation, origin.Incarnation);
        Assert.NotEqual(source.Incarnation, origin.Incarnation);
        Assert.Equal("effect", origin.FaceId);
        Assert.Equal("Title effect", origin.Title);
    }

    [Fact]
    public void ReplacementAndOriginExposeOnlyTheEffectiveIdentityAndReadsArePure()
    {
        var (world, hero) = Board();
        var source = world.CreateCard("secret-physical-face", world.Seats[0].Hero);
        source.AssignProfile(new("public-profile", "Drone", CardKind.Minion, [],
            ImmutableDictionary<string, long>.Empty.Add("ATK", 1)));
        source.TurnFaceDown();
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 2, source.ObjectId, hero.ObjectId));
        string digest = world.Digest().Canonical();
        long randomWords = world.Random.Generator.WordsConsumed;
        ContinuousEffect[] effects = world.Effects.Registered.ToArray();

        for (int i = 0; i < 3; i++)
        {
            CardValueEvaluation profile = CardValues.Evaluate(world, source, "attack", world.Facts, 1);
            Assert.Equal(CardValueBaseKind.Replacement, profile.BaseKind);
            Assert.Equal(1, profile.BaseValue);
            CardSourceSnapshot origin = Assert.Single(CardValues.Evaluate(world, hero, "attack", world.Facts, 1).Steps).Source!;
            Assert.Equal("public-profile", origin.FaceId);
            Assert.Equal("Drone", origin.Title);
            Assert.True(origin.Exposure.ReplacementIdentity);
        }

        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(randomWords, world.Random.Generator.WordsConsumed);
        Assert.Equal(effects, world.Effects.Registered);
    }

    private static (World World, Card Hero) Board()
    {
        var world = new World(new CardValueFacts(), 1, seed: 7);
        var seat = world.CreateSeat("p0");
        var hero = world.CreateCard("hero", seat.Hero);
        seat.IdentityCard = hero;
        return (world, hero);
    }
}
