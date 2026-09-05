using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData("discard")]
    [InlineData("moveDamage")]
    [InlineData("removeThreat")]
    [InlineData("putIntoPlay")]
    public void AreaChangingEffectsRefuseAnUnstableSearchBeforePaying(string operation)
    {
        string effect = operation switch
        {
            "discard" => """{"discard":{"titled":"Shocker"}}""",
            "moveDamage" => """{"moveDamage":{"from":"you","to":{"titled":"Shocker"},"amount":1}}""",
            "removeThreat" => """{"removeThreat":{"scheme":{"query":"sideSchemes"},"amount":1}}""",
            _ => """{"putIntoPlay":{"card":{"cardsIn":{"area":"encounterDiscardPile","kind":"Minion"}},"where":"engagedWithYou"}}""",
        };
        string face = operation == "removeThreat" ? "01127" : "01103";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"seq":[{{{{{{effect}}}}}},
              {"search":{"in":[{"encounterDiscardPile":1}],"for":"{{{{{{face}}}}}}"}}]}
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        Card? affected = null;
        World? world = null;
        DeckType area = operation == "putIntoPlay" ? DeckType.EncounterDiscardPile
            : operation == "removeThreat" ? DeckType.SideSchemesArea : DeckType.EngagedEnemiesArea;

        var failure = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            affected = board.CreateCard(face, area == DeckType.EngagedEnemiesArea
                ? board.AreaOf(area, PlayArea.Of(0)) : board.AreaOf(area));
            if (operation == "moveDamage")
            {
                affected.TakeDamage(2);
                board.Seats[0].IdentityCard.TakeDamage(1);
            }
            if (operation == "removeThreat") affected.PlaceTokens("k_threat", 1);
        }, hero: true, abilities: runner));

        Assert.Contains("searches an area after its matching cards may change", failure.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(area, affected!.Area.Type);
        Assert.Equal(operation == "moveDamage" ? 2 : 0, affected.Damage);
        Assert.Equal(operation == "moveDamage" ? 1 : 0, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(operation == "removeThreat" ? 1 : 0, affected.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData("attack", false)]
    [InlineData("attack", true)]
    [InlineData("thwart", false)]
    [InlineData("thwart", true)]
    public void PowerEligibilityAndSchedulingUseTheCompiledTarget(string operation, bool choice)
    {
        string target = choice ? "\"chosen\"" : operation == "attack"
            ? """{"titled":"Shocker"}""" : """{"query":"mainScheme"}""";
        string body = operation == "attack"
            ? """{"dealAttackDamage":{"cards":"chosen","amount":1}}"""
            : """{"removeThreat":{"scheme":"chosen","amount":1}}""";
        string arguments = "{\"target\":" + target + ",\"effect\":" + body + "}";
        var (runner, fields) = MutableNumericRunner(operation, arguments, false,
            cards: choice ? operation == "attack" ? "minions" : "schemes" : null);
        fields["target"] = new AbilityValue.Word("yourAlterEgo");
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01103", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, hero: true, abilities: runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threat = main.Tokens["k_threat"];
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        if (choice)
        {
            Assert.Equal(Question.Element, game.Pending!.Asking);
            int chosen = operation == "attack" ? minion!.ObjectId : main.ObjectId;
            Assert.Equal([chosen], game.Pending.Affordances.Select(option => option.Id));
            game.Resolve(Decision.Take(chosen));
        }

        Assert.Equal(operation == "attack" ? 1 : 0, minion!.Damage);
        Assert.Equal(operation == "thwart" ? threat - 1 : threat, main.Tokens["k_threat"]);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutomaticThwartTargetingKeepsItsCompiledPermission(bool automatic)
    {
        string marker = automatic ? ",\"automaticTarget\":1" : string.Empty;
        var (runner, fields) = MutableEffectRunner("thwart", $$$$$$"""
            {"target":{"query":"mainScheme"}{{{{{{marker}}}}}},
             "effect":{"heal":{"card":"you","amount":1}}}
            """, false);
        if (automatic) fields.Remove("automaticTarget");
        else fields["automaticTarget"] = new AbilityValue.Number(1);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(2);
            board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea)).PlaceTokens("k_threat", 2);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, hero: true, abilities: runner);

        Assert.True(MainScheme.Crisis(world, world.Facts));
        if (!automatic)
        {
            Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
            Assert.True(source!.Ready);
            Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
            return;
        }
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.True(MainScheme.Crisis(world, world.Facts));
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupThwartSchedulingKeepsItsCompiledSchemeSet(bool choice)
    {
        var (runner, fields) = MutableNumericRunner("thwartSchemes", """
            {"schemes":{"query":"sideSchemes"},"power":{"thwart":{
              "target":"chosen","effect":{"removeThreat":{"scheme":{"query":"powerTargets"},"amount":1}}}}}
            """, false, cards: choice ? "sideSchemes" : null);
        fields["schemes"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            ["query"] = new AbilityValue.Word("mainScheme"),
        });
        Card? source = null;
        Card? side = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            side = board.CreateCard("01127", board.AreaOf(DeckType.SideSchemesArea));
            side.PlaceTokens("k_threat", 3);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 4);
        }, hero: true, abilities: runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threat = main.Tokens["k_threat"];
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        if (choice)
        {
            Assert.Equal(Question.Element, game.Pending!.Asking);
            Assert.Equal([side!.ObjectId], game.Pending.Affordances.Select(option => option.Id));
            game.Resolve(Decision.Take(side.ObjectId));
        }

        Assert.Equal(2, side!.Tokens["k_threat"]);
        Assert.Equal(threat, main.Tokens["k_threat"]);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("areas", false)]
    [InlineData("areas", true)]
    [InlineData("face", false)]
    [InlineData("face", true)]
    public void SearchEligibilityChecksItsCompiledAreasAndFace(string changed, bool ambiguous)
    {
        var (runner, fields) = MutableEffectRunner("search",
            """{"in":[{"encounterDiscardPile":1}],"for":"01103"}""", false);
        var (world, source) = FixedCountBoard(runner);
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        var first = world.CreateCard("01103", discard);
        if (ambiguous) world.CreateCard("01103", discard);
        var other = world.CreateCard("01102", discard);
        var original = discard.Cards.ToArray();
        if (changed == "areas") fields["in"] = new AbilityValue.List([]);
        else fields["for"] = new AbilityValue.Word("01102");

        if (ambiguous)
        {
            var failure = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Contains("2 copies of '01103'", failure.Message, StringComparison.Ordinal);
            Assert.Equal(original, discard.Cards);
            Assert.True(source.Ready);
            Assert.Empty(world.Agenda.Outstanding);
            return;
        }
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == source.ObjectId);
        Assert.True(source.Ready);
        Assert.Equal(original, discard.Cards);
        runner.Act(world, action, [], []);

        Assert.Equal([other], discard.Cards);
        Assert.Equal(DeckType.RevealingArea, first.Area.Type);
        Assert.Equal(first.ObjectId, Assert.Single(world.Agenda.Outstanding,
            step => step.What == Steps.RevealEncounterCard).Subject);
        Assert.False(source.Ready);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
    }
}
