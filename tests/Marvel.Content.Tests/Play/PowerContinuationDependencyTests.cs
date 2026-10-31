using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PowerContinuationDependencyTests : CoreCardTestBase
{
    [Fact]
    public void IndependentDrawCannotAdmitAnUnsupportedMinionDeparture()
    {
        var runner = Runner("01006", "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"dealAttackDamage":{"cards":{"titled":"Sandman"},"amount":4}},
              {"draw":{"player":"you","count":1}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? minion = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, "01006");
            minion = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("16149",
                board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId, cardOwner: -1));
        }, hero: true, abilities: runner));
        Assert.Contains("permanent attachment", error.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, minion!.Damage);
    }

    [Fact]
    public void OneDamageEffectCannotReadAnotherRecipientAfterAnUnprojectedStageConstant()
    {
        var runner = Runner("01006", "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"dealAttackDamage":{"cards":{"query":"enemies"},"amount":100}},
              {"draw":{"player":"you","count":1}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? minion = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, "01006");
            minion = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner, scenario: "crossbones"));
        Assert.Contains("constant abilities", error.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, minion!.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedDrawSuffixDoesNotEraseOuterBoardDependentReads(bool encounter)
    {
        // Synthetic contract: an inner fixed draw is independent, but the outer
        // continuation still needs the changed board. It must fail before costs.
        string damage = encounter
            ? """{"dealAttackDamage":{"cards":{"titled":"Hydra Soldier"},"amount":4}}"""
            : """{"dealAttackDamage":{"cards":{"query":"villain"},"amount":100}}""";
        string test = encounter
            ? """{"exists":{"cardsIn":{"area":"encounterDeck"}}}"""
            : """{"exists":{"query":"villain"}}""";
        var runner = Runner("01006", "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"seq":[DAMAGE,{"draw":{"player":"you","count":1}}]},
              {"if":{"test":TEST,"then":{"dealDamage":{"cards":{"query":"villain"},"amount":1}}}}
            ]}}}
            """.Replace("DAMAGE", damage, StringComparison.Ordinal)
                .Replace("TEST", test, StringComparison.Ordinal),
            cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? target = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, "01006");
            target = encounter
                ? board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)))
                : board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, abilities: runner, scenario: "crossbones"));
        Assert.Contains(encounter ? "unprojected read" : "constant abilities", error.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, target!.Damage);
    }

    [Fact]
    public void IndependentDrawDoesNotEraseAnAlreadyUnsupportedProjection()
    {
        var world = Hero("01010a");
        var source = world.CreateCard("01013", world.Seats[0].Hand);
        var query = new AbilityQueryContext(world, source, new Occurrence(0, []), 0,
            source.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var scope = new AbilityAdmission.AbilityAdmissionScope(new AbilityAdmissionContext(
            AuthoredCards.Program, world.ResourceAbilities, expression, new(), null), []);
        var draw = new AbilityEffect.Draw(new AbilityPlayerSelection.OnePlayer(AbilityPlayer.You), 1);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            AbilityPowerSuspension.SuspendsPowerEffect(draw, scope,
                reachability: new RuleProjection<AbilityPowerState>.Unsupported("required earlier board read")));
        Assert.Equal("required earlier board read", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PaymentBranchWithAChoiceStillRequiresSuspension(bool choiceFirst)
    {
        var world = Hero("01010a");
        var source = world.CreateCard("01013", world.Seats[0].Hand);
        var query = new AbilityQueryContext(world, source, new Occurrence(0, []), 0,
            source.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "Y", 0, false, null);
        var context = new AbilityAdmissionContext(AuthoredCards.Program,
            world.ResourceAbilities, expression, new(), null);
        var draw = new AbilityEffect.Draw(new AbilityPlayerSelection.OnePlayer(AbilityPlayer.You), 1);
        var choice = new AbilityEffect.Choose([draw, draw], ["first", "second"]);
        var conditional = new AbilityEffect.Conditional(new AbilityCondition.PaidWithResource('Y'),
            choiceFirst ? choice : draw, choiceFirst ? draw : choice);
        Assert.True(AbilityPowerSuspension.SuspendsPowerEffect(conditional, context, stateMayChange: true));
    }
}

