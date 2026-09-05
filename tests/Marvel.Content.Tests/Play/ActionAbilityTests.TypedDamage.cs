using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompiledThreatExceptionOverridesOnlyItsNamedSource(bool anotherProhibition)
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"preventThreatRemoval":{"query":"mainScheme"}}},
              {"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
               "effect":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1,"overridesCannotFrom":"this"}}}
            ]},{"card":"01091","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"preventThreatRemoval":{"query":"mainScheme"}}}
            ]}]}
            """);
        var fields = ((AbilityValue.Map)parsed.Abilities[1].Effect.Argument).Entries
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var abilities = parsed.Abilities.ToList();
        abilities[1] = abilities[1] with { Effect = new AbilityNode("removeThreat", new AbilityValue.Map(fields)) };
        var runner = new AbilityRunner(new AbilityBook(abilities, parsed.Authored));
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            if (anotherProhibition) InPlay(board, "01091");
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, abilities: runner);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.False(runner.CanRemoveThreat(world, scheme));
        Assert.Equal(!anotherProhibition, runner.CanRemoveThreat(world, scheme, source!.ObjectId));

        // Engine-owned source addressing: the compiled exception identifies
        // one prohibition, not permission to ignore every active prohibition.
        fields["overridesCannotFrom"] = new AbilityValue.Word("you");
        runner.WhenRevealed(world, source, 0);

        Assert.Equal(anotherProhibition ? 3 : 2, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(runner.CanRemoveThreat(world, scheme));
    }

    [Rule("rr:overkill.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AttackDamageKeepsItsCompiledOverkillFlag(bool overkill)
    {
        // "If this attack defeats a minion, deal the damage beyond its hit
        // points to the villain." The compiled flag decides whether this
        // synthetic attack has the keyword, not a later edit to its input.
        string marker = overkill ? ",\"overkill\":1" : string.Empty;
        var (runner, fields) = MutableDamageRunner("dealAttackDamage",
            $$"""{"cards":{"query":"minionsEngagedWithYou"},"amount":5{{marker}}}""", false);
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard(AuthoredCards.Shocker,
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        long excess = 5 - Damage.Health(world, Cards, minion!);
        Assert.True(excess > 0);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        if (overkill) fields.Remove("overkill");
        else fields["overkill"] = new AbilityValue.Number(1);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.EncounterDiscardPile, minion!.Area.Type);
        Assert.Equal(overkill ? excess : 0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Kind == Keywords.Overkill);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OrdinaryDamageKeepsItsCompiledEventVerb(bool attackVerb)
    {
        string marker = attackVerb ? ",\"attack\":1" : string.Empty;
        var (runner, fields) = MutableDamageRunner("dealDamage",
            $$"""{"cards":{"query":"villain"},"amount":1{{marker}}}""", false);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        if (attackVerb) fields.Remove("attack");
        else fields["attack"] = new AbilityValue.Number(1);

        var result = game.Resolve(Decision.Take(action.Id));

        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(1, villain.Damage);
        var change = Assert.Single(result.Events.OfType<FieldSet>(), entry => entry.Card == villain.ObjectId);
        Assert.Equal(attackVerb ? "Attack" : "Deal_Damage", change.Verb);
    }

    [Theory]
    [InlineData("dealDamage", false)]
    [InlineData("dealDamage", true)]
    [InlineData("dealAttackDamage", false)]
    [InlineData("moveDamage", false)]
    [InlineData("moveAttackDamage", false)]
    [InlineData("placeThreat", false)]
    [InlineData("removeThreat", false)]
    [InlineData("removeThreat", true)]
    public void DamageAndThreatExecutionUsesCompiledAmounts(string operation, bool repeated)
    {
        string arguments = operation switch
        {
            "moveDamage" or "moveAttackDamage" =>
                """{"from":"you","to":{"query":"villain"},"amount":1}""",
            "placeThreat" or "removeThreat" =>
                """{"scheme":{"query":"mainScheme"},"amount":1}""",
            _ => """{"cards":{"query":"villain"},"amount":1}""",
        };
        var (runner, fields) = MutableDamageRunner(operation, arguments, repeated);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(5);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        // Engine choice: lowering snapshots arguments. This also covers the
        // combined-instance path, which calls the resolver from forEach.
        fields["amount"] = new AbilityValue.Number(2);
        game.Resolve(Decision.Take(action.Id));

        int amount = repeated ? 2 : 1;
        Assert.Equal(operation is "placeThreat" or "removeThreat" ? 0 : amount,
            world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(operation is "moveDamage" or "moveAttackDamage" ? 4 : 5,
            world.Seats[0].IdentityCard.Damage);
        Assert.Equal(operation == "placeThreat" ? 4 : operation == "removeThreat" ? 3 - amount : 3,
            world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IndirectAssignmentKeepsCompiledAmountAndRecipients(bool mutateBeforeAction)
    {
        var (runner, fields) = MutableDamageRunner("indirectDamage",
            """{"among":{"query":"heroesAndAllies"},"amount":2}""", repeated: false);
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            ally = board.CreateCard("01020",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        void ChangeArguments()
        {
            fields["amount"] = new AbilityValue.Number(3);
            fields["among"] = new AbilityValue.Word("you");
        }

        // The same compiled instruction must govern prompt construction and
        // answer validation, even if the caller edits its original map.
        if (mutateBeforeAction) ChangeArguments();
        game.Resolve(Decision.Take(action.Id));
        var assignment = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(Question.Element, game.Pending.Asking);
        Assert.Equal(2, assignment.Targets!.Min);
        Assert.Equal(2, assignment.Targets.Max);
        Assert.Contains(ally!.ObjectId, assignment.Targets.Legal);
        ChangeArguments();
        game.Resolve(Decision.Take(assignment.Id, [identity.ObjectId, ally.ObjectId], []));

        Assert.Equal(1, identity.Damage);
        Assert.Equal(1, ally.Damage);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    private static (AbilityRunner Runner, Dictionary<string, AbilityValue> Fields) MutableDamageRunner(
        string operation, string arguments, bool repeated)
    {
        var parsed = AbilityCatalog.Parse($$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"{{operation}}":{{arguments}}}
            }]}]}
            """);
        var fields = ((AbilityValue.Map)parsed.Abilities[0].Effect.Argument).Entries
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var effect = new AbilityNode(operation, new AbilityValue.Map(fields));
        if (repeated)
        {
            effect = new AbilityNode("forEach", new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
            {
                ["count"] = new AbilityValue.Number(2),
                ["effect"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
                {
                    [operation] = effect.Argument,
                }),
            }));
        }
        return (new AbilityRunner(new AbilityBook(
            [parsed.Abilities[0] with { Effect = effect }], parsed.Authored)), fields);
    }
}
