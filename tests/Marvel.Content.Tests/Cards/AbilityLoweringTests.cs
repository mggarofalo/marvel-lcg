using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class AbilityLoweringTests
{
    private static readonly AbilityLocation Location = new("01005", 0, "effect/amount");

    [Fact]
    public void ArithmeticRetainsOperandOrderAndEngineOperations()
    {
        var syntax = Node("add", new AbilityValue.List([
            new AbilityValue.Number(2),
            Node("mul", new AbilityValue.List([
                Node("perPlayer", new AbilityValue.Number(3)),
                Node("result", new AbilityValue.Word("healed")),
            ])),
        ]));

        var sum = Assert.IsType<AbilityNumber.Sum>(AbilityLowering.Number(syntax, Location));

        Assert.Collection(sum.Operands,
            first => Assert.Equal(new AbilityNumber.Constant(2), first),
            second => Assert.Collection(Assert.IsType<AbilityNumber.Product>(second).Operands,
                factor => Assert.Equal(new AbilityNumber.PerPlayer(3), factor),
                factor => Assert.Equal(new AbilityNumber.Result("healed"), factor)));
    }

    [Theory]
    [InlineData("add")]
    [InlineData("mul")]
    public void EmptyArithmeticCollectionsKeepTheirDefinedIdentity(string kind)
    {
        var lowered = AbilityLowering.Number(Node(kind, new AbilityValue.List([])), Location);

        if (kind == "add")
        {
            Assert.Empty(Assert.IsType<AbilityNumber.Sum>(lowered).Operands);
        }
        else
        {
            Assert.Empty(Assert.IsType<AbilityNumber.Product>(lowered).Operands);
        }
    }

    [Fact]
    public void MinimumCannotDeferAnEmptyOperandFailureUntilGameplay()
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Number(
            Node("min", new AbilityValue.List([])), Location));

        Assert.Contains("'01005' ability 0 at 'effect/amount/min'", failure.Message,
            StringComparison.Ordinal);
        Assert.Contains("at least one", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryNestedOperandIsCheckedEvenIfItsValueWouldBeMultipliedByZero()
    {
        var syntax = Node("mul", new AbilityValue.List([
            new AbilityValue.Number(0),
            Node("perPlayer", new AbilityValue.Word("three")),
        ]));

        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Number(syntax, Location));

        Assert.Contains("effect/amount/mul/1/perPlayer", failure.Message, StringComparison.Ordinal);
        Assert.Contains("expected an integer", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownOperationsFailAtTheirAuthoredLocation()
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Number(
            Node("multiply", new AbilityValue.List([])), Location));

        Assert.Contains("effect/amount/multiply", failure.Message, StringComparison.Ordinal);
        Assert.Contains("not a numeric operation", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NumericOperationsCannotSilentlyIgnoreAnAdditionalProperty()
    {
        var syntax = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["perPlayer"] = new AbilityValue.Number(2),
            ["extra"] = new AbilityValue.Number(1),
        });

        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Number(syntax, Location));

        Assert.Contains("expected a number or one numeric operation", failure.Message,
            StringComparison.Ordinal);
    }

    private static AbilityValue.Map Node(string kind, AbilityValue argument) =>
        new(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { [kind] = argument });

    [Fact]
    public void RankingLowersItsRelationAndValueWithoutRestrictingCardTitles()
    {
        var selected = AbilityLowering.Cards(Selector(
            """{"maxBy":{"of":{"withTrait":{"cards":{"titled":"Any authored title"},"trait":"TECH"}},"by":"attack"}}"""), Location);

        var ranked = Assert.IsType<AbilityCardSelection.Ranked>(selected);
        Assert.True(ranked.Maximum);
        Assert.Equal(AbilityCardRank.Attack, ranked.By);
        var filtered = Assert.IsType<AbilityCardSelection.WithTrait>(ranked.Cards);
        Assert.Equal("TECH", filtered.Trait);
        Assert.Equal(new AbilityCardSelection.Titled("Any authored title"), filtered.Cards);
    }

    [Fact]
    public void SearchRetainsAreaOrderAndAllPrintedFilters()
    {
        var selected = AbilityLowering.Cards(Selector(
            """{"cardsIn":{"areas":["encounterDiscardPile","encounterDeck"],"kind":"Minion","trait":"CRIMINAL","title":"A title"}}"""), Location);

        var search = Assert.IsType<AbilityCardSelection.InAreas>(selected);
        Assert.Equal(new[] { AbilitySearchArea.EncounterDiscardPile, AbilitySearchArea.EncounterDeck }, search.Areas);
        Assert.Equal(Marvel.Rules.State.CardKind.Minion, search.Kind);
        Assert.Equal("CRIMINAL", search.Trait);
        Assert.Equal("A title", search.Title);
    }

    [Theory]
    [InlineData("\"trigger.target\"", AbilityCardBinding.TriggerTarget)]
    [InlineData("\"you\"", AbilityCardBinding.You)]
    public void CardBindingsAreLoweredToNamedEngineRelations(string json, AbilityCardBinding expected)
    {
        Assert.Equal(new AbilityCardSelection.Bound(expected), AbilityLowering.Cards(Selector(json), Location));
    }

    [Theory]
    [InlineData("{\"query\":\"attackableEnemies\"}", AbilityCardQuery.AttackableEnemies)]
    [InlineData("{\"query\":\"topmostTechInChosenDiscard\"}", AbilityCardQuery.TopmostTechInChosenDiscard)]
    public void QueriesAreLoweredToNamedEngineOperations(string json, AbilityCardQuery expected)
    {
        Assert.Equal(new AbilityCardSelection.Query(expected), AbilityLowering.Cards(Selector(json), Location));
    }

    [Theory]
    [InlineData("{\"withTrait\":{\"cards\":\"you\",\"trait\":\"TECH\",\"optional\":1}}", "withTrait/optional")]
    [InlineData("{\"cardsIn\":{\"area\":\"encounterDeck\",\"areas\":[]}}", "cardsIn")]
    [InlineData("{\"cardsIn\":{\"areas\":\"encounterDeck\"}}", "cardsIn/areas")]
    [InlineData("{\"cardsIn\":{\"area\":\"unknown\"}}", "cardsIn/area")]
    [InlineData("{\"cardsIn\":{\"area\":\"encounterDeck\",\"kind\":\"1\"}}", "cardsIn/kind")]
    [InlineData("{\"maxBy\":{\"of\":\"you\",\"by\":\"typo\"}}", "maxBy/by")]
    [InlineData("{\"query\":\"allTheCards\"}", "query")]
    [InlineData("\"someCard\"", "effect/amount")]
    public void InvalidSelectorsFailBeforeAnyBoardIsNeeded(string json, string path)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Cards(Selector(json), Location));

        Assert.Contains("'01005' ability 0", failure.Message, StringComparison.Ordinal);
        Assert.Contains(path, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoweredOperandsDoNotRetainAnAuthoredMutableList()
    {
        var operands = new List<AbilityValue> { new AbilityValue.Number(2) };
        var sum = Assert.IsType<AbilityNumber.Sum>(AbilityLowering.Number(
            Node("add", new AbilityValue.List(operands)), Location));

        operands[0] = new AbilityValue.Number(9);

        Assert.Equal(new AbilityNumber.Constant(2), Assert.Single(sum.Operands));
    }

    private static AbilityValue Selector(string json) => AbilityCatalog.Parse(
        $$"""
        { "cards": [ { "card": "01005", "abilities": [ {
          "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "this" },
          "effect": { "heal": { "cards": {{json}}, "amount": 1 } }
        } ] } ] }
        """).Abilities[0].Effect.Require("cards");

    [Fact]
    public void EveryCoreEnvelopeConditionLowersWithoutAWorld()
    {
        var conditions = AuthoredCards.Book.Abilities.Where(ability => ability.When is not null).ToList();
        Assert.NotEmpty(conditions);
        foreach (var ability in conditions)
        {
            var when = ability.When!;
            var lowered = AbilityLowering.Condition(Node(when.Kind, when.Argument),
                new AbilityLocation(ability.Card, 0, "when"));
            Assert.IsAssignableFrom<AbilityCondition>(lowered);
        }
    }

    [Fact]
    public void NumericConditionsValidateBothBranchesAndSupplyTheDefinedZeroDefault()
    {
        var test = Node("finalStep", new AbilityValue.Word("true"));
        var fields = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["test"] = test,
            ["then"] = new AbilityValue.Number(4),
        };
        var lowered = Assert.IsType<AbilityNumber.Conditional>(AbilityLowering.Number(
            Node("if", new AbilityValue.Map(fields)), Location));

        Assert.Equal(new AbilityCondition.Flag(AbilityConditionFact.FinalStep), lowered.Test);
        Assert.Equal(new AbilityNumber.Constant(4), lowered.Then);
        Assert.Equal(new AbilityNumber.Constant(0), lowered.Else);

        fields["else"] = Node("unknownNumber", new AbilityValue.Number(1));
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Number(
            Node("if", new AbilityValue.Map(fields)), Location));
        Assert.Contains("effect/amount/if/else/unknownNumber", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConditionLoweringDoesNotShortCircuitAfterAnEmptyDisjunction()
    {
        var syntax = Node("and", new AbilityValue.List([
            Node("or", new AbilityValue.List([])),
            Node("paidWithResource", new AbilityValue.Word("YY")),
        ]));

        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Condition(syntax, Location));

        Assert.Contains("effect/amount/and/1/paidWithResource", failure.Message, StringComparison.Ordinal);
        Assert.Contains("one supported resource symbol", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AFixedConditionArgumentCannotBeAcceptedAndThenIgnored()
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Condition(
            Node("finalStep", new AbilityValue.Word("false")), Location));

        Assert.Contains("expected 'true'", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NumericQueriesCarryTypedSelectorsAndRejectUnmappedEngineFields()
    {
        var fields = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["card"] = new AbilityValue.Word("you"),
            ["field"] = new AbilityValue.Word("attack"),
        };
        var lowered = AbilityLowering.Number(Node("modified", new AbilityValue.Map(fields)), Location);

        Assert.Equal(new AbilityNumber.Modified(
            new AbilityCardSelection.Bound(AbilityCardBinding.You), "attack"), lowered);

        fields["field"] = new AbilityValue.Word("imaginaryStat");
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Number(
            Node("modified", new AbilityValue.Map(fields)), Location));
        Assert.Contains("not an engine-owned modifiable field", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("healling")]
    [InlineData("defenseAbilityDefender")]
    public void ResultNamesMustHaveAuthoredEngineSemantics(string name)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Number(
            Node("result", new AbilityValue.Word(name)), Location));

        Assert.Contains("not an authored resolution result", failure.Message, StringComparison.Ordinal);
    }
}
