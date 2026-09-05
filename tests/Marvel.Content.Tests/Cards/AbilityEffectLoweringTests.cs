using Marvel.Cards.Dsl;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class AbilityEffectLoweringTests
{
    private static readonly AbilityLocation Location = new("01005", 0, "effect");

    [Fact]
    public void MainSchemeAdvancementNamesTheImplementedNextStageOperation()
    {
        Assert.Equal(new AbilityEffect.Fixed(AbilityFixedInstruction.AdvanceMainScheme),
            Lower("""{"advanceMainScheme":"next"}"""));
        var failure = Assert.Throws<AbilityException>(() => Lower(
            """{"advanceMainScheme":"last"}"""));
        Assert.Contains("effect/advanceMainScheme", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"changeForm":{"player":"you","to":"alterEgo"}}""")]
    [InlineData("""{"heal":{"cards":"you","amount":1}}""")]
    [InlineData("""{"dealDamage":{"card":"you","amount":1}}""")]
    [InlineData("""{"generate":"E"}""")]
    public void UnimplementedSpellingsAreRejectedEvenInAnUnselectedBranch(string effect)
    {
        var failure = Assert.Throws<AbilityException>(() => Lower($$$"""
            {"if":{"test":{"inForm":{"player":"you","form":"hero"}},"else":{{{effect}}}}}
            """));
        Assert.Contains("effect/if/else/", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryCoreAbilityEffectLowersWithoutAWorld()
    {
        Assert.NotEmpty(AuthoredCards.Book.Abilities);
        foreach (var card in AuthoredCards.Book.Abilities.GroupBy(ability => ability.Card, StringComparer.Ordinal))
        {
            int ordinal = 0;
            foreach (var ability in card)
            {
                var effect = ability.Effect;
                Assert.IsAssignableFrom<AbilityEffect>(AbilityLowering.Effect(new AbilityValue.Map(
                    new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { [effect.Kind] = effect.Argument }),
                    new AbilityLocation(ability.Card, ordinal++, "effect")));
            }
        }
    }

    [Fact]
    public void EveryCoreConstantEffectLowersWithoutAWorld()
    {
        var constants = AuthoredCards.Book.Abilities.Where(ability => ability.Trigger.Timing == AbilityType.Constant).ToList();
        Assert.NotEmpty(constants);
        foreach (var ability in constants)
        {
            var effect = ability.Effect;
            Assert.IsAssignableFrom<AbilityEffect>(AbilityLowering.Effect(new AbilityValue.Map(
                new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { [effect.Kind] = effect.Argument }),
                new AbilityLocation(ability.Card, 0, "effect")));
        }
    }

    [Fact]
    public void ModifierLoweringPreservesTheDistinctExistingAmountDefaults()
    {
        var constant = Assert.IsType<AbilityEffect.GrantField>(Lower(
            """{"grant":{"card":"you","keyword":"retaliate"}}"""));
        var lasting = Assert.IsType<AbilityEffect.GrantField>(Lower(
            """{"grantUntil":{"card":"you","keyword":"retaliate","until":"EndOfRound"}}"""));

        Assert.Equal(new AbilityNumber.Constant(1), constant.Amount);
        Assert.Null(constant.Until);
        Assert.Equal(new AbilityNumber.Constant(0), lasting.Amount);
        Assert.Equal(TimingPoints.EndOfRound, lasting.Until);
    }

    [Fact]
    public void DelayedStunHasAFutureRecipientRatherThanAnOrdinaryCardSelector()
    {
        Assert.Equal(new AbilityEffect.DelayedStun(TimingPoints.EndOfAttack), Lower(
            """{"delayUntil":{"condition":"WhenDamageDealt","within":"EndOfAttack","effect":{"giveStatus":{"card":"damaged","status":"stunned"}}}}"""));
    }

    [Fact]
    public void AChoiceRetainsDescriptionsAndTypedBranches()
    {
        var choice = Assert.IsType<AbilityEffect.Choose>(Lower(
            """{"choose":{"descriptions":["Draw","Ready"],"options":[{"draw":{"player":"you","count":1}},{"ready":"you"}]}}"""));

        Assert.Collection(choice.Descriptions,
            first => Assert.Equal("Draw", first), second => Assert.Equal("Ready", second));
        Assert.Collection(choice.Options,
            first => Assert.Equal(new AbilityEffect.Draw(new AbilityPlayerSelection.OnePlayer(AbilityPlayer.You), 1), first),
            second => Assert.Equal(new AbilityEffect.CardAction(AbilityCardInstruction.Ready,
                new AbilityCardSelection.Bound(AbilityCardBinding.You)), second));
    }

    [Fact]
    public void AConditionalCanHaveOnlyAnElseBranch()
    {
        var conditional = Assert.IsType<AbilityEffect.Conditional>(Lower(
            """{"if":{"test":{"finalStep":"true"},"else":{"gainSurge":1}}}"""));

        Assert.Null(conditional.Then);
        Assert.Equal(new AbilityEffect.GainSurge(1), conditional.Else);
        Assert.Equal(new AbilityCondition.Flag(AbilityConditionFact.FinalStep), conditional.Test);
    }

    [Fact]
    public void OrdinaryAndAttackDamageRetainDifferentOperations()
    {
        var damage = Assert.IsType<AbilityEffect.Damage>(Lower(
            """{"dealDamage":{"cards":"chosen","amount":3}}"""));
        var attack = Assert.IsType<AbilityEffect.AttackDamage>(Lower(
            """{"dealAttackDamage":{"cards":"chosen","amount":3,"overkill":1}}"""));

        Assert.False(damage.AttackVerb);
        Assert.True(attack.Overkill);
        Assert.Equal(damage.Cards, attack.Cards);
        Assert.Equal(damage.Amount, attack.Amount);
    }

    [Fact]
    public void PreventionNamesTheSupportedOccurrenceRelation()
    {
        Assert.Equal(new AbilityEffect.PreventDamage(new AbilityNumber.Constant(long.MaxValue)),
            Lower("""{"preventDamage":"trigger.target"}"""));
        Assert.Equal(new AbilityEffect.PreventDamage(new AbilityNumber.Constant(3)),
            Lower("""{"preventDamage":{"card":"trigger.target","amount":3}}"""));
        var failure = Assert.Throws<AbilityException>(() => Lower(
            """{"preventDamage":{"card":"chosen","amount":3}}"""));
        Assert.Contains("expected 'trigger.target'", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DependentEffectsKeepTheirResultRequirement()
    {
        var then = Assert.IsType<AbilityEffect.Dependent>(Lower(
            """{"then":{"effect":{"ready":"you"},"then":{"draw":{"player":"you","count":1}}}}"""));
        var otherwise = Assert.IsType<AbilityEffect.Dependent>(Lower(
            """{"otherwise":{"effect":{"ready":"you"},"otherwise":{"draw":{"player":"you","count":1}}}}"""));

        Assert.True(then.OnFull);
        Assert.False(otherwise.OnFull);
        Assert.Equal(then.Effect, otherwise.Effect);
        Assert.Equal(then.Continuation, otherwise.Continuation);
    }

    [Theory]
    [InlineData("{\"choose\":{\"options\":[{\"ready\":\"you\"}]}}", "at least two")]
    [InlineData("{\"choose\":{\"descriptions\":[\"Only one\"],\"options\":[{\"ready\":\"you\"},{\"exhaust\":\"you\"}]}}", "one description per option")]
    [InlineData("{\"if\":{\"test\":{\"finalStep\":\"true\"},\"then\":{\"ready\":\"you\"},\"else\":{\"unknown\":1}}}", "if/else/unknown")]
    [InlineData("{\"draw\":{\"player\":\"you\",\"count\":1,\"ignoreDeck\":1}}", "draw/ignoreDeck")]
    [InlineData("{\"draw\":{\"player\":\"you\",\"count\":-1}}", "nonnegative engine-sized count")]
    [InlineData("{\"dealAttackDamage\":{\"cards\":\"you\",\"amount\":1,\"overkill\":0}}", "expected the marker 1")]
    [InlineData("{\"giveStatus\":{\"card\":\"you\",\"status\":\"togh\"}}", "supported status")]
    [InlineData("{\"changeForm\":{\"player\":\"you\",\"to\":\"bird\"}}", "supported form")]
    [InlineData("{\"placeAccelerationToken\":4}", "expected the marker 1")]
    [InlineData("{\"payOrExhaust\":{\"resources\":\"B\",\"otherwise\":{\"ready\":\"you\"}}}", "requires an exhaust alternative")]
    [InlineData("{\"removeThreat\":{\"scheme\":\"this\",\"amount\":1,\"ignoresCrisis\":\"maybe\"}}", "expected 'true' or 'false'")]
    [InlineData("{\"grant\":{\"card\":\"you\",\"keyword\":\"togh\"}}", "modifier implemented by the engine")]
    [InlineData("{\"grant\":{\"card\":\"you\",\"keyword\":\"attack\",\"trait\":\"AERIAL\"}}", "exactly one")]
    [InlineData("{\"grant\":{\"card\":\"you\",\"trait\":\"AERIAL\",\"amount\":1}}", "no numeric amount")]
    [InlineData("{\"grantUntil\":{\"card\":\"you\",\"keyword\":\"attack\",\"until\":\"Eventually\"}}", "supported timing point")]
    [InlineData("{\"preventDamageWhile\":{\"card\":\"chosen\",\"condition\":{\"finalStep\":\"true\"}}}", "expected 'this'")]
    [InlineData("{\"delayUntil\":{\"condition\":\"WhenRoundEnds\",\"within\":\"EndOfAttack\",\"effect\":{\"discard\":\"this\"}}}", "does not implement a separate timing bound")]
    public void InvalidEffectPropertiesFailBeforeChoosingAPath(string json, string expected)
    {
        var failure = Assert.Throws<AbilityException>(() => Lower(json));

        Assert.Contains("'01005' ability 0 at 'effect/", failure.Message, StringComparison.Ordinal);
        Assert.Contains(expected, failure.Message, StringComparison.Ordinal);
    }

    private static AbilityEffect Lower(string json)
    {
        var effect = AbilityCatalog.Parse($$$"""
            {"cards":[{"card":"01005","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{{{json}}}
            }]}]}
            """).Abilities[0].Effect;
        return AbilityLowering.Effect(new AbilityValue.Map(
            new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { [effect.Kind] = effect.Argument }), Location);
    }
}
