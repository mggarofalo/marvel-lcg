using System.Text.Json.Nodes;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class IronManHandSizeMaximumTests : CoreCardTestBase
{
    [Theory]
    [InlineData(3, 5)]
    [InlineData(5, 7)]
    [InlineData(6, 7)]
    [Rule("rr:modifiers.2")]
    public void AssessRaisesHandSizeOnlyUpToIronMansTotalMaximum(int techCount, int expected)
    {
        // rr:modifiers.2: "all additive and subtractive modifiers are calculated" together.
        // Ruling 39c26b6b8d77edfd confirms Iron Man's total hand-size maximum.
        var world = Hero("01029a,01029b", 2);
        var runner = Runner();
        world.Abilities = runner;
        Card hero = world.Seats[0].IdentityCard;
        AddTech(world, techCount);
        Assert.Equal(1 + techCount, Modified(world, hero, "hand_size"));
        PlayAssess(world, runner);
        Assert.Equal(expected, Modified(world, hero, "hand_size"));
        Assert.Equal(5, Modified(world, world.Seats[1].IdentityCard, "hand_size"));
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(1 + techCount, Modified(world, hero, "hand_size"));
    }

    [Fact]
    public void AssessOnAnotherHeroIsNotSubjectToIronMansMaximum()
    {
        var world = Hero("01029a,01029b", 2);
        var runner = Runner();
        world.Abilities = runner;
        AddTech(world, 6);
        var other = world.Seats[1];
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "hand_size", 2,
            Affects: other.IdentityCard.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase)));
        var assess = world.CreateCard("12033", other.Hand);
        var action = Assert.Single(runner.Actions(world, 1), pending => pending.Card == assess.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(8, Modified(world, other.IdentityCard, "hand_size"));
        Assert.Equal(7, Modified(world, world.Seats[0].IdentityCard, "hand_size"));
    }

    [Fact]
    public void MultipleAssessCopiesAndNegativeModifiersApplyBeforeTheMaximum()
    {
        var world = Hero("01029a,01029b");
        var runner = Runner();
        world.Abilities = runner;
        Card hero = world.Seats[0].IdentityCard;
        AddTech(world, 6);
        PlayAssess(world, runner);
        PlayAssess(world, runner);
        Assert.Equal(7, Modified(world, hero, "hand_size"));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "hand_size", -2,
            Affects: hero.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase)));
        Assert.Equal(7, Modified(world, hero, "hand_size"));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "hand_size", -1,
            Affects: hero.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase)));
        Assert.Equal(6, Modified(world, hero, "hand_size"));
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(7, Modified(world, hero, "hand_size"));
    }

    [Fact]
    public void MaximumFollowsFaceAndBlankingAndDrawUsesTheCappedValue()
    {
        var world = Hero("01029a,01029b");
        var runner = Runner();
        world.Abilities = runner;
        Card hero = world.Seats[0].IdentityCard;
        AddTech(world, 6);
        PlayAssess(world, runner);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "hand_size", 2,
            Affects: hero.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase)));
        for (int i = 0; i < 10; i++) world.CreateCard("01088", world.Seats[0].Deck);
        PhaseEnd.DrawToHandSize(world, Cards, []);
        Assert.Equal(7, world.Seats[0].Hand.Cards.Count);
        hero.TurnTo("01029b");
        Assert.Equal(9, Modified(world, hero, "hand_size"));
        hero.TurnTo("01029a");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked,
            Affects: hero.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound))
            { TargetIncarnation = hero.Incarnation });
        Assert.Equal(4, Modified(world, hero, "hand_size"));
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Equal(7, Modified(world, hero, "hand_size"));
    }

    [Fact]
    public void SyntheticProjectionAppliesTheMaximumAfterProjectedModifiers()
    {
        // The generic projection helpers do not currently have a reachable hand-size consumer.
        var world = Hero("01029a");
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "hand_size", 7,
            Affects: hero.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "hand_size_maximum", 7,
            Affects: hero.ObjectId));
        var query = new AbilityQueryContext(world, hero, new Occurrence(1, []), 0,
            hero.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var context = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expression, new(), null);
        var projected = new AreaProjectionState(context);
        projected.Modifiers[(hero.ObjectId, "hand_size")] = -2;
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var modifiers = new Dictionary<(int Card, string Field), long> { [(hero.ObjectId, "hand_size")] = -2 };
        Assert.Equal(7, Modified(world, hero, "hand_size"));
        Assert.Equal(6, projected.ModifiedOf(context, hero, "hand_size"));
        Assert.Equal(6, AbilityRepeatedCharacteristicTrace.TraceModified(hero, "hand_size", scope, [], modifiers));
        Assert.Equal(7, Modified(world, hero, "hand_size"));

    }

    [Theory]
    [InlineData(false, "hand_size", 10, 3, true)]
    [InlineData(true, "hand_size", 10, 3, true)]
    [InlineData(false, "hand_size", 2, 0, true)]
    [InlineData(true, "hand_size", 2, 0, true)]
    [InlineData(false, "health", 2, -6, true)]
    [InlineData(true, "health", 2, -6, true)]
    [InlineData(false, "hand_size", 20, 13, false)]
    [InlineData(true, "hand_size", 20, 13, false)]
    [InlineData(false, "hand_size", 2, 0, false)]
    [InlineData(true, "hand_size", 2, 0, false)]
    [InlineData(false, "attack", 10, 3, false)]
    [InlineData(true, "attack", 10, 3, false)]
    [InlineData(false, "attack", 2, 0, false)]
    [InlineData(true, "attack", 2, 0, false)]
    public void SyntheticProjectionClampsAfterAllModifiers(bool repeatedTrace, string field, long projectedAmount, long expected, bool maximum)
    {
        // Synthetic contract: no current end-to-end projected hand-size consumer exists.
        var world = Hero("01029a");
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, field, -8,
            Affects: hero.ObjectId));
        if (maximum)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "hand_size_maximum", 7,
                Affects: hero.ObjectId));
        var query = new AbilityQueryContext(world, hero, new Occurrence(1, []), 0,
            hero.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var context = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expression, new(), null);
        var projected = new AreaProjectionState(context);
        projected.Modifiers[(hero.ObjectId, field)] = projectedAmount;
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var modifiers = new Dictionary<(int Card, string Field), long> { [(hero.ObjectId, field)] = projectedAmount };
        long result = repeatedTrace
            ? AbilityRepeatedCharacteristicTrace.TraceModified(hero, field, scope, [], modifiers)
            : projected.ModifiedOf(context, hero, field);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-8, long.MinValue, long.MinValue)]
    [InlineData(8, long.MaxValue, long.MaxValue)]
    public void SyntheticAreaProjectionSaturatesSignedAdjustments(long live, long projectedAmount, long expected)
    {
        var world = Hero("01029a");
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "health", live,
            Affects: hero.ObjectId));
        var query = new AbilityQueryContext(world, hero, new Occurrence(1, []), 0,
            hero.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var context = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expression, new(), null);
        var projection = new AreaProjectionState(context);
        projection.Modifiers[(hero.ObjectId, "health")] = projectedAmount;
        Assert.Equal(expected, projection.ModifiedOf(context, hero, "health"));
    }

    private static AbilityRunner Runner()
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        book["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"12033","abilities":[{"trigger":{"event":"WhenActionTriggered",
             "timing":"Action","subject":"this"},"effect":{"grantUntil":{"card":"you",
             "keyword":"hand_size","amount":1,"until":"EndOfPhase"}}}]}
            """));
        return new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
    }

    private static void AddTech(World world, int count)
    {
        var area = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
            world.Seats[0].IdentityCard.ObjectId, 0);
        for (int i = 0; i < count; i++) world.CreateCard("01038", area);
    }

    private static void PlayAssess(World world, AbilityRunner runner)
    {
        var assess = world.CreateCard("12033", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == assess.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, Cards, runner, []);
    }
}
